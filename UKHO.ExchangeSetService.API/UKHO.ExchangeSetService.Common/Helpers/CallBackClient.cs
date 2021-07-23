using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class CallBackClient : ICallBackClient
    {
        private readonly HttpClient httpClient;

        public CallBackClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task CallBackApi(HttpMethod method, string requestBody, string uri, string correlationId = "")
        {
            HttpContent content = null;

            if (requestBody != null)
                content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            using var httpRequestMessage = new HttpRequestMessage(method, uri)
            { Content = content };

            if (correlationId != "")
            {
                httpRequestMessage.Headers.Add("X-Correlation-ID", correlationId);
            }
             
            await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}