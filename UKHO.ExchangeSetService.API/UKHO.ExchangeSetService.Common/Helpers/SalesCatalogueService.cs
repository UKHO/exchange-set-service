using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class SalesCatalogueService: ISalesCatalogueService
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<SalesCatalogueService> logger;
        private readonly IAuthTokenProvider authTokenProvider;
        private readonly IOptions<SalesCatalogueConfiguration> salesCatalogueConfig;

        public SalesCatalogueService(HttpClient httpClient,
                                     ILogger<SalesCatalogueService> logger,
                                     IAuthTokenProvider authTokenProvider,
                                     IOptions<SalesCatalogueConfiguration> salesCatalogueConfig)
        {
            this.httpClient = httpClient;
            this.logger = logger;
            this.authTokenProvider = authTokenProvider;
            this.salesCatalogueConfig = salesCatalogueConfig;
        }

        public async Task<SalesCatalogueResponse> GetProductsFromSpecificDateAsync(string sinceDateTime)
        {
            logger.LogInformation(EventIds.SCSGetAllProductRequestStart.ToEventId(),$"Get sales catalogue service from specific date time started");
            
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(salesCatalogueConfig.Value.ResourceId);
            var uri = $"/{salesCatalogueConfig.Value.Version}/productData/{salesCatalogueConfig.Value.ProductType}/products?sinceDateTime={sinceDateTime}";
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var httpResponse = await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);

            var response = new SalesCatalogueResponse();
            var body = await httpResponse.Content.ReadAsStringAsync();
            if (httpResponse.StatusCode != HttpStatusCode.OK && httpResponse.StatusCode != HttpStatusCode.NotModified)
            
            {
                logger.LogError(EventIds.SalesCatalogueNonOkResponse.ToEventId(), $"Sales catalougue service responded with {httpResponse.StatusCode} and message {body}");
                response.ResponseCode = httpResponse.StatusCode;
                response.ResponseBody = null;
            }
            else
            {
                response.ResponseCode = httpResponse.StatusCode;
                response.ResponseBody = JsonConvert.DeserializeObject<SalesCatalogueProductResponse>(body);
            }

            logger.LogInformation(EventIds.SCSGetAllProductRequestCompleted.ToEventId(),$"Get sales catalogue service from specific date time completed");
            return response; 
        }

        public async Task<SalesCatalogueResponse> PostProductIdentifiersAsync(List<string> ProductIdentifiers)
        {
            logger.LogInformation(EventIds.SCSPostProductIdentifiersRequestStart.ToEventId(), $"Post sales catalogue service for ProductIdentifiers Started");
            var response = new SalesCatalogueResponse();
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(salesCatalogueConfig.Value.ResourceId);
            var uri = $"/{salesCatalogueConfig.Value.Version}/productData/{salesCatalogueConfig.Value.ProductType}/products/productIdentifiers";

            string payloadJson = JsonConvert.SerializeObject(ProductIdentifiers);

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") })
            {
                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var httpResponse = await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);

                var body = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode != HttpStatusCode.OK && httpResponse.StatusCode != HttpStatusCode.NotModified)

                {
                    logger.LogError(EventIds.SalesCatalogueNonOkResponse.ToEventId(), $"Sales catalougue service responded with {httpResponse.StatusCode} and message {body}");
                    response.ResponseCode = httpResponse.StatusCode;
                    response.ResponseBody = null;
                }
                else
                {
                    response.ResponseCode = httpResponse.StatusCode;
                    response.ResponseBody = JsonConvert.DeserializeObject<SalesCatalogueProductResponse>(body);
                }
            }

            logger.LogInformation(EventIds.SCSPostProductIdentifiersRequestCompleted.ToEventId(), $"Post sales catalogue service for ProductIdentifiers completed");
            return response;
        }

        public async Task<SalesCatalogueResponse> PostProductVersionsAsync(List<ProductVersionRequest> productVersions)
        {
            logger.LogInformation(EventIds.SCSPostProductVersionsRequestStart.ToEventId(), $"Post sales catalouge service for ProductVersions started");
            var response = new SalesCatalogueResponse();
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(salesCatalogueConfig.Value.ResourceId);
            var uri = $"/{salesCatalogueConfig.Value.Version}/productData/{salesCatalogueConfig.Value.ProductType}/products/productVersions";

            string payloadJson = JsonConvert.SerializeObject(productVersions);

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") })
            {
                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var httpResponse = await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);

                var body = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode != HttpStatusCode.OK && httpResponse.StatusCode != HttpStatusCode.NotModified)

                {
                    logger.LogError(EventIds.SalesCatalogueNonOkResponse.ToEventId(), $"Sales catalouge service responded with {httpResponse.StatusCode} and message {body}");
                    response.ResponseCode = httpResponse.StatusCode;
                    response.ResponseBody = null;
                }
                else
                {
                    response.ResponseCode = httpResponse.StatusCode;
                    response.ResponseBody = JsonConvert.DeserializeObject<SalesCatalogueProductResponse>(body);
                }
            }


            logger.LogInformation(EventIds.SCSPostProductVersionsRequestCompleted.ToEventId(), $"Post sales catalogue service for ProductVersions completed");
            return response;
        }
    }
}
