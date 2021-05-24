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
        private readonly IAuthTokenProvider authTokenProvider;
        private readonly ISalesCatalogueClient salesCatalogueClient;
        private readonly IOptions<SalesCatalogueConfiguration> salesCatalogueConfig;

        public SalesCatalogueService(ISalesCatalogueClient salesCatalogueClient,
                                     ILogger<SalesCatalogueService> logger,
                                     IAuthTokenProvider authTokenProvider,
                                     IOptions<SalesCatalogueConfiguration> salesCatalogueConfig)
        {
            this.logger = logger;
            this.authTokenProvider = authTokenProvider;
            this.salesCatalogueConfig = salesCatalogueConfig;
            this.salesCatalogueClient = salesCatalogueClient;
        }

        public async Task<SalesCatalogueResponse> GetProductsFromSpecificDateAsync(string sinceDateTime)
        {
            logger.LogInformation(EventIds.SCSGetProductsFromSpecificDateRequestStart.ToEventId(),$"Get sales catalogue service from specific date time started");
            
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(salesCatalogueConfig.Value.ResourceId);
            var uri = $"/{salesCatalogueConfig.Value.Version}/productData/{salesCatalogueConfig.Value.ProductType}/products?sinceDateTime={sinceDateTime}";

            var httpResponse = await salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Get, null, accessToken, uri);

            SalesCatalogueResponse response = await CreateSalesCatalogueServiceResponse(httpResponse);

            logger.LogInformation(EventIds.SCSGetProductsFromSpecificDateRequestCompleted.ToEventId(),$"Get sales catalogue service from specific date time completed");
            return response; 
        }

        public async Task<SalesCatalogueResponse> PostProductIdentifiersAsync(List<string> ProductIdentifiers)
        {
            logger.LogInformation(EventIds.SCSPostProductIdentifiersRequestStart.ToEventId(), $"Post sales catalogue service for ProductIdentifiers Started");

            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(salesCatalogueConfig.Value.ResourceId);
            var uri = $"/{salesCatalogueConfig.Value.Version}/productData/{salesCatalogueConfig.Value.ProductType}/products/productIdentifiers";

            string payloadJson = JsonConvert.SerializeObject(ProductIdentifiers);

            var httpResponse = await salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Post, payloadJson, accessToken, uri);

            SalesCatalogueResponse response = await CreateSalesCatalogueServiceResponse(httpResponse);

            logger.LogInformation(EventIds.SCSPostProductIdentifiersRequestCompleted.ToEventId(), $"Post sales catalogue service for ProductIdentifiers completed");
            return response;
        }

        public async Task<SalesCatalogueResponse> PostProductVersionsAsync(List<ProductVersionRequest> productVersions)
        {
            logger.LogInformation(EventIds.SCSPostProductVersionsRequestStart.ToEventId(), $"Post sales catalouge service for ProductVersions started");

            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(salesCatalogueConfig.Value.ResourceId);
            var uri = $"/{salesCatalogueConfig.Value.Version}/productData/{salesCatalogueConfig.Value.ProductType}/products/productVersions";

            string payloadJson = JsonConvert.SerializeObject(productVersions);

            var httpResponse = await salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Post, payloadJson, accessToken, uri);

            SalesCatalogueResponse response = await CreateSalesCatalogueServiceResponse(httpResponse);
            
            logger.LogInformation(EventIds.SCSPostProductVersionsRequestCompleted.ToEventId(), $"Post sales catalogue service for ProductVersions completed");
            return response;
        }

        private async Task<SalesCatalogueResponse> CreateSalesCatalogueServiceResponse(HttpResponseMessage httpResponse)
        {
            var response = new SalesCatalogueResponse();
            var body = await httpResponse.Content.ReadAsStringAsync();
            if (httpResponse.StatusCode != HttpStatusCode.OK && httpResponse.StatusCode != HttpStatusCode.NotModified)
            {
                logger.LogError(EventIds.SalesCatalogueNonOkResponse.ToEventId(), $"Sales catalougue service with uri {httpResponse.RequestMessage.RequestUri} and responded with {httpResponse.StatusCode} and message {body}");
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
    }
}
