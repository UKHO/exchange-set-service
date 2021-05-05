using FluentValidation;
using FluentValidation.Results;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.Validation
{
    public interface IProductIdentifierValidator
    {
        Task<ValidationResult> Validate(ProductIdentifierRequest productIdentifierRequest);
    }

    public class ProductIdentifierValidator : AbstractValidator<ProductIdentifierRequest>, IProductIdentifierValidator
    {       
        public ProductIdentifierValidator()
        {
            RuleFor(p => p.ProductIdentifier)
               .Must(pi => pi != null && pi.All(u => !string.IsNullOrWhiteSpace(u)))
               .WithErrorCode(HttpStatusCode.BadRequest.ToString())
               .WithMessage("Product Identifiers cannot be null or empty.");           

            RuleFor(x => x.CallbackUri)
                .Must(x => x.IsValidCallbackUri()).When(x => !string.IsNullOrEmpty(x.CallbackUri))
                .WithMessage("Invalid Callback Uri format.")
                .WithErrorCode(HttpStatusCode.BadRequest.ToString());
        }
        Task<ValidationResult> IProductIdentifierValidator.Validate(ProductIdentifierRequest productIdentifierRequest)
        {
            return ValidateAsync(productIdentifierRequest);
        }       
    }    
}
