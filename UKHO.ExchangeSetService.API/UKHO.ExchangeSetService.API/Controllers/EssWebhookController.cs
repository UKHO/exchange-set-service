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
        [Route("/Options")]
        public IActionResult Options()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var webhookRequestOrigin = HttpContext.Request.Headers["WebHook-Request-Origin"].FirstOrDefault();
                ////var webhookRequestCallback = HttpContext.Request.Headers["WebHook-Request-Callback"];
                ////var webhookRequestRate = HttpContext.Request.Headers["WebHook-Request-Rate"];
                HttpContext.Response.Headers.Add("WebHook-Allowed-Rate", "*");
                HttpContext.Response.Headers.Add("WebHook-Allowed-Origin", webhookRequestOrigin);
            }

            return Ok();
        }

        [HttpPost]
        [Route("/PostEssWebhook")]
        public virtual async Task<IActionResult> PostEssWebhook([FromBody] JObject request)
        {
            var eventGridEvent = JsonConvert.DeserializeObject<CustomEventGridEvent>(request.ToString());
            var data = (eventGridEvent.Data as JObject).ToObject<EnterpriseEventCacheDataRequest>();

            Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadEventStart.ToEventId(), "Clear Cache Event started for Data:{data} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(data), data, GetCurrentCorrelationId());

            var validationResult = await essWebhookService.ValidateEventGridCacheDataRequest(data);

            if (!validationResult.IsValid)
            {
                Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadValidationEvent.ToEventId(), "Clear Cache Search and Download Event- BusinessUnit and Attributes are null and _X-Correlation-ID:{correlationId}", GetCurrentCorrelationId());
                return GetCacheResponse();
            }

            await essWebhookService.DeleteSearchAndDownloadCacheData(data, GetCurrentCorrelationId());

            Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadEventCompleted.ToEventId(), "Clear Cache Event completed for ProductName:{} with OK response and _X-Correlation-ID:{correlationId}", data.BatchId, GetCurrentCorrelationId());

            return GetCacheResponse();
        }
    }
}