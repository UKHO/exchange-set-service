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
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.Controllers
{
    [ApiController]
    [Authorize]
    public class EssWebhookController : BaseController<EssWebhookController>
    {
        private readonly IEssWebhookService essWebhookService;

        public EssWebhookController(IHttpContextAccessor contextAccessor,
                                    ILogger<EssWebhookController> logger,
                                    IEssWebhookService essWebhookService)
        : base(contextAccessor, logger)
        {
            this.essWebhookService = essWebhookService;
        }

        [HttpOptions]
        [Route("webhook/newfilespublished")]
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
        [Route("webhook/newfilespublished")]
        public virtual async Task<IActionResult> NewFilesPublished([FromBody] JObject request)
        {
            var eventGridEvent = JsonConvert.DeserializeObject<CustomEventGridEvent>(request.ToString());
            var data = (eventGridEvent.Data as JObject).ToObject<EnterpriseEventCacheDataRequest>();

            Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadEventStart.ToEventId(), "Clear Cache Event started for Data:{data} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(data), GetCurrentCorrelationId());

            var validationResult = await essWebhookService.ValidateEventGridCacheDataRequest(data);

            if (!validationResult.IsValid)
            {
                Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadValidationEvent.ToEventId(), "Invalid data i.e.BusinessUnit:{data.BusinessUnit} or Attributes are null in payload from Enterprise event for Clear Cache Search and Download Event and _X-Correlation-ID:{correlationId}", data.BusinessUnit, GetCurrentCorrelationId());
                Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadEventCompleted.ToEventId(), "Clear Cache Event completed for ProductName:{} with OK response and _X-Correlation-ID:{correlationId}", data.BatchId, GetCurrentCorrelationId());
                return GetCacheResponse();
            }

            await essWebhookService.DeleteSearchAndDownloadCacheData(data, GetCurrentCorrelationId());

            Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadEventCompleted.ToEventId(), "Clear Cache Event completed for ProductName:{} with OK response and _X-Correlation-ID:{correlationId}", data.BatchId, GetCurrentCorrelationId());

            return GetCacheResponse();
        }
    }
}