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

        public async Task CallBackApi(HttpMethod method, string requestBody, string uri)
        {
            using var httpRequestMessage = new HttpRequestMessage(method, uri)
            { 
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

             await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}