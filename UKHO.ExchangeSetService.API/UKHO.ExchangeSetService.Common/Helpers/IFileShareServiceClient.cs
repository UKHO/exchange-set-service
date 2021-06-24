using System.Net.Http;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IFileShareServiceClient
    {
        public Task<HttpResponseMessage> CallFileShareServiceApi(HttpMethod method, string requestBody, string authToken, string uri,string correlationId="");
        Task<HttpResponseMessage> AddFileInBatchAsync(HttpMethod method, string requestBody, string authToken, string baseUrl, string batchId, string fileName, long? fileContentSizeHeader,
                string mimeTypeHeader = "application/octet-stream", string correlationId = "");
    }
}
