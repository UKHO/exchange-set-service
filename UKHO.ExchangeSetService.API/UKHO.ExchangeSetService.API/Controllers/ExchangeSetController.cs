using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.API.Controllers
{
    [Route("v2/exchangeSet")]
    public class ExchangeSetController : ExchangeSetControllerBase<ExchangeSetController>
    {
        private readonly ILogger<ExchangeSetController> _logger;
        private readonly IExchangeSetService _exchangeSetService;

        public ExchangeSetController(
            IHttpContextAccessor httpContextAccessor,
            ILogger<ExchangeSetController> logger,
            IExchangeSetService exchangeSetService
            ) : base(httpContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exchangeSetService = exchangeSetService ?? throw new ArgumentNullException(nameof(exchangeSetService));
        }

        [HttpPost("{exchangeSetStandard}/productNames")]
        public Task<IActionResult> ProductNames([FromBody] string[] productNames, [FromQuery] string callbackUri)
        {
            return _logger.LogStartEndAndElapsedTimeAsync<ExchangeSetController, IActionResult>(
                EventIds.ESSPostProductIdentifiersRequestStart,
                EventIds.ESSPostProductIdentifiersRequestCompleted,
                "Product Identifiers Endpoint request for _X-Correlation-ID:{correlationId}",
                async () =>
                {
                    var result = await _exchangeSetService.CreateProductDataByProductNames(productNames, callbackUri, GetCorrelationId());

                    return result.StatusCode switch
                    {
                        HttpStatusCode.Accepted => Accepted(result.Value),
                        HttpStatusCode.BadRequest => BadRequest(result.ErrorDescription),
                        _ => StatusCode((int)result.StatusCode)
                    };
                },
                GetCorrelationId());
        }
    }
}
