using FluentValidation;
using FluentValidation.Results;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.Validation
{
    public interface IEnterpriseEventCacheDataRequestValidator
    {
        Task<ValidationResult> Validate(EnterpriseEventCacheDataRequest enterpriseEventCacheDataRequest);
    }

    [ExcludeFromCodeCoverage]
    public class EnterpriseEventCacheDataRequestValidator : AbstractValidator<EnterpriseEventCacheDataRequest>, IEnterpriseEventCacheDataRequestValidator
    {
        public EnterpriseEventCacheDataRequestValidator()
        {
            RuleFor(v => v.BusinessUnit).NotEmpty().NotNull().Must(ru => !string.IsNullOrWhiteSpace(ru))
               .When(ru => ru != null)
               .WithErrorCode(HttpStatusCode.OK.ToString());

            RuleFor(b => b.Attributes)
              .Must(at => at.All(a => !string.IsNullOrWhiteSpace(a.Key) && !string.IsNullOrWhiteSpace(a.Value)))
              .When(ru => ru.Attributes != null)
              .WithErrorCode(HttpStatusCode.OK.ToString());
        }
        Task<ValidationResult> IEnterpriseEventCacheDataRequestValidator.Validate(EnterpriseEventCacheDataRequest enterpriseEventCacheDataRequest)
        {
            return ValidateAsync(enterpriseEventCacheDataRequest);
        }
    }
}
