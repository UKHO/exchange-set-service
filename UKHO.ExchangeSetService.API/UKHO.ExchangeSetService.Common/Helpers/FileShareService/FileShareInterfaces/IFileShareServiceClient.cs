using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IFileShareServiceClient
    {
        public Task<HttpResponseMessage> CallFileShareServiceApi(HttpMethod method, string requestBody, string authToken, string uri, CancellationToken cancellationToken, string correlationId = "");
        Task<HttpResponseMessage> AddFileInBatchAsync(HttpMethod method, FileCreateModel fileModel, string authToken, string baseUrl, string batchId, string fileName, long? fileContentSizeHeader,
                string mimeTypeHeader = "application/octet-stream", string correlationId = "");
        Task<HttpResponseMessage> UploadFileBlockAsync(HttpMethod method, string baseUrl, string batchId, string fileName, string blockId, byte[] blockBytes, byte[] md5Hash, string accessToken, CancellationToken cancellationToken, string mimeTypeHeader = "application/octet-stream", string correlationId = "");
        Task<HttpResponseMessage> WriteBlockInFileAsync(HttpMethod method, string baseUrl, string batchId, string fileName, WriteBlockFileModel writeBlockFileModel, string accessToken, string mimeTypeHeader = "application/octet-stream", string correlationId = "");
        Task<HttpResponseMessage> CommitBatchAsync(HttpMethod method, string baseUrl, string batchId, BatchCommitModel batchCommitModel, string accessToken, string correlationId = "");
        Task<HttpResponseMessage> GetBatchStatusAsync(HttpMethod method, string baseUrl, string batchId, string accessToken, string correlationId = "");
    }
}
