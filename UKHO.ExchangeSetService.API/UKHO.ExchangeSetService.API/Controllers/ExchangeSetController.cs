using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models;

namespace UKHO.ExchangeSetService.API.Controllers
{
    [Route("v2/exchangeSet")]
    public class ExchangeSetController : ExchangeSetControllerBase<ExchangeSetController>
    {
        private readonly ILogger<ExchangeSetController> _logger;
        private readonly IExchangeSetStandardService _exchangeSetStandardService;

        public ExchangeSetController(
            IHttpContextAccessor httpContextAccessor,
            ILogger<ExchangeSetController> logger,
            IExchangeSetStandardService exchangeSetStandardService
            ) : base(httpContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exchangeSetStandardService = exchangeSetStandardService ?? throw new ArgumentNullException(nameof(exchangeSetStandardService));
        }

        [HttpPost("{exchangeSetStandard}/productNames")]
        public Task<IActionResult> PostProductNames(string exchangeSetStandard, [FromBody] string[] productNames, [FromQuery] string callbackUri)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(
                EventIds.ESSPostProductNamesRequestStart,
                EventIds.ESSPostProductNamesRequestCompleted,
                "Product Names Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}",
                async () =>
                {
                    var result = await _exchangeSetStandardService.CreateProductDataByProductNames(productNames, callbackUri, GetCorrelationId());

                    return result.ToActionResult();
                },
                GetCorrelationId(), exchangeSetStandard);
        }
    }
}
