// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

        [HttpPost("{exchangeSetStandard}/productVersions")]
        public virtual Task<IActionResult> PostProductVersions([FromBody] IEnumerable<ProductVersionRequest> productVersionRequest, [FromQuery] string callbackUri, string exchangeSetStandard)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(EventIds.PostProductVersionsRequestStart, EventIds.PostProductVersionsRequestCompleted,
                "ProductVersions endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}",
                async () =>
                {
                    var productVersionsRequest = new ProductVersionsRequest
                    {
                        ProductVersions = productVersionRequest,
                        CallbackUri = callbackUri,
                        CorrelationId = GetCorrelationId()
                    };

                    var result = await _exchangeSetStandardService.CreateExchangeSetByProductVersions(productVersionsRequest, GetRequestCancellationToken());
                    return result.ToActionResult();

                }, GetCorrelationId(), exchangeSetStandard);
        }
    }
}
