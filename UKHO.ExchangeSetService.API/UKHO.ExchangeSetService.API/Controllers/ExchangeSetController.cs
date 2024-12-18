// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        public Task<IActionResult> PostUpdatesSince([FromBody] string sinceDateTime, [FromQuery] string productIdentifier, [FromQuery] string callbackUri)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(EventIds.ESSGetProductsFromSpecificDateRequestStart, EventIds.ESSGetProductsFromSpecificDateRequestCompleted,
                "Product Data SinceDateTime Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}",
                async () =>
                {
                    var updatesSinceRequest = new UpdatesSinceRequest()
                    {
                        SinceDateTime = sinceDateTime,
                        CallbackUri = callbackUri,
                        ProductIdentifier = productIdentifier,
                        CorrelationId = GetCorrelationId()
                    };

                    var result = await _exchangeSetService.CreateUpdateSince(updatesSinceRequest);

                    return result.StatusCode switch
                    {
                        HttpStatusCode.OK => StatusCode((int)HttpStatusCode.Accepted, result.Value),
                        HttpStatusCode.BadRequest => BadRequest(result.ErrorDescription.Errors),
                        _ => (IActionResult)StatusCode((int)result.StatusCode)
                    };

                }, GetCorrelationId());
        }
    }
}
