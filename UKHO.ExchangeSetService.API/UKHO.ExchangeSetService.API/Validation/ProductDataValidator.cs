using System;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.Validation
{
    public interface IProductDataValidator
    {
        Task<ValidationResult> Validate(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest);
    }

    public class ProductDataValidator : AbstractValidator<ProductDataSinceDateTimeRequest>, IProductDataValidator
    {
        private DateTime sinceDateTime;
        public ProductDataValidator()
        {
            RuleFor(x => x.SinceDateTime)
                .Must(x => x.IsValidRfc1123Format(out sinceDateTime)).WithMessage($"Provided since date time is either invalid or invalid format, the valid formats are 'RFC1123 formats'.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.SinceDateTime)
                    .Must(x => DateTime.Compare(sinceDateTime, DateTime.UtcNow) <= 0).WithMessage("Since date time cannot be a future date.");
                })
                .WithErrorCode(HttpStatusCode.BadRequest.ToString());

            RuleFor(x => x.CallbackUri).NotEmpty().Matches(@"^http(s)?://([\w-]+.)+[\w-]+(/[\w- ./?%&=])?$").WithMessage("Please Check URI Pattern").WithErrorCode(HttpStatusCode.BadRequest.ToString()); 
        }

        Task<ValidationResult> IProductDataValidator.Validate(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest)
        {
            return ValidateAsync(productDataSinceDateTimeRequest);
        }
    }
}
