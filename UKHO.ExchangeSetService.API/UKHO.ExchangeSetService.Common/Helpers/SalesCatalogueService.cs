using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class SalesCatalogueService : ISalesCatalogueService
    {
        private readonly ILogger<SalesCatalogueService> logger;
        private readonly IAuthScsTokenProvider authScsTokenProvider;
        private readonly ISalesCatalogueClient salesCatalogueClient;
        private readonly IOptions<SalesCatalogueConfiguration> salesCatalogueConfig;

        public SalesCatalogueService(ISalesCatalogueClient salesCatalogueClient,
                                     ILogger<SalesCatalogueService> logger,
                                     IAuthScsTokenProvider authScsTokenProvider,
                                     IOptions<SalesCatalogueConfiguration> salesCatalogueConfig)
        {
            this.logger = logger;
            this.authScsTokenProvider = authScsTokenProvider;
            this.salesCatalogueConfig = salesCatalogueConfig;
            this.salesCatalogueClient = salesCatalogueClient;
        }

        public async Task<SalesCatalogueResponse> GetProductsFromSpecificDateAsync(string sinceDateTime, string correlationId)
        {
            logger.LogInformation(EventIds.SCSGetProductsFromSpecificDateRequestStart.ToEventId(), "Get sales catalogue service from specific date time started for _X-Correlation-ID:{CorrelationId}", correlationId);

            var accessToken = await authScsTokenProvider.GetManagedIdentityAuthAsync(salesCatalogueConfig.Value.ResourceId);
            var uri = $"/{salesCatalogueConfig.Value.Version}/productData/{salesCatalogueConfig.Value.ProductType}/products?sinceDateTime={sinceDateTime}";

            var httpResponse = await salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Get, null, accessToken, uri);

            SalesCatalogueResponse response = await CreateSalesCatalogueServiceResponse(httpResponse, correlationId);

            logger.LogInformation(EventIds.SCSGetProductsFromSpecificDateRequestCompleted.ToEventId(), "Get sales catalogue service from specific date time completed for _X-Correlation-ID:{CorrelationId}", correlationId);
            return response;
        }

        public async Task<SalesCatalogueResponse> PostProductIdentifiersAsync(List<string> productIdentifiers, string correlationId)
        {
            logger.LogInformation(EventIds.SCSPostProductIdentifiersRequestStart.ToEventId(), "Post sales catalogue service for ProductIdentifiers Started for _X-Correlation-ID:{CorrelationId}", correlationId);

            var accessToken = await authScsTokenProvider.GetManagedIdentityAuthAsync(salesCatalogueConfig.Value.ResourceId);
            var uri = $"/{salesCatalogueConfig.Value.Version}/productData/{salesCatalogueConfig.Value.ProductType}/products/productIdentifiers";

            string payloadJson = JsonConvert.SerializeObject(productIdentifiers);

            var httpResponse = await salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Post, payloadJson, accessToken, uri);

            SalesCatalogueResponse response = await CreateSalesCatalogueServiceResponse(httpResponse, correlationId);

            logger.LogInformation(EventIds.SCSPostProductIdentifiersRequestCompleted.ToEventId(), "Post sales catalogue service for ProductIdentifiers completed for _X-Correlation-ID:{CorrelationId}", correlationId);
            return response;
        }

        public async Task<SalesCatalogueResponse> PostProductVersionsAsync(List<ProductVersionRequest> productVersions, string correlationId)
        {
            logger.LogInformation(EventIds.SCSPostProductVersionsRequestStart.ToEventId(), "Post sales catalogue service for ProductVersions started for _X-Correlation-ID:{CorrelationId}", correlationId);

            var accessToken = await authScsTokenProvider.GetManagedIdentityAuthAsync(salesCatalogueConfig.Value.ResourceId);
            var uri = $"/{salesCatalogueConfig.Value.Version}/productData/{salesCatalogueConfig.Value.ProductType}/products/productVersions";

            string payloadJson = JsonConvert.SerializeObject(productVersions);

            var httpResponse = await salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Post, payloadJson, accessToken, uri);

            SalesCatalogueResponse response = await CreateSalesCatalogueServiceResponse(httpResponse, correlationId);

            logger.LogInformation(EventIds.SCSPostProductVersionsRequestCompleted.ToEventId(), "Post sales catalogue service for ProductVersions completed for _X-Correlation-ID:{CorrelationId}", correlationId);
            return response;
        }

        public async Task<SalesCatalogueDataResponse> GetSalesCatalogueDataResponse(string batchId, string correlationId)
        {
            logger.LogInformation(EventIds.SCSGetSalesCatalogueDataRequestStart.ToEventId(), "Get sales catalogue service for CatalogueData started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);

            var accessToken = await authScsTokenProvider.GetManagedIdentityAuthAsync(salesCatalogueConfig.Value.ResourceId);
            var uri = $"/{salesCatalogueConfig.Value.Version}/productData/{salesCatalogueConfig.Value.ProductType}/catalogue/{salesCatalogueConfig.Value.CatalogueType}";

            var httpResponse = await salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Get, null, accessToken, uri, correlationId);

            SalesCatalogueDataResponse response = await CreateSalesCatalogueDataResponse(httpResponse, batchId, correlationId);

            logger.LogInformation(EventIds.SCSGetSalesCatalogueDataRequestCompleted.ToEventId(), "Get sales catalogue service for CatalogueData completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
            return response;
        }

        private async Task<SalesCatalogueResponse> CreateSalesCatalogueServiceResponse(HttpResponseMessage httpResponse, string correlationId)
        {
            var response = new SalesCatalogueResponse();
            var body = await httpResponse.Content.ReadAsStringAsync();
            if (httpResponse.StatusCode != HttpStatusCode.OK && httpResponse.StatusCode != HttpStatusCode.NotModified)
            {
                logger.LogError(EventIds.SalesCatalogueServiceNonOkResponse.ToEventId(), "Error in sales catalogue service with uri:{RequestUri} and responded with {StatusCode} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, correlationId);
                response.ResponseCode = httpResponse.StatusCode;
                response.ResponseBody = null;
            }
            else
            {
                response.ResponseCode = httpResponse.StatusCode;
                var lastModified = httpResponse.Content.Headers.LastModified;
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    response.ResponseBody = JsonConvert.DeserializeObject<SalesCatalogueProductResponse>(body);
                }
                if (lastModified != null)
                {
                    response.LastModified = ((DateTimeOffset)lastModified).UtcDateTime;
                }
            }

            return response;
        }

        private async Task<SalesCatalogueDataResponse> CreateSalesCatalogueDataResponse(HttpResponseMessage httpResponse, string batchId, string correlationId)
        {
            var response = new SalesCatalogueDataResponse();
            var body = await httpResponse.Content.ReadAsStringAsync();
            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError(EventIds.SalesCatalogueServiceCatalogueDataNonOkResponse.ToEventId(), "Error in sales catalogue service catalogue end point with uri:{RequestUri} responded with {StatusCode} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchId, correlationId);
                response.ResponseCode = httpResponse.StatusCode;
                response.ResponseBody = null;
                throw new FulfilmentException(EventIds.SalesCatalogueServiceCatalogueDataNonOkResponse.ToEventId());
            }
            else
            {
                response.ResponseCode = httpResponse.StatusCode;
                var lastModified = httpResponse.Content.Headers.LastModified;
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    response.ResponseBody = JsonConvert.DeserializeObject<List<SalesCatalogueDataProductResponse>>(body);
                }
                if (lastModified != null)
                {
                    response.LastModified = ((DateTimeOffset)lastModified).UtcDateTime;
                }
            }

            return response;
        }
    }
}
