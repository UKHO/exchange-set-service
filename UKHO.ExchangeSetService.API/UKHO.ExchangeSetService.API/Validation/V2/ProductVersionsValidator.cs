using FluentValidation;
using FluentValidation.Results;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.Validation.V2
{   
    public class ProductVersionsValidator : AbstractValidator<ProductVersionsRequest>, IProductVersionsValidator
    {
        public ProductVersionsValidator()
        {
            RuleFor(x => x.CallbackUri)
               .Must(x => x.IsValidCallbackUri()).When(x => !string.IsNullOrEmpty(x.CallbackUri))
               .WithMessage("Invalid callbackUri format.")
               .WithErrorCode(HttpStatusCode.BadRequest.ToString());

            RuleFor(v => v.ProductVersions).NotEmpty().NotNull()
                .Must(productVersions => productVersions != null && productVersions.Any())
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("productVersions cannot be null or empty.");

            RuleForEach(v => v.ProductVersions).SetValidator(new ProductVersionValidator());
        }

        Task<ValidationResult> IProductVersionsValidator.Validate(ProductVersionsRequest request)
        {
            return ValidateAsync(request);
        }

        public class ProductVersionValidator : AbstractValidator<ProductVersionRequest>
        {
            public ProductVersionValidator()
            {
                RuleFor(v => v.ProductName).NotEmpty().NotNull().OverridePropertyName("productName").Must(ru => !string.IsNullOrWhiteSpace(ru))
                .When(ru => ru != null)
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("productName cannot be blank or null.");

                RuleFor(v => v.EditionNumber).NotEmpty().NotNull().OverridePropertyName("editionNumber").GreaterThanOrEqualTo(0).Must(ru => ru.HasValue && ru >= 0)
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("editionNumber cannot be less than zero or null.");

                RuleFor(v => v.UpdateNumber).NotEmpty().NotNull().OverridePropertyName("updateNumber").GreaterThanOrEqualTo(0).Must(ru => ru.HasValue && ru >= 0)
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("updateNumber cannot be less than zero or null.");
            }
        }
    }
}
