using FluentValidation;
using FluentValidation.Results;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Extensions;
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
                .WithMessage("Invalid CallbackUri format.")
                .WithErrorCode(HttpStatusCode.BadRequest.ToString());
            RuleFor(v => v.ProductVersions).NotEmpty().NotNull()
            .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                 .WithMessage("Product Versions cannot be null.");
            When(b => b != null && b.ProductVersions != null, () =>
            {
                RuleFor(b => b.ProductVersions)
                     .Must(ru => ru.All(u => u != null && !string.IsNullOrWhiteSpace(u.ProductName)))
                     .When(ru => ru.ProductVersions != null)
                     .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                     .WithMessage("Product Versions product name cannot be blank or null.");
                RuleFor(b => b.ProductVersions)
                     .Must(ru => ru.All(u => u != null && u.EditionNumber != null && u.EditionNumber >= 0))
                     .When(ru => ru.ProductVersions != null)
                     .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                     .WithMessage("Product Versions edition number cannot be less than zero or null.");
                RuleFor(b => b.ProductVersions)
                     .Must(ru => ru.All(u => u != null && u.UpdateNumber != null && u.UpdateNumber >= 0))
                     .When(ru => ru.ProductVersions != null)
                     .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                     .WithMessage("Product Versions update number cannot be less than zero or null.");
            });
        }

        Task<ValidationResult> IProductDataProductVersionsValidator.Validate(ProductDataProductVersionsRequest request)
        {
            return ValidateAsync(request);
        }
    }
}
