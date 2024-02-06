using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class ExchangeSetApiClient
    {
        public static HttpClient httpClient = new();
        private readonly string apiHost;

        public ExchangeSetApiClient(string apiHost)
        {
            this.apiHost = apiHost;
        }

        /// <summary>
        /// Provide all the releasable data after a datetime. - POST /productData
        /// </summary>
        /// <param name="sincedateTime">The date and time from which changes are requested which follows RFC1123 format</param>
        /// <param name="callbackUri">callbackUri, pass NULL to skip call back notification</param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <param name="isUnencrypted"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetExchangeSetBasedOnDateTimeAsync(string sincedateTime = null, string callbackUri = null, string accessToken = null, string isUnencrypted= "false")
        {
            var uri = $"{apiHost}/productData";
            
            uri += $"?isUnencrypted={isUnencrypted}";

            if (sincedateTime != null)
            {
                uri += $"&sinceDateTime={sincedateTime}";
            }
            if (callbackUri != null)
            {
                uri += $"&callbackuri={callbackUri}";
            }

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            if (accessToken != null)
            {
                httpRequestMessage.SetBearerToken(accessToken);
            }

            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        /// <summary>
        /// Get latest baseline data for a specified set of ENCs. - POST /productData/productVersions
        /// </summary>
        /// <param name="productVersionModel"></param>
        /// <param name="callbackUri">callbackUri, pass NULL to skip call back notification</param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <param name="isUnencrypted"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetProductVersionsAsync(List<ProductVersionModel> productVersionModel, string callbackUri = null, string accessToken = null, string isUnencrypted = "false")
        {
            var uri = $"{apiHost}/productData/productVersions";
            if (callbackUri != null)
            {
                uri += $"?callbackuri={callbackUri}&isUnencrypted={isUnencrypted}";
            }
            else
            {
                uri += $"?isUnencrypted={isUnencrypted}";
            }
            var payloadJson = JsonConvert.SerializeObject(productVersionModel);

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
                { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") };
            if (accessToken != null)
            {
                httpRequestMessage.SetBearerToken(accessToken);
            }
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        /// <summary>
        /// Get latest baseline data for a specified set of ENCs. with isUnencrypted parameter - POST /productData/productIdentifiers
        /// </summary>
        /// <param name="productIdentifierModel"></param>
        /// <param name="callbackUri">callbackUri, pass NULL to skip call back notification</param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <param name="isUnencrypted"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetProductIdentifiersDataAsync(List<string> productIdentifierModel, string callbackUri = null, string accessToken = null, string isUnencrypted="false")
        {
            var uri = $"{apiHost}/productData/productIdentifiers";
            if (callbackUri != null)
            {
                uri += $"?callbackuri={callbackUri}&isUnencrypted={isUnencrypted}";
            }
            else
            {
                uri += $"?isUnencrypted={isUnencrypted}";
            }
            var payloadJson = JsonConvert.SerializeObject(productIdentifierModel);

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
        
                { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") };
            if (accessToken != null)
            {
                httpRequestMessage.SetBearerToken(accessToken);
            }

            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        /// <summary>
        /// Get latest baseline data for a specified set of ENCs. without isUnencrypted parameter - POST /productData/productIdentifiers
        /// </summary>
        /// <param name="productIdentifierModel"></param>
        /// <param name="callbackUri">callbackUri, pass NULL to skip call back notification</param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetProductIdentifiersDataWithoutIsUnencryptedParameterAsync(List<string> productIdentifierModel, string callbackUri = null, string accessToken = null)
        {
            var uri = $"{apiHost}/productData/productIdentifiers";
            if (callbackUri != null)
            {
                uri += $"?callbackuri={callbackUri}";
            }
           
            var payloadJson = JsonConvert.SerializeObject(productIdentifierModel);

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)

                { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") };
            if (accessToken != null)
            {
                httpRequestMessage.SetBearerToken(accessToken);
            }

            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        /// <summary>
        /// Get latest baseline data for a specified set of ENCs. without isUnencrypted parameter  - POST /productData/productVersions
        /// </summary>
        /// <param name="productVersionModel"></param>
        /// <param name="callbackUri">callbackUri, pass NULL to skip call back notification</param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetProductVersionsWithoutIsUnencryptedParameterAsync(List<ProductVersionModel> productVersionModel, string callbackUri = null, string accessToken = null)
        {
            var uri = $"{apiHost}/productData/productVersions";
            if (callbackUri != null)
            {
                uri += $"?callbackuri={callbackUri}";
            }
           
            var payloadJson = JsonConvert.SerializeObject(productVersionModel);

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
                { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") };
            if (accessToken != null)
            {
                httpRequestMessage.SetBearerToken(accessToken);
            }
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        /// <summary>
        /// Provide all the releasable data after a datetime.  without isUnencrypted parameter - POST /productData
        /// </summary>
        /// <param name="sincedateTime">The date and time from which changes are requested which follows RFC1123 format</param>
        /// <param name="callbackUri">callbackUri, pass NULL to skip call back notification</param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetExchangeSetBasedOnDateTimeWithoutIsUnencryptedParameterAsync(string sincedateTime = null, string callbackUri = null, string accessToken = null)
        {
            var uri = $"{apiHost}/productData";

            if (sincedateTime != null)
            {
                uri += $"?sinceDateTime={sincedateTime}";
            }
            if (callbackUri != null)
            {
                uri += $"&callbackuri={callbackUri}";
            }

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            if (accessToken != null)
            {
                httpRequestMessage.SetBearerToken(accessToken);
            }

            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        public async Task<HttpResponseMessage> PostNewFilesPublishedAsync([FromBody] JObject request, string accessToken = null)
        {
            var uri = $"{apiHost}/webhook/newfilespublished";
            var payloadJson = JsonConvert.SerializeObject(request);
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
                { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") };
            if (accessToken != null)
            {
                httpRequestMessage.SetBearerToken(accessToken);
            }

            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}
