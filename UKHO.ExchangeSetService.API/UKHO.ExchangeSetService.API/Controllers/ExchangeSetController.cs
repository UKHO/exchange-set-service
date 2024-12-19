// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.Controllers
{
    [Route("v2/exchangeSet")]
    public class ExchangeSetController : ExchangeSetBaseController<ExchangeSetController>
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

        [HttpPost]
        [Route("{exchangeSetStandard}/productVersions")]
        public Task<IActionResult> PostProductVersions([FromBody] IEnumerable<ProductVersionRequest> productVersionRequest, [FromQuery] string callbackUri, string exchangeSetStandard)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(EventIds.ESSPostProductVersionsRequestStart, EventIds.ESSPostProductVersionsRequestCompleted,
                "Product Versions Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}",
                async () =>
                {
                    var productVersionsRequest = new ProductVersionsRequest
                    {
                        ProductVersions = productVersionRequest,
                        CallbackUri = callbackUri,
                        CorrelationId = GetCorrelationId()
                    };

                    var result = await _exchangeSetService.CreateExchangeSetByProductVersions(productVersionsRequest, GetRequestCancellationToken());
                    return result.ToActionResult();

                }, GetCorrelationId(), exchangeSetStandard);
        }
    }
}
