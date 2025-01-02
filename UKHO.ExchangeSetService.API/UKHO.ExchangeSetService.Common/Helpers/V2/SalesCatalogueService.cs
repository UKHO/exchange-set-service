// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.Common.Helpers.V2
{
    public class SalesCatalogueService : ISalesCatalogueService
    {
        private const string SaleCatalogueServiceProductVersion = "v2";
        private readonly ILogger<SalesCatalogueService> _logger;
        private readonly IAuthScsTokenProvider _authScsTokenProvider;
        private readonly ISalesCatalogueClient _salesCatalogueClient;
        private readonly IOptions<SalesCatalogueConfiguration> _salesCatalogueConfig;
        private readonly IUriHelper _uriHelper;

        private const string SCSUpdateSinceURL = "/{0}/products/{1}/updatesSince?sinceDateTime={2}";
        private const string SCSProductVersionsURL = "/{0}/products/{1}/productVersions";

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

        public Task<ServiceResponseResult<SalesCatalogueResponse>> GetProductsFromSpecificDateAsync(string exchangeSetStandard, string sinceDateTime, string correlationId)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(
                EventIds.SCSGetProductsFromSpecificDateRequestStart,
                EventIds.SCSGetProductsFromSpecificDateRequestCompleted,
                "Get sales catalogue service from specific date time for _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    var uri = _uriHelper.CreateUri(_salesCatalogueConfig.Value.BaseUrl,
                                                     SCSUpdateSinceURL,
                                                     correlationId,
                                                     SaleCatalogueServiceProductVersion,
                                                     exchangeSetStandard,
                                                     sinceDateTime);

                    var accessToken = await _authScsTokenProvider.GetManagedIdentityAuthAsync(_salesCatalogueConfig.Value.ResourceId);

                    var httpResponse = await _salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Get, null, accessToken, uri.AbsoluteUri);

                    return await CreateSalesCatalogueServiceResponse(httpResponse, correlationId);
                },
                correlationId);
        }

        private async Task<ServiceResponseResult<SalesCatalogueResponse>> CreateSalesCatalogueServiceResponse(HttpResponseMessage httpResponse, string correlationId)
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

        public Task<ServiceResponseResult<SalesCatalogueResponse>> PostProductVersionsAsync(IEnumerable<ProductVersionRequest> productVersions, string exchangeSetStandard, string correlationId)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(
                EventIds.SCSPostProductVersionsV2RequestStarted,
                EventIds.SCSPostProductVersionsV2RequestCompleted,
                "SalesCatalogueService PostProductVersions endpoint request for _X-Correlation-ID:{correlationId}",
                async () =>
                {

                    var uri = _uriHelper.CreateUri(_salesCatalogueConfig.Value.BaseUrl,
                                                     SCSProductVersionsURL,
                                                     _salesCatalogueConfig.Value.Version,
                                                     exchangeSetStandard);

                    var accessToken = await _authScsTokenProvider.GetManagedIdentityAuthAsync(_salesCatalogueConfig.Value.ResourceId);

                    string payloadJson = JsonConvert.SerializeObject(productVersions);

                    var httpResponse = await _salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Post, payloadJson, accessToken, uri.AbsoluteUri);

                    return await CreateSalesCatalogueServiceResponse(httpResponse, correlationId);

                },
                correlationId);
        }
    }
}
