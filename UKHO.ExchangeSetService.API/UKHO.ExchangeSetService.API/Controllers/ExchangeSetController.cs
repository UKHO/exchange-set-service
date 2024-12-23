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
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.Controllers
{
    [Route("v2/exchangeSet")]
    public class ExchangeSetController : ExchangeSetBaseController<ExchangeSetController>
    {
        private readonly ILogger<ExchangeSetController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IExchangeSetStandardService _exchangeSetService;

        public ExchangeSetController(
            IHttpContextAccessor httpContextAccessor,
            ILogger<ExchangeSetController> logger,
            IExchangeSetStandardService exchangeSetService
            ) : base(httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exchangeSetService = exchangeSetService ?? throw new ArgumentNullException(nameof(exchangeSetService));
        }

        [HttpPost("{exchangeSetStandard}/updatesSince")]
        public virtual Task<IActionResult> PostUpdatesSince(string exchangeSetStandard, [FromBody] UpdatesSinceRequest updatesSinceRequest, [FromQuery] string productIdentifier, [FromQuery] string callbackUri)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(EventIds.PostUpdatesSinceRequestStarted, EventIds.PostUpdatesSinceRequestCompleted,
                "UpdatesSince endpoint request for X-Correlation-ID : {correlationId} and ExchangeSetStandard : {exchangeSetStandard}",
                async () =>
                {
                    if (updatesSinceRequest == null)
                    {
                        return BadRequestErrorResponse();
                    }

                    updatesSinceRequest.ProductIdentifier = productIdentifier;
                    updatesSinceRequest.CallbackUri = callbackUri;

                    var result = await _exchangeSetService.CreateUpdatesSince(updatesSinceRequest, GetCorrelationId(), GetRequestCancellationToken());

                    return result.ToActionResult(_httpContextAccessor);

                }, GetCorrelationId(), exchangeSetStandard);
        }
    }
}
