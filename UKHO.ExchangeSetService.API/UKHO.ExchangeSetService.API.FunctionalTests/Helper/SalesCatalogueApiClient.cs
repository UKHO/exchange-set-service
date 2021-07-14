using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class SalesCatalogueApiClient
    {
        static HttpClient httpClient = new HttpClient();
        private readonly string apiHost;

        public SalesCatalogueApiClient(string apiHost)
        {
            this.apiHost = apiHost;
        }

        /// <summary>
        /// Get latest baseline data for a specified set of ENCs. - POST /productData/productVersions
        /// </summary>
        /// <param name="productType"></param>
        /// <param name="catalogueType">callbackUri, pass NULL to skip call back notification</param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GeScsCatalogueAsync(string productType, string catalogueType, string accessToken = null)
        {
            string uri = $"{apiHost}/v1/productData/{productType}/catalogue/{catalogueType}";
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                if (accessToken != null)
                {
                    httpRequestMessage.SetBearerToken(accessToken);
                }
                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }

        public async Task<HttpResponseMessage> GetProductIdentifiersAsync(string productType, List<string> productIdentifiers, string accessToken = null)
        {
            string uri = $"{apiHost}/v1/productData/{productType}/products/productIdentifiers";

            string payloadJson = JsonConvert.SerializeObject(productIdentifiers);

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") })
            {
                if (accessToken != null)
                {
                    httpRequestMessage.SetBearerToken(accessToken);
                }

                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }

        }
    }
}
