using FluentValidation;
using FluentValidation.Results;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.Validation
{
    public interface IEventGridCacheDataRequestValidator
    {
        Task<ValidationResult> Validate(EventGridCacheDataRequest eventGridCacheDataRequest);
    }

    public class EventGridCacheDataRequestValidator : AbstractValidator<EventGridCacheDataRequest>, IEventGridCacheDataRequestValidator
    {
        public EventGridCacheDataRequestValidator()
        {
            RuleFor(v => v.BusinessUnit).NotEmpty().NotNull().Must(ru => !string.IsNullOrWhiteSpace(ru))
               .When(ru => ru != null)
               .WithErrorCode(HttpStatusCode.BadRequest.ToString())
               .WithMessage("businessUnit cannot be blank or null.");

            RuleFor(b => b.Attributes)
              .Must(at => at.All(a => !string.IsNullOrWhiteSpace(a.Key) && !string.IsNullOrWhiteSpace(a.Value)))
              .When(ru => ru.Attributes != null)
              .WithErrorCode(HttpStatusCode.BadRequest.ToString())
              .WithMessage("attribute key & value cannot be blank.");
        }
        Task<ValidationResult> IEventGridCacheDataRequestValidator.Validate(EventGridCacheDataRequest eventGridCacheDataRequest)
        {
            return ValidateAsync(eventGridCacheDataRequest);
        }
    }
}
