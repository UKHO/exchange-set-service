using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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
        /// <param name="updatesSinceModel"></param>
        /// <param name="sincedateTime">The date and time from which changes are requested which follows RFC1123 format</param>
        /// <param name="callbackUri">callbackUri, pass NULL to skip call back notification</param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <param name="exchangeSetStandard">exchangeSetStandard, pass s63 or s57 for valid exchange set</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetExchangeSetBasedOnDateTimeAsync(string sincedateTime = null, string callbackUri = null, string accessToken = null, string exchangeSetStandard = "s63", UpdatesSinceModel updatesSinceModel = null)
        {
            var uri = $"{apiHost}/productData";
            var payloadJson = string.Empty;

            if (exchangeSetStandard == "s100")
            {
                uri = $"{apiHost}/v2/exchangeSet/s100/updatesSince";
                payloadJson = JsonConvert.SerializeObject(updatesSinceModel);
            }
            else
            {
                uri += $"?exchangeSetStandard={exchangeSetStandard}";
                if (!string.IsNullOrEmpty(sincedateTime))
                {
                    uri += $"&sinceDateTime={sincedateTime}";
                }
            }

            if (!string.IsNullOrEmpty(callbackUri))
            {
                uri += $"&callbackuri={callbackUri}";
            }

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            if (exchangeSetStandard == "s100")
            {
                httpRequestMessage.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            }

            if (!string.IsNullOrEmpty(accessToken))
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
        /// <param name="exchangeSetStandard">exchangeSetStandard, pass s63 or s57 for valid exchange set</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetProductVersionsAsync(List<ProductVersionModel> productVersionModel, string callbackUri = null, string accessToken = null, string exchangeSetStandard = "s63")
        {
            var uri = $"{apiHost}/productData/productVersions";
            if (callbackUri != null)
            {
                uri += $"?callbackuri={callbackUri}&exchangeSetStandard={exchangeSetStandard}";
            }
            else
            {
                uri += $"?exchangeSetStandard={exchangeSetStandard}";
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
        /// Get latest baseline data for a specified set of ENCs. with exchangeSetStandard parameter - POST /productData/productIdentifiers
        /// </summary>
        /// <param name="productIdentifierModel"></param>
        /// <param name="callbackUri">callbackUri, pass NULL to skip call back notification</param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <param name="exchangeSetStandard">exchangeSetStandard, pass s63 or s57 for valid exchange set</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetProductIdentifiersDataAsync(List<string> productIdentifierModel, string callbackUri = null, string accessToken = null, string exchangeSetStandard = "s63")
        {
            var uri = $"{apiHost}/productData/productIdentifiers";
            if (callbackUri != null)
            {
                uri += $"?callbackuri={callbackUri}&exchangeSetStandard={exchangeSetStandard}";
            }
            else
            {
                uri += $"?exchangeSetStandard={exchangeSetStandard}";
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
        /// Get latest baseline data for a specified set of ENCs. without exchangeSetStandard parameter - POST /productData/productIdentifiers
        /// </summary>
        /// <param name="productIdentifierModel"></param>
        /// <param name="callbackUri">callbackUri, pass NULL to skip call back notification</param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetProductIdentifiersDataWithoutExchangeSetStandardParameterAsync(List<string> productIdentifierModel, string callbackUri = null, string accessToken = null)
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
        /// Get latest baseline data for a specified set of ENCs. without exchangeSetStandard parameter  - POST /productData/productVersions
        /// </summary>
        /// <param name="productVersionModel"></param>
        /// <param name="callbackUri">callbackUri, pass NULL to skip call back notification</param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetProductVersionsWithoutExchangeSetStandardParameterAsync(List<ProductVersionModel> productVersionModel, string callbackUri = null, string accessToken = null)
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
        /// Provide all the releasable data after a datetime.  without exchangeSetStandard parameter - POST /productData
        /// </summary>
        /// <param name="sincedateTime">The date and time from which changes are requested which follows RFC1123 format</param>
        /// <param name="callbackUri">callbackUri, pass NULL to skip call back notification</param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetExchangeSetBasedOnDateTimeWithoutExchangeSetStandardParameterAsync(string sincedateTime = null, string callbackUri = null, string accessToken = null)
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


        /// <summary>
        /// Get latest baseline data for a specified set of ENCs. with exchangeSetStandard parameter - POST /productData/productIdentifiers
        /// </summary>
        /// <param name="productIdentifierModel"></param>
        /// <param name="callbackUri">callbackUri, pass NULL to skip call back notification</param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <param name="exchangeSetStandard">exchangeSetStandard, pass s63 for s63 standard exchange set and s57 for s57 standard exchange set</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetProductIdentifiersDataWithIncorrectOptionalParameterAsync(List<string> productIdentifierModel, string callbackUri = null, string accessToken = null, string exchangeSetStandard = "s63")
        {
            var uri = $"{apiHost}/productData/productIdentifiers";
            if (callbackUri != null)
            {
                uri += $"?callbackuri={callbackUri}&exchangeSetStandar={exchangeSetStandard}";
            }
            else
            {
                uri += $"?exchangeSetStandar={exchangeSetStandard}";
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

        public async Task<HttpResponseMessage> GetExchangeSetProductIdentifiersAsync(string accessToken, List<string> payload)
        {
            var uri = $"{apiHost}/ProductInformation/productIdentifiers";
            var payloadJson = JsonConvert.SerializeObject(payload);
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") };
            if (accessToken != null)
            {
                httpRequestMessage.SetBearerToken(accessToken);
            }
            return await httpClient.SendAsync(httpRequestMessage);
        }

        public async Task<HttpResponseMessage> GetProductInformationByDateTimeAsync(string accessToken = null, string sinceDateTime = null)
        {
            var uri = $"{apiHost}/productInformation";
            if (sinceDateTime != null)
                uri += $"?sinceDateTime={sinceDateTime}";
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            if (accessToken != null)
                httpRequestMessage.SetBearerToken(accessToken);
            return await httpClient.SendAsync(httpRequestMessage);
        }
    }
}
