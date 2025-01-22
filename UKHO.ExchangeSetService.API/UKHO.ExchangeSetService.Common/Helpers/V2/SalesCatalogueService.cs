// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue.V2;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.Common.Helpers.V2
{
    public class SalesCatalogueService : ISalesCatalogueService
    {
        private readonly ILogger<SalesCatalogueService> _logger;
        private readonly IAuthScsTokenProvider _authScsTokenProvider;
        private readonly ISalesCatalogueClient _salesCatalogueClient;
        private readonly IOptions<SalesCatalogueConfiguration> _salesCatalogueConfig;
        private readonly IUriFactory _uriFactory;

        private const string ProductNamesEndpointPathFormat = "/{0}/products/{1}/productNames";
        private const string ScsProductVersionsEndpointPathFormat = "/{0}/products/{1}/productVersions";
        private const string ScsUpdateSinceEndpointPathFormat = "/{0}/products/{1}/updatesSince?sinceDateTime={2}&productIdentifier={3}";

        public SalesCatalogueService(ILogger<SalesCatalogueService> logger,
            IAuthScsTokenProvider authScsTokenProvider,
            ISalesCatalogueClient salesCatalogueClient,
            IOptions<SalesCatalogueConfiguration> salesCatalogueConfig,
            IUriFactory uriFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            _authScsTokenProvider = authScsTokenProvider ?? throw new ArgumentNullException(nameof(authScsTokenProvider));
            _salesCatalogueClient = salesCatalogueClient ?? throw new ArgumentNullException(nameof(salesCatalogueClient));
            _salesCatalogueConfig = salesCatalogueConfig ?? throw new ArgumentNullException(nameof(salesCatalogueConfig));
            _uriFactory = uriFactory ?? throw new ArgumentNullException(nameof(uriFactory));
        }

        /// <summary>
        /// Posts the product names to the sales catalogue service and returns the response.
        /// </summary>
        /// <param name="apiVersion">The API version to be used.</param>
        /// <param name="productType">The standard of the Exchange Set.</param>
        /// <param name="productNames">The list of product names to be posted.</param>
        /// <param name="correlationId">Guid based id for tracking the request.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <returns>Sales Catalogue Service response.</returns>
        public Task<ServiceResponseResult<V2SalesCatalogueResponse>> PostProductNamesAsync(ApiVersion apiVersion, string productType, IEnumerable<string> productNames, string correlationId, CancellationToken cancellationToken)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(
                EventIds.SCSPostProductNamesRequestStart,
                EventIds.SCSPostProductNamesRequestCompleted,
                "Post sales catalogue service for ProductNames for _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    var uri = _uriFactory.CreateUri(_salesCatalogueConfig.Value.BaseUrl,
                        ProductNamesEndpointPathFormat,
                        correlationId,
                        apiVersion,
                        productType);

                    var accessToken = await _authScsTokenProvider.GetManagedIdentityAuthAsync(_salesCatalogueConfig.Value.ResourceId);

                    var payloadJson = JsonConvert.SerializeObject(productNames);

                    var httpResponse = await _salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Post, payloadJson, accessToken, uri.AbsoluteUri, correlationId, cancellationToken);

                    return await HandleSalesCatalogueServiceResponseAsync(httpResponse, correlationId, cancellationToken);
                },
                correlationId);
        }

        /// <summary>
        /// Posts the product versions to the sales catalogue service and returns the response.
        /// </summary>
        /// <param name="apiVersion">The API version to be used.</param>
        /// <param name="productType">The standard of the Exchange Set.</param>
        /// <param name="productVersions">The list of product versions to be posted.</param>
        /// <param name="correlationId">Guid based id for tracking the request.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <returns>Sales Catalogue Service response.</returns>
        public Task<ServiceResponseResult<V2SalesCatalogueResponse>> PostProductVersionsAsync(ApiVersion apiVersion, string productType, IEnumerable<ProductVersionRequest> productVersions, string correlationId, CancellationToken cancellationToken)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(
                EventIds.SCSPostProductVersionsRequestStart,
                EventIds.SCSPostProductVersionsRequestCompleted,
                "SalesCatalogueService PostProductVersions V2 endpoint request for _X-Correlation-ID:{correlationId}",
                async () =>
                {
                    var uri = _uriFactory.CreateUri(_salesCatalogueConfig.Value.BaseUrl,
                        ScsProductVersionsEndpointPathFormat,
                        correlationId,
                        apiVersion,
                        productType);

                    var accessToken = await _authScsTokenProvider.GetManagedIdentityAuthAsync(_salesCatalogueConfig.Value.ResourceId);

                    var payloadJson = JsonConvert.SerializeObject(productVersions);

                    var httpResponse = await _salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Post, payloadJson, accessToken, uri.AbsoluteUri, correlationId, cancellationToken);

                    return await HandleSalesCatalogueServiceResponseAsync(httpResponse, correlationId, cancellationToken);
                },
                correlationId);
        }

        /// <summary>
        /// Gets the products from the sales catalogue service since the specified date and returns the response.
        /// </summary>
        /// <param name="apiVersion">The API version to be used.</param>
        /// <param name="productType">The standard of the Exchange Set.</param>
        /// <param name="updatesSinceRequest">The request containing the sinceDateTime parameter.</param>
        /// <param name="correlationId">Guid based id for tracking the request.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <returns>Sales Catalogue Service response.</returns>
        public Task<ServiceResponseResult<V2SalesCatalogueResponse>> GetProductsFromUpdatesSinceAsync(ApiVersion apiVersion, string productType, UpdatesSinceRequest updatesSinceRequest, string correlationId, CancellationToken cancellationToken)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(
                EventIds.SCSGetProductsFromSpecificDateRequestStart,
                EventIds.SCSGetProductsFromSpecificDateRequestCompleted,
                "Get sales catalogue service from specific date time for _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    var uri = _uriFactory.CreateUri(_salesCatalogueConfig.Value.BaseUrl,
                        ScsUpdateSinceEndpointPathFormat,
                        correlationId,
                        apiVersion,
                        productType,
                        updatesSinceRequest.SinceDateTime,
                        updatesSinceRequest.ProductIdentifier);

                    var accessToken = await _authScsTokenProvider.GetManagedIdentityAuthAsync(_salesCatalogueConfig.Value.ResourceId);

                    var httpResponse = await _salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Get, null, accessToken, uri.AbsoluteUri, correlationId, cancellationToken);

                    return await HandleSalesCatalogueServiceResponseAsync(httpResponse, correlationId, cancellationToken);
                },
                correlationId);
        }

        /// <summary>
        /// Handles the response from the sales catalogue service and returns the service response result.
        /// </summary>
        /// <param name="httpResponse">The HTTP response from the sales catalogue service.</param>
        /// <param name="correlationId">Guid based id for tracking the request.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <returns>Sales Catalogue Service response result with status code.</returns>
        private async Task<ServiceResponseResult<V2SalesCatalogueResponse>> HandleSalesCatalogueServiceResponseAsync(HttpResponseMessage httpResponse, string correlationId, CancellationToken cancellationToken)
        {
            var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            var response = new V2SalesCatalogueResponse
            {
                ResponseCode = httpResponse.StatusCode,
                ScsRequestDateTime = httpResponse.Headers.Date?.UtcDateTime ?? DateTime.UtcNow
            };

            switch (httpResponse.StatusCode)
            {
                case HttpStatusCode.OK:
                    response.ResponseBody = JsonConvert.DeserializeObject<V2SalesCatalogueProductResponse>(body);
                    response.LastModified = httpResponse.Content.Headers.LastModified?.UtcDateTime;
                    return ServiceResponseResult<V2SalesCatalogueResponse>.Success(response);

                case HttpStatusCode.NotModified:
                    response.LastModified = httpResponse.Content.Headers.LastModified?.UtcDateTime;
                    _logger.LogInformation(EventIds.SalesCatalogueServiceNonOkResponse.ToEventId(),
                        "Content is already up to date, no new content available in sales catalogue service with uri:{RequestUri} | statusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}",
                        httpResponse.RequestMessage.RequestUri,
                        httpResponse.StatusCode,
                        correlationId);

                    return ServiceResponseResult<V2SalesCatalogueResponse>.NotModified(response);

                case HttpStatusCode.BadRequest:
                    _logger.LogError(EventIds.SalesCatalogueServiceNonOkResponse.ToEventId(),
                        "Error in sales catalogue service with uri:{RequestUri} and responded with error:{Error} | statusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}",
                        httpResponse.RequestMessage.RequestUri,
                        body,
                        httpResponse.StatusCode,
                        correlationId);

                    var responseBody = JsonConvert.DeserializeObject<ErrorDescription>(body);
                    return ServiceResponseResult<V2SalesCatalogueResponse>.BadRequest(responseBody);

                case HttpStatusCode.NotFound:
                    _logger.LogError(EventIds.SalesCatalogueServiceNonOkResponse.ToEventId(),
                        "Error in sales catalogue service with uri:{RequestUri} and responded with error:{Error} | statusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}",
                        httpResponse.RequestMessage.RequestUri,
                        body,
                        httpResponse.StatusCode,
                        correlationId);

                    var errorBody = JsonConvert.DeserializeObject<ErrorResponse>(body);
                    return ServiceResponseResult<V2SalesCatalogueResponse>.NotFound(errorBody);

                default:
                    _logger.LogError(EventIds.SalesCatalogueServiceNonOkResponse.ToEventId(),
                        "Error in sales catalogue service with uri:{RequestUri} and responded with error:{Error} | statusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}",
                        httpResponse.RequestMessage.RequestUri,
                        body,
                        httpResponse.StatusCode,
                        correlationId);

                    return ServiceResponseResult<V2SalesCatalogueResponse>.InternalServerError();
            }
        }
    }
}
