using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Text;
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
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var webhookRequestOrigin = HttpContext.Request.Headers["WebHook-Request-Origin"].FirstOrDefault();
                HttpContext.Response.Headers.Add("WebHook-Allowed-Rate", "*");
                HttpContext.Response.Headers.Add("WebHook-Allowed-Origin", webhookRequestOrigin);
            }
            return GetCacheResponse();
        }

        [HttpPost]
        [Route("/webhook/newfilespublished")]
        public virtual async Task<IActionResult> NewFilesPublished([FromBody] JObject request)
        {
            Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadEventStart.ToEventId(), "Clear Cache Event started for _X-Correlation-ID:{correlationId}", GetCurrentCorrelationId());
            var azureAdB2C = new AzureAdB2C()
            {
                AudToken = TokenAudience,
                IssToken = TokenIssuer
            };
            bool isAzureB2C = azureAdB2CHelper.IsAzureB2CUser(azureAdB2C, GetCurrentCorrelationId());
            if (isAzureB2C)
            {
                Logger.LogInformation(EventIds.ESSB2CUserValidationEvent.ToEventId(), "Event was triggered with invalid Azure AD token from Enterprise event for Clear Cache Search and Download Event for _X-Correlation-ID:{correlationId}", GetCurrentCorrelationId());
                Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadEventCompleted.ToEventId(), "Clear Cache Event completed as Azure AD Authentication failed with OK response and _X-Correlation-ID:{correlationId}", GetCurrentCorrelationId());
                return GetCacheResponse();
            }

            var eventGridEvent = JsonConvert.DeserializeObject<CustomEventGridEvent>(request.ToString());
            var data = (eventGridEvent.Data as JObject).ToObject<EnterpriseEventCacheDataRequest>();

            Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadEventStart.ToEventId(), "Enterprise Event data deserialized in ESS and Data:{data} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(data), GetCurrentCorrelationId());
           
            var validationResult = await essWebhookService.ValidateEventGridCacheDataRequest(data);

            var productName = data.Attributes.Where(a => a.Key == "CellName").Select(a => a.Value).FirstOrDefault();

            if (!validationResult.IsValid)
            {
                Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadValidationEvent.ToEventId(), "Required attributes missing in event data from Enterprise event for Clear Cache Search and Download Event for _X-Correlation-ID:{correlationId}", GetCurrentCorrelationId());
                Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadEventCompleted.ToEventId(), "Clear Cache Event completed for ProductName:{productName} as required data was missing in payload with OK response and _X-Correlation-ID:{correlationId}", productName, GetCurrentCorrelationId());
                return GetCacheResponse();
            }

            await essWebhookService.DeleteSearchAndDownloadCacheData(data, GetCurrentCorrelationId());

            Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadEventCompleted.ToEventId(), "Clear Cache Event completed for ProductName:{productName} with OK response and _X-Correlation-ID:{correlationId}", productName, GetCurrentCorrelationId());

            return GetCacheResponse();
        }
    }
}