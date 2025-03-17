using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;

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

        public async Task<HttpResponseMessage> CallFileShareServiceApi(HttpMethod method, string requestBody, string authToken, string uri, CancellationToken cancellationToken, string correlationId = "")
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
            var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
            return response;
        }

        public async Task<HttpResponseMessage> AddFileInBatchAsync(HttpMethod method, FileCreateModel fileModel, string authToken, string baseUrl, string batchId, string fileName, long? fileContentSizeHeader,
                string mimeTypeHeader = "application/octet-stream", string correlationId = "")
        {
            var uri = $"{baseUrl}/batch/{batchId}/files/{fileName}";
            var payloadJson = JsonConvert.SerializeObject(fileModel);

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

        public async Task<HttpResponseMessage> UploadFileBlockAsync(HttpMethod method, string baseUrl, string batchId, string fileName, string blockId, byte[] blockBytes, byte[] md5Hash, string accessToken, CancellationToken cancellationToken, string mimeTypeHeader = "application/octet-stream", string correlationId = "")
        {
            var uri = $"{baseUrl}/batch/{batchId}/files/{fileName}/{blockId}";

            using var httpRequestMessage = new HttpRequestMessage(method, uri)
            { Content = blockBytes == null ? null : new ByteArrayContent(blockBytes) };
            httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

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

            return await httpClient.SendAsync(httpRequestMessage, cancellationToken);

        }

        public async Task<HttpResponseMessage> WriteBlockInFileAsync(HttpMethod method, string baseUrl, string batchId, string fileName, WriteBlockFileModel writeBlockFileModel, string accessToken, string mimeTypeHeader = "application/octet-stream", string correlationId = "")
        {
            var uri = $"{baseUrl}/batch/{batchId}/files/{fileName}";
            var payloadJson = JsonConvert.SerializeObject(writeBlockFileModel);

            using var httpRequestMessage = new HttpRequestMessage(method, uri)
            { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") };

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

        public async Task<HttpResponseMessage> CommitBatchAsync(HttpMethod method, string baseUrl, string batchId, BatchCommitModel batchCommitModel, string accessToken, string correlationId = "")
        {
            var uri = $"{baseUrl}/batch/{batchId}";
            var payloadJson = JsonConvert.SerializeObject(batchCommitModel.FileDetails);

            using var httpRequestMessage = new HttpRequestMessage(method, uri)
            { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") };

            if (correlationId != "")
            {
                httpRequestMessage.Headers.Add("X-Correlation-ID", correlationId);
            }

            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        public async Task<HttpResponseMessage> GetBatchStatusAsync(HttpMethod method, string baseUrl, string batchId, string accessToken, string correlationId = "")
        {
            var uri = $"{baseUrl}/batch/{batchId}/status";

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            if (correlationId != "")
            {
                httpRequestMessage.Headers.Add("X-Correlation-ID", correlationId);
            }
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}
