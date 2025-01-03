// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers.V2
{
    public class SalesCatalogueService : ISalesCatalogueService
    {
        private readonly ILogger<SalesCatalogueService> _logger;
        private readonly IAuthScsTokenProvider _authScsTokenProvider;
        private readonly ISalesCatalogueClient _salesCatalogueClient;
        private readonly IOptions<SalesCatalogueConfiguration> _salesCatalogueConfig;
        private readonly IUriHelper _uriHelper;

        private const string SCSUpdateSinceURL = "/{0}/products/{1}/updatesSince?sinceDateTime={2}";

        public SalesCatalogueService(ILogger<SalesCatalogueService> logger,
                                     IAuthScsTokenProvider authScsTokenProvider,
                                     ISalesCatalogueClient salesCatalogueClient,
                                     IOptions<SalesCatalogueConfiguration> salesCatalogueConfig,
                                     IUriHelper uriHelper)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            _authScsTokenProvider = authScsTokenProvider ?? throw new ArgumentNullException(nameof(authScsTokenProvider));
            _salesCatalogueClient = salesCatalogueClient ?? throw new ArgumentNullException(nameof(salesCatalogueClient));
            _salesCatalogueConfig = salesCatalogueConfig ?? throw new ArgumentNullException(nameof(salesCatalogueConfig));
            _uriHelper = uriHelper ?? throw new ArgumentNullException(nameof(uriHelper));
        }

        private async Task<ServiceResponseResult<SalesCatalogueResponse>> HandleSalesCatalogueServiceResponseAsync(HttpResponseMessage httpResponse, string correlationId)
        {
            var body = await httpResponse.Content.ReadAsStringAsync();
            var response = new SalesCatalogueResponse
            {
                ResponseCode = httpResponse.StatusCode,
                ScsRequestDateTime = httpResponse.Headers.Date?.UtcDateTime ?? DateTime.UtcNow
            };

            switch (httpResponse.StatusCode)
            {
                case HttpStatusCode.OK:
                    response.ResponseBody = JsonConvert.DeserializeObject<SalesCatalogueProductResponse>(body);
                    return ServiceResponseResult<SalesCatalogueResponse>.Success(response);

                case HttpStatusCode.NotModified:
                    response.LastModified = httpResponse.Content.Headers.LastModified?.UtcDateTime;
                    _logger.LogInformation(EventIds.SalesCatalogueServiceNonOkResponse.ToEventId(),
                                     "Content is already up to date, no new content available in sales catalogue service with uri:{RequestUri} | statuscode:{StatusCode} | _X-Correlation-ID:{CorrelationId}",
                                     httpResponse.RequestMessage.RequestUri,
                                     httpResponse.StatusCode,
                                     correlationId);

                    return ServiceResponseResult<SalesCatalogueResponse>.NotModified(response);

                case HttpStatusCode.BadRequest:
                    _logger.LogError(EventIds.SalesCatalogueServiceNonOkResponse.ToEventId(),
                                     "Error in sales catalogue service with uri:{RequestUri} and responded with error:{Error} | statuscode:{StatusCode} | _X-Correlation-ID:{CorrelationId}",
                                     httpResponse.RequestMessage.RequestUri,
                                     body,
                                     httpResponse.StatusCode,
                                     correlationId);

                    return ServiceResponseResult<SalesCatalogueResponse>.BadRequest();

                case HttpStatusCode.NotFound:
                    _logger.LogError(EventIds.SalesCatalogueServiceNonOkResponse.ToEventId(),
                                     "Error in sales catalogue service with uri:{RequestUri} and responded with error:{Error} | statuscode:{StatusCode} | _X-Correlation-ID:{CorrelationId}",
                                     httpResponse.RequestMessage.RequestUri,
                                     body,
                                     httpResponse.StatusCode,
                                     correlationId);

                    return ServiceResponseResult<SalesCatalogueResponse>.NotFound();

                default:
                    _logger.LogError(EventIds.SalesCatalogueServiceNonOkResponse.ToEventId(),
                                     "Error in sales catalogue service with uri:{RequestUri} and responded with error:{Error} | statuscode:{StatusCode} | _X-Correlation-ID:{CorrelationId}",
                                     httpResponse.RequestMessage.RequestUri,
                                     body,
                                     httpResponse.StatusCode,
                                     correlationId);

                    return ServiceResponseResult<SalesCatalogueResponse>.InternalServerError();
            }
        }
    }
}
