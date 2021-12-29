using FluentValidation;
using FluentValidation.Results;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.Validation
{
    public interface IProductDataProductVersionsValidator
    {
        Task<ValidationResult> Validate(ProductDataProductVersionsRequest request);
    }
    public class ProductDataProductVersionsValidator : AbstractValidator<ProductDataProductVersionsRequest>, IProductDataProductVersionsValidator
    {
        public ProductDataProductVersionsValidator()
        {
            RuleFor(x => x.CallbackUri)
                .Must(x => x.IsValidCallbackUri()).When(x => !string.IsNullOrEmpty(x.CallbackUri))
                .WithMessage("Invalid callbackUri format.")
                .WithErrorCode(HttpStatusCode.BadRequest.ToString());
            RuleFor(v => v.ProductVersions).NotEmpty().NotNull()
            .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                 .WithMessage("productVersions cannot be null.");
            RuleForEach(v => v.ProductVersions).SetValidator(new ProductVersionsValidator());
        }

        Task<ValidationResult> IProductDataProductVersionsValidator.Validate(ProductDataProductVersionsRequest request)
        {
            return ValidateAsync(request);
        }

        public class ProductVersionsValidator : AbstractValidator<ProductVersionRequest>
        {
            public ProductVersionsValidator()
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
