using System;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.Validation
{
    public interface IScsDataSinceDateTimeValidator
    {
        Task<ValidationResult> Validate(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest);
    }
    public class ScsDataSinceDateTimeValidator : AbstractValidator<ProductDataSinceDateTimeRequest>, IScsDataSinceDateTimeValidator
    {
        private readonly IConfiguration configuration;
        private DateTime sinceDateTime;

        public ScsDataSinceDateTimeValidator(IConfiguration configuration)
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
                        .Must(x => DateTime.Compare(sinceDateTime, DateTime.UtcNow.AddDays(-Convert.ToInt32(this.configuration["MaximumNumerOfDaysValidForSinceDateTimeEndpoint"]))) > 0)
                        .WithMessage("Provided sinceDateTime must be within last " + configuration["MaximumNumerOfDaysValidForSinceDateTimeEndpoint"] + " days.")
                        .WithErrorCode(HttpStatusCode.BadRequest.ToString());
                });
        }

        Task<ValidationResult> IScsDataSinceDateTimeValidator.Validate(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest)
        {
            return ValidateAsync(productDataSinceDateTimeRequest);
        }
    }
}