using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers.SalesCatalogue
{
    [ExcludeFromCodeCoverage] ////Excluded from code coverage as it has actual http calls 
    public class SalesCatalogueClient : ISalesCatalogueClient
    {
        private readonly HttpClient httpClient;

        public SalesCatalogueClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> CallSalesCatalogueServiceApi(HttpMethod method, string requestBody, string authToken, string uri, string correlationId = "")
        {
            HttpContent content = null;

            if (requestBody != null)
            {
                content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            }

            using var httpRequestMessage = new HttpRequestMessage(method, uri)
            {
                Content = content
            };

            if (correlationId != "")
            {
                httpRequestMessage.Headers.Add("X-Correlation-ID", correlationId);
            }

            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            var response = await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            return response;
        }
    }
}
