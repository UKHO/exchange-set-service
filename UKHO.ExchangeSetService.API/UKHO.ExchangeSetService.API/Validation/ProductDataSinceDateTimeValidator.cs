using System;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
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
        private DateTime sinceDateTime;
        public ProductDataSinceDateTimeValidator()
        {
            RuleFor(x => x.SinceDateTime)
                .Must(x => x.IsValidRfc1123Format(out sinceDateTime))
                .WithMessage($"Provided SinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format' (e.g. 'Wed, 21 Oct 2020 07:28:00 GMT').")
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .DependentRules(() =>
                {
                    RuleFor(x => x.SinceDateTime)
                    .Must(x => DateTime.Compare(sinceDateTime, DateTime.UtcNow) <= 0)
                    .WithMessage("Provided SinceDateTime cannot be a future date.")
                    .WithErrorCode(HttpStatusCode.BadRequest.ToString());
                });

            RuleFor(x => x.CallbackUri)
                .Must(x => x.IsValidCallbackUri()).When(x => !string.IsNullOrEmpty(x.CallbackUri))
                .WithMessage("Invalid CallbackUri format.")
                .WithErrorCode(HttpStatusCode.BadRequest.ToString());
        }

        Task<ValidationResult> IProductDataSinceDateTimeValidator.Validate(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest)
        {
            return ValidateAsync(productDataSinceDateTimeRequest);
        }


    }
}
