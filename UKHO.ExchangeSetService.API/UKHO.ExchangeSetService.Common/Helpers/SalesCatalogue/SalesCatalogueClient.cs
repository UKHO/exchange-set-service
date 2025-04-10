// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers.SalesCatalogue
{
    public class SalesCatalogueClient : ISalesCatalogueClient
    {
        public const string XCorrelationIdHeaderKey = "X-Correlation-ID";
        private readonly HttpClient _httpClient;

        public SalesCatalogueClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("Sales Catalogue");
        }

        public async Task<HttpResponseMessage> CallSalesCatalogueServiceApi(HttpMethod method,
            string requestBody,
            string authToken,
            string uri,
            string correlationId = "",
            CancellationToken cancellationToken = default)
        {
            using var httpRequestMessage = new HttpRequestMessage(method, uri)
            {
                Content = string.IsNullOrEmpty(requestBody) ? null : new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrEmpty(correlationId))
            {
                httpRequestMessage.Headers.Add(XCorrelationIdHeaderKey, correlationId);
            }

            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            return await _httpClient.SendAsync(httpRequestMessage, cancellationToken);
        }
    }
}
