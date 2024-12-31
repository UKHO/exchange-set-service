// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.ExchangeSetService.Common.Configuration.V2;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Response;
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
                                                     _salesCatalogueConfig.Value.Version,
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
                    response.LastModified = httpResponse.Content.Headers.LastModified?.UtcDateTime;
                    return ServiceResponseResult<SalesCatalogueResponse>.Success(response);

                case HttpStatusCode.NotModified:
                    return ServiceResponseResult<SalesCatalogueResponse>.NotModified();

                case HttpStatusCode.NoContent:
                    return ServiceResponseResult<SalesCatalogueResponse>.NoContent();

                case HttpStatusCode.BadRequest:
                    _logger.LogError(EventIds.SalesCatalogueServiceNonOkResponse.ToEventId(),
                                     "Error in sales catalogue service with uri:{RequestUri} and responded with error:{Error} | statuscode:{StatusCode} | _X-Correlation-ID:{CorrelationId}",
                                     httpResponse.RequestMessage.RequestUri,
                                     body,
                                     httpResponse.StatusCode,
                                     correlationId);

                    var errorDescription = new ErrorDescription
                    {
                        CorrelationId = correlationId,
                        Errors =
                        [
                            new Error
                            {
                                Source = "Sales catalogue service",
                                Description = body
                            }
                        ]
                    };

                    return ServiceResponseResult<SalesCatalogueResponse>.BadRequest(errorDescription);

                default:
                    _logger.LogError(EventIds.SalesCatalogueServiceNonOkResponse.ToEventId(),
                                     "Error in sales catalogue service with uri:{RequestUri} and responded with statuscode:{StatusCode} | _X-Correlation-ID:{CorrelationId}",
                                     httpResponse.RequestMessage.RequestUri,
                                     httpResponse.StatusCode,
                                     correlationId);

                    return ServiceResponseResult<SalesCatalogueResponse>.InternalServerError();
            }
        }
    }
}
