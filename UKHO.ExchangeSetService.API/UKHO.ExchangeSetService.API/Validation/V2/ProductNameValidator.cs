using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.Validation.V2
{
    public class ProductNameValidator : AbstractValidator<ProductNameRequest>, IProductNameValidator
    {
        public ProductNameValidator()
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

        Task<ValidationResult> IProductNameValidator.Validate(ProductNameRequest productNameRequest)
        {
            return ValidateAsync(productNameRequest);
        }
    }
}
