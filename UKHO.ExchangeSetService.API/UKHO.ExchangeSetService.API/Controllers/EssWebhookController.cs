using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.Controllers
{
    [ApiController]
    [Authorize]
    public class EssWebhookController : BaseController<EssWebhookController>
    {
        private readonly IEssWebhookService essWebhookService;
        private readonly IAzureAdB2CHelper azureAdB2CHelper;

        public EssWebhookController(IHttpContextAccessor contextAccessor,
                                    ILogger<EssWebhookController> logger,
                                    IEssWebhookService essWebhookService, IAzureAdB2CHelper azureAdB2CHelper)
        : base(contextAccessor, logger)
        {
            this.essWebhookService = essWebhookService;
            this.azureAdB2CHelper = azureAdB2CHelper;
        }

        [HttpOptions]
        [Route("/webhook/newfilespublished")]
        public IActionResult NewFilesPublishedOptions()
        {
            var webhookRequestOrigin = HttpContext.Request.Headers["WebHook-Request-Origin"].FirstOrDefault();

            Logger.LogInformation(EventIds.NewFilesPublishedWebhookOptionsCallStarted.ToEventId(), "Started processing the Options request for the New Files Published event webhook for WebHook-Request-Origin:{webhookRequestOrigin}", webhookRequestOrigin);

            HttpContext.Response.Headers.Add("WebHook-Allowed-Rate", "*");
            HttpContext.Response.Headers.Add("WebHook-Allowed-Origin", webhookRequestOrigin);

            Logger.LogInformation(EventIds.NewFilesPublishedWebhookOptionsCallCompleted.ToEventId(), "Completed processing the Options request for the New Files Published event webhook for WebHook-Request-Origin:{webhookRequestOrigin}", webhookRequestOrigin);

            return GetCacheResponse();
        }

        [HttpPost]
        [Route("/webhook/newfilespublished")]
        public virtual async Task<IActionResult> NewFilesPublished([FromBody] JObject request)
        {
            Logger.LogInformation(EventIds.ESSUpsertSearchAndDownloadCacheEventStart.ToEventId(), "Clear Cache Event started for _X-Correlation-ID:{correlationId}", GetCurrentCorrelationId());
            var azureAdB2C = new AzureAdB2C
            {
                AudToken = TokenAudience,
                IssToken = TokenIssuer
            };

            if (azureAdB2CHelper.IsAzureB2CUser(azureAdB2C, GetCurrentCorrelationId()))
            {
                Logger.LogInformation(EventIds.ESSB2CUserValidationEvent.ToEventId(), "Event was triggered with invalid Azure AD token from Enterprise event for Clear Cache Search and Download Event for _X-Correlation-ID:{correlationId}", GetCurrentCorrelationId());
                Logger.LogInformation(EventIds.ESSUpsertSearchAndDownloadCacheEventCompleted.ToEventId(), "Clear Cache Event completed as Azure AD Authentication failed with OK response and _X-Correlation-ID:{correlationId}", GetCurrentCorrelationId());
                return GetCacheResponse();
            }

            var eventGridEvent = new CustomEventGridEvent();
            JsonConvert.PopulateObject(request.ToString(), eventGridEvent);
            var data = (eventGridEvent.Data as JObject).ToObject<EnterpriseEventCacheDataRequest>();

            Logger.LogInformation(EventIds.ESSUpsertSearchAndDownloadCacheEventStart.ToEventId(), "Enterprise Event data deserialized in ESS and Data:{data} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(data), GetCurrentCorrelationId());

            var validationResult = await essWebhookService.ValidateEventGridCacheDataRequest(data);

            var productName = data.Attributes.Where(a => a.Key == "CellName").Select(a => a.Value).FirstOrDefault();

            if (!validationResult.IsValid)
            {
                Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadValidationEvent.ToEventId(), "Required attributes missing in event data from Enterprise event for Clear Cache Search and Download Event for _X-Correlation-ID:{correlationId}", GetCurrentCorrelationId());
                Logger.LogInformation(EventIds.ESSUpsertSearchAndDownloadCacheEventCompleted.ToEventId(), "Clear Cache Event completed for ProductName:{productName} as required data was missing in payload with OK response and _X-Correlation-ID:{correlationId}", productName, GetCurrentCorrelationId());
                return GetCacheResponse();
            }

            await essWebhookService.UpsertSearchAndDownloadCacheData(data, GetCurrentCorrelationId());

            Logger.LogInformation(EventIds.ESSUpsertSearchAndDownloadCacheEventCompleted.ToEventId(), "Clear Cache Event completed for ProductName:{productName} of BusinessUnit:{businessUnit} with OK response and _X-Correlation-ID:{correlationId}", productName, data.BusinessUnit, GetCurrentCorrelationId());

            return GetCacheResponse();
        }
    }
}