using FluentValidation;
using FluentValidation.Results;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
            RuleFor(v => v.CallbackUri).NotNull().When(v => !string.IsNullOrEmpty(v.CallbackUri))
            .Matches(@"^http(s)?://([\w-]+.)+[\w-]+(/[\w- ./?%&=])?$")
            .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                 .WithMessage("Invalid CallbackUri format.");
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
