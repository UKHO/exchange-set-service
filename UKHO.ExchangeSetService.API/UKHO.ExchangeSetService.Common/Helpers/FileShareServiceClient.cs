using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage] ////Excluded from code coverage as it has actual http calls 
    public class FileShareServiceClient : IFileShareServiceClient
    {
        private readonly HttpClient httpClient;

        public FileShareServiceClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> CallFileShareServiceApi(HttpMethod method, string requestBody, string authToken, string uri, string correlationId="")
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

            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            var response = await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            return response;
        }

        public async Task<HttpResponseMessage> AddFileInBatchAsync(HttpMethod method, string requestBody, string authToken,string baseUrl,string batchId, string fileName, long? fileContentSizeHeader,
                string mimeTypeHeader = "application/octet-stream", string correlationId = "")
        {
            string uri = $"{baseUrl}/batch/{batchId}/files/{fileName}";
            string payloadJson = JsonConvert.SerializeObject(requestBody);

            using var httpRequestMessage = new HttpRequestMessage(method, uri)
            { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") };
            if (fileContentSizeHeader.HasValue)
            {
                httpRequestMessage.Headers.Add("X-Content-Size", fileContentSizeHeader.Value.ToString());
            }
            if (correlationId != "")
            {
                httpRequestMessage.Headers.Add("X-Correlation-ID", correlationId);
            }
            if (mimeTypeHeader != null)
            {
                httpRequestMessage.Headers.Add("X-MIME-Type", mimeTypeHeader);
            }

            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        public async Task<HttpResponseMessage> UploadFileBlockAsync(HttpMethod method, string baseUrl,string batchId, string fileName, string blockId, byte[] blockBytes, byte[] md5Hash, string accessToken, string mimeTypeHeader = "application/octet-stream", string correlationId = "")
        {
            string uri = $"{baseUrl}/batch/{batchId}/files/{fileName}/{blockId}";

            using var httpRequestMessage = new HttpRequestMessage(method, uri)
            { Content = (blockBytes == null) ? null : new ByteArrayContent(blockBytes) };
            httpRequestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            if (md5Hash != null)
            {
                httpRequestMessage.Content.Headers.ContentMD5 = md5Hash;
            }
            if (correlationId != "")
            {
                httpRequestMessage.Headers.Add("X-Correlation-ID", correlationId);
            }
            if (mimeTypeHeader != null)
            {
                httpRequestMessage.Headers.Add("X-MIME-Type", mimeTypeHeader);
            }
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);

        }
    }
}
