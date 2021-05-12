using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class SalesCatalogueService: ISalesCatalougeService
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<SalesCatalogueService> logger;
        private readonly IAuthTokenProvider authTokenProvider;
        private const string ProductType = "encs57";
        private const String Version = "v1";
        public SalesCatalogueService(HttpClient httpClient,
                                     ILogger<SalesCatalogueService> logger,
                                     IAuthTokenProvider authTokenProvider)
        {
            this.httpClient = httpClient;
            this.logger = logger;
            this.authTokenProvider = authTokenProvider;
        }

        public async Task<SalesCatalougeResponse> GetProductsFromSpecificDateAsync(string sinceDateTime)
        {
            logger.LogInformation($"Get Sales Catalogue service from SinceDateTime Started");
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync("abc");
            var uri = $"/{Version}/productData/{ProductType}/products?sinceDateTime={sinceDateTime}";
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var httpResponse = await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);

            var response = new SalesCatalougeResponse
            {
                ResponseCode = httpResponse.StatusCode
            };

            string bodyJson = await httpResponse.Content.ReadAsStringAsync();
            response.ResponseBody = JsonConvert.DeserializeObject<SalesCatalogueProductResponse>(bodyJson);
            logger.LogInformation($"Get Sales Catalogue service from SinceDateTime Started");
            return response; 
        }

        public Task<SalesCatalougeResponse> PostProductIdentifiersAsync(List<string> ProductIdentifiers)
        {
            throw new NotImplementedException();
        }

        public Task<SalesCatalougeResponse> PostProductVersionsAsync(List<ProductVersionRequest> productVersions)
        {
            throw new NotImplementedException();
        }
    }
}
