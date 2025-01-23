// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.Controllers.V2
{
    [Authorize]
    [ServiceFilter(typeof(ExchangeSetAuthorizationFilterAttribute))]
    [Route("v2/exchangeSet/{exchangeSetStandard}")]
    public class ExchangeSetController : ExchangeSetBaseController<ExchangeSetController>
    {
        private readonly string _correlationId;
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
            _correlationId = GetCorrelationId();
        }

        /// <summary>
        /// Provide all the latest releasable baseline data for a specified set of product names.
        /// </summary>
        /// <remarks>
        /// Given a list of product name, return all the versions of the products that are releasable and that are needed to bring the products up to date.
        /// </remarks>
        /// <param name="exchangeSetStandard">The standard of the Exchange Set.</param>
        /// <param name="productNames">The JSON body containing product names.</param>
        /// <param name="callbackUri">An optional callback URI for notification once the Exchange Set is ready.</param>
        /// <response code="202">A JSON body that indicates the URL that the Exchange Set will be available on as well as the number of cells in that Exchange Set.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="401">Unauthorized - either you have not provided any credentials, or your credentials are not recognised.</response>
        /// <response code="403">Forbidden - you have been authorised, but you are not allowed to access this resource.</response>
        /// <response code="429">Too Many Requests - You have sent too many requests in a given amount of time. Please back-off for the time in the Retry-After header (in seconds) and try again.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpPost("productNames")]
        public Task<IActionResult> PostProductNames(string exchangeSetStandard, [FromBody] string[] productNames, [FromQuery] string callbackUri)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(
                EventIds.PostProductNamesRequestStart,
                EventIds.PostProductNamesRequestCompleted,
                "Product Names Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}",
                async () =>
                {
                    var result = await _exchangeSetStandardService.ProcessProductNamesRequestAsync(productNames, ApiVersion.V2, exchangeSetStandard, callbackUri, _correlationId, GetRequestCancellationToken());

                    return result.ToActionResult(_httpContextAccessor, _correlationId);
                },
                _correlationId, exchangeSetStandard);
        }

        /// <summary>
        /// Provide all the latest releasable baseline data for a specified set of product versions (e.g. Edition x Update y).
        /// </summary>
        /// <remarks>
        /// Given a list of product names and their edition and update numbers, return all the versions of the products that are releasable from that version onwards and that are needed to bring the products up to date.
        /// </remarks>
        /// <param name="exchangeSetStandard">The standard of the Exchange Set.</param>
        /// <param name="productVersionRequest">The JSON body containing product names and their edition and update numbers.</param>
        /// <param name="callbackUri">An optional callback URI for notification once the Exchange Set is ready.</param>
        /// <response code="202">A JSON body that indicates the URL that the Exchange Set will be available on as well as the number of cells in that Exchange Set.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="401">Unauthorized - either you have not provided any credentials, or your credentials are not recognised.</response>
        /// <response code="403">Forbidden - you have been authorised, but you are not allowed to access this resource.</response>
        /// <response code="429">Too Many Requests - You have sent too many requests in a given amount of time. Please back-off for the time in the Retry-After header (in seconds) and try again.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpPost("productVersions")]
        public Task<IActionResult> PostProductVersions(string exchangeSetStandard, [FromBody] IEnumerable<ProductVersionRequest> productVersionRequest, [FromQuery] string callbackUri)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(
                EventIds.ESSPostProductVersionsRequestStart,
                EventIds.ESSPostProductVersionsRequestCompleted,
                "ProductVersions V2 endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}",
                async () =>
                {
                    var result = await _exchangeSetStandardService.ProcessProductVersionsRequestAsync(productVersionRequest, ApiVersion.V2, exchangeSetStandard, callbackUri, _correlationId, GetRequestCancellationToken());

                    return result.ToActionResult(_httpContextAccessor, _correlationId);

                }, _correlationId, exchangeSetStandard);
        }

        /// <summary>
        /// Provide all the releasable data after a specified datetime.
        /// </summary>
        /// <remarks>
        /// Given a datetime, build an Exchange Set of all the releasable product versions that have been issued since that datetime.
        /// </remarks>
        /// <param name="exchangeSetStandard">The standard of the Exchange Set.</param>
        /// <param name="updatesSinceRequest" example="2024-12-20T11:51:00.000Z">The JSON body containing the sinceDateTime parameter. It returns all the releasable data after that date and time from which changes are requested. Any changes since the date will be returned. The date is in ISO 8601 format. The date and time must be within 28 days and cannot be future date.</param>
        /// <param name="productIdentifier">An optional product identifier for filtering the updates.</param>
        /// <param name="callbackUri">An optional callback URI for notification once the Exchange Set is ready.</param>
        /// <response code="202">A JSON body that indicates the URL that the Exchange Set will be available on as well as the number of cells in that Exchange Set.</response>
        /// <response code="304">Not Modified - - The requested resource has not been modified.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="401">Unauthorized - either you have not provided any credentials, or your credentials are not recognised.</response>
        /// <response code="403">Forbidden - you have been authorised, but you are not allowed to access this resource.</response>
        /// <response code="429">Too Many Requests - You have sent too many requests in a given amount of time. Please back-off for the time in the Retry-After header (in seconds) and try again.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpPost("updatesSince")]
        public Task<IActionResult> PostUpdatesSince(string exchangeSetStandard, [FromBody] UpdatesSinceRequest updatesSinceRequest, [FromQuery] string productIdentifier, [FromQuery] string callbackUri)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(
                EventIds.PostUpdatesSinceRequestStarted,
                EventIds.PostUpdatesSinceRequestCompleted,
                "UpdatesSince endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}",
                async () =>
                {
                    var result = await _exchangeSetStandardService.ProcessUpdatesSinceRequestAsync(updatesSinceRequest, ApiVersion.V2, exchangeSetStandard, productIdentifier, callbackUri, _correlationId, GetRequestCancellationToken());

                    return result.ToActionResult(_httpContextAccessor, _correlationId);

                }, _correlationId, exchangeSetStandard);
        }
    }
}
