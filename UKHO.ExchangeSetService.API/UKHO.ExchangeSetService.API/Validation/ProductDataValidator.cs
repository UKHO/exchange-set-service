using FluentValidation;
using FluentValidation.Results;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.Validation
{
    public interface IProductDataValidator
    {
        Task<ValidationResult> Validate(ProductVersionsRequest request);
    }
    public class ProductDataValidator : AbstractValidator<ProductVersionsRequest>, IProductDataValidator
    {
        public ProductDataValidator()
        {
            RuleFor(v => v.CallbackUri).NotNull().When(v => !string.IsNullOrEmpty(v.CallbackUri))
            .Matches(@"^http(s)?://([\w-]+.)+[\w-]+(/[\w- ./?%&=])?$")
            .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                 .WithMessage("Invalid Call back Uri format.");
            RuleFor(b => b.ProductVersions)
                 .Must(ru => ru.All(u => !string.IsNullOrWhiteSpace(u.ProductName)))
                 .When(ru => ru.ProductVersions != null)
                 .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                 .WithMessage("Product Versions product name cannot be blank or null.");
        }

        Task<ValidationResult> IProductDataValidator.Validate(ProductVersionsRequest request)
        {
            return ValidateAsync(request);
        }
    }

    public class ProductVersionsValidator : AbstractValidator<ProductVersionRequest>
    {
        public ProductVersionsValidator()
        {
            RuleFor(v => v.ProductName).NotEmpty().NotNull()
            .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                 .WithMessage("Product Name cannot be blank or null.");
            RuleFor(v => v.EditionNumber).GreaterThanOrEqualTo(0)
            .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                 .WithMessage("Edition Number cannot be less than 0.");
            RuleFor(v => v.UpdateNumber).GreaterThanOrEqualTo(0)
            .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                 .WithMessage("Update Number cannot be less than 0.");
        }
    }
}
