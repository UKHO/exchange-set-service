using System;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.Validation
{
    public interface IProductDataSinceDateTimeValidator
    {
        Task<ValidationResult> Validate(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest);
    }

    public class ProductDataSinceDateTimeValidator : AbstractValidator<ProductDataSinceDateTimeRequest>, IProductDataSinceDateTimeValidator
    {
        private readonly IConfiguration configuration;
        private DateTime sinceDateTime;

        public ProductDataSinceDateTimeValidator(IConfiguration configuration)
        {
            this.configuration = configuration;

            RuleFor(x => x.SinceDateTime)
                .Must(x => x.IsValidRfc1123Format(out sinceDateTime))
                .WithMessage($"Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format' (e.g. 'Wed, 21 Oct 2020 07:28:00 GMT').")
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .DependentRules(() =>
                {
                    RuleFor(x => x.SinceDateTime)
                    .Must(x => DateTime.Compare(sinceDateTime, DateTime.UtcNow) <= 0)
                    .WithMessage("Provided sinceDateTime cannot be a future date.")
                    .WithErrorCode(HttpStatusCode.BadRequest.ToString());
                    RuleFor(x => x.SinceDateTime)
                    .Must(x => DateTime.Compare(sinceDateTime, DateTime.UtcNow.AddDays(-GetValidTillDays())) > 0)
                    .WithMessage("Provided sinceDateTime must be within last " +configuration["SinceDateTimeDateValidTillDateOfPastWeeks"] + " weeks.")
                    .WithErrorCode(HttpStatusCode.BadRequest.ToString());
                });

            RuleFor(x => x.CallbackUri)
                .Must(x => x.IsValidCallbackUri()).When(x => !string.IsNullOrEmpty(x.CallbackUri))
                .WithMessage("Invalid callbackUri format.")
                .WithErrorCode(HttpStatusCode.BadRequest.ToString());
        }

        private int GetValidTillDays()
        {
            int daysInWeek = 7;
            string validSinceDateTimeTillWeeks = Convert.ToString(configuration["SinceDateTimeDateValidTillDateOfPastWeeks"]);
            return validSinceDateTimeTillWeeks != null ? (daysInWeek * Convert.ToInt32(validSinceDateTimeTillWeeks)) : 0;
        }

        Task<ValidationResult> IProductDataSinceDateTimeValidator.Validate(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest)
        {
            return ValidateAsync(productDataSinceDateTimeRequest);
        }
    }
}