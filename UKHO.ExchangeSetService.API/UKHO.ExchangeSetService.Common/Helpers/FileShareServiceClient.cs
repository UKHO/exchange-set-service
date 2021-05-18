using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class FileShareServiceClient : IFileShareServiceClient
    {
        private readonly HttpClient httpClient;

        public FileShareServiceClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> CallFileShareServiceApi(HttpMethod method, string postBody, string authToken, string uri)
        {
            HttpContent content = null;

            if (postBody != null)
                content = new StringContent(postBody, Encoding.UTF8, "application/json");

            using var httpRequestMessage = new HttpRequestMessage(method, uri)
            { Content = content };
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            var response = await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            return response;
        }
    }
}
