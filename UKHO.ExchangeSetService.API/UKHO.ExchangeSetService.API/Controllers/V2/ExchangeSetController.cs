﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.API.Filters.V2;
using UKHO.ExchangeSetService.API.Services.V2;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.Controllers.V2
{
    [Authorize]
    [ServiceFilter(typeof(ExchangeSetAuthorizationFilterAttribute))]
    [Route("v2/exchangeSet")]
    public class ExchangeSetController : ExchangeSetBaseController<ExchangeSetController>
    {
        private readonly ILogger<ExchangeSetController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IExchangeSetStandardService _exchangeSetStandardService;

        public ExchangeSetController(
            IHttpContextAccessor httpContextAccessor,
            ILogger<ExchangeSetController> logger,
            IExchangeSetStandardService exchangeSetStandardService
            ) : base(httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exchangeSetStandardService = exchangeSetStandardService ?? throw new ArgumentNullException(nameof(exchangeSetStandardService));
        }

        [HttpPost("{exchangeSetStandard}/productNames")]
        public Task<IActionResult> PostProductNames(string exchangeSetStandard, [FromBody] string[] productNames, [FromQuery] string callbackUri)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(
                EventIds.PostProductNamesRequestStart,
                EventIds.PostProductNamesRequestCompleted,
                "Product Names Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}",
                async () =>
                {
                    var result = await _exchangeSetStandardService.ProcessProductNamesRequest(productNames, callbackUri, GetCorrelationId(), GetRequestCancellationToken());

                    return result.ToActionResult(_httpContextAccessor);
                },
                GetCorrelationId(), exchangeSetStandard);
        }

        [HttpPost("{exchangeSetStandard}/productVersions")]
        public Task<IActionResult> PostProductVersions(string exchangeSetStandard, [FromBody] IEnumerable<ProductVersionRequest> productVersionRequest, [FromQuery] string callbackUri)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(EventIds.PostProductVersionsRequestStart, EventIds.PostProductVersionsRequestCompleted,
                "ProductVersions endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}",
                async () =>
                {
                    var result = await _exchangeSetStandardService.ProcessProductVersionsRequest(productVersionRequest, callbackUri, GetCorrelationId(), GetRequestCancellationToken());

                    return result.ToActionResult(_httpContextAccessor);

                }, GetCorrelationId(), exchangeSetStandard);
        }

        [HttpPost("{exchangeSetStandard}/updatesSince")]
        public Task<IActionResult> PostUpdatesSince(string exchangeSetStandard, [FromBody] UpdatesSinceRequest updatesSinceRequest, [FromQuery] string productIdentifier, [FromQuery] string callbackUri)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(
                EventIds.PostUpdatesSinceRequestStarted,
                EventIds.PostUpdatesSinceRequestCompleted,
                "UpdatesSince endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}",
                async () =>
                {
                    var result = await _exchangeSetStandardService.ProcessUpdatesSinceRequest(updatesSinceRequest, productIdentifier, callbackUri, GetCorrelationId(), GetRequestCancellationToken());

                    return result.ToActionResult(_httpContextAccessor);

                }, GetCorrelationId(), exchangeSetStandard);
        }
    }
}