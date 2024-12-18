// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

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

        [HttpPost("{exchangeSetStandard}/updatesSince")]
        public virtual Task<IActionResult> PostUpdatesSince(string exchangeSetStandard, [FromBody] UpdatesSinceRequest updatesSinceRequest, [FromQuery] string productIdentifier, [FromQuery] string callbackUri)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(EventIds.ESSGetProductsFromSpecificDateRequestStart, EventIds.ESSGetProductsFromSpecificDateRequestCompleted,
                "UpdatesSince Endpoint request for _X-Correlation-ID : {correlationId} and ExchangeSetStandard : {exchangeSetStandard}",
                async () =>
                {
                    updatesSinceRequest.ProductIdentifier = productIdentifier;
                    updatesSinceRequest.CallbackUri = callbackUri;

                    var result = await _exchangeSetService.CreateUpdateSince(updatesSinceRequest, GetCorrelationId(), GetRequestCancellationToken());

                    return result.ToActionResult();

                }, GetCorrelationId(), exchangeSetStandard);
        }
    }
}
