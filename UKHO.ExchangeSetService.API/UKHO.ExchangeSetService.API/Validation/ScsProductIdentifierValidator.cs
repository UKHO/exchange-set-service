using FluentValidation;
using FluentValidation.Results;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.Validation
{
    public interface IScsProductIdentifierValidator
    {
        Task<ValidationResult> Validate(ScsProductIdentifierRequest scsProductIdentifierRequest);
    }

    public class ScsProductIdentifierValidator : AbstractValidator<ScsProductIdentifierRequest>, IScsProductIdentifierValidator
    {
        public ScsProductIdentifierValidator()
        {
            RuleFor(p => p.ProductIdentifier)
               .Must(pi => pi != null && pi.All(u => !string.IsNullOrWhiteSpace(u)))
               .WithErrorCode(HttpStatusCode.BadRequest.ToString())
               .WithMessage("productIdentifiers cannot be null or empty.");

        }
        Task<ValidationResult> IScsProductIdentifierValidator.Validate(ScsProductIdentifierRequest scsProductIdentifierRequest)
        {
            return ValidateAsync(scsProductIdentifierRequest);
        }
    }
}
