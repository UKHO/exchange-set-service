using System;
using FluentValidation;
using FluentValidation.Results;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.Validation
{
    public class ProductIdentifierValidator : AbstractValidator<ProductIdentifierRequest>, IProductIdentifierValidator
    {
        public ProductIdentifierValidator()
        {
            RuleFor(p => p.ProductIdentifier)
                .Must(pi => pi != null && pi.Length != 0 && pi.All(u => !string.IsNullOrWhiteSpace(u)) && pi != Array.Empty<string>())
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("productIdentifiers cannot be null or empty.");

            RuleFor(x => x.CallbackUri)
                .Must(x => x.IsValidCallbackUri()).When(x => !string.IsNullOrEmpty(x.CallbackUri))
                .WithMessage("Invalid callbackUri format.")
                .WithErrorCode(HttpStatusCode.BadRequest.ToString());
        }
        Task<ValidationResult> IProductIdentifierValidator.Validate(ProductIdentifierRequest productIdentifierRequest)
        {
            return ValidateAsync(productIdentifierRequest);
        }
    }
}
