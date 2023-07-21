using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IFileShareService
    {
        public Task<CreateBatchResponse> CreateBatch(string userOid, string correlationId);
        Task<SearchBatchResponse> GetBatchInfoBasedOnProducts(List<Products> products, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath);
        Task<bool> DownloadBatchFiles(BatchDetail entry, IEnumerable<string> uri, string downloadPath, SalesCatalogueServiceResponseQueueMessage queueMessage, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken);
        Task<bool> DownloadReadMeFile(string readMeFilePath, string batchId, string exchangeSetRootPath, string correlationId);
        Task<string> SearchReadMeFilePath(string batchId, string correlationId);
        Task<bool> CreateZipFileForExchangeSet(string batchId, string exchangeSetZipRootPath, string correlationId);
        Task<bool> UploadFileToFileShareService(string batchId, string exchangeSetZipRootPath, string correlationId, string fileName);
        Task<bool> UploadLargeMediaFileToFileShareService(string batchId, string exchangeSetZipPath, string correlationId, string fileName);
        Task<bool> CommitAndGetBatchStatusForLargeMediaExchangeSet(string batchId, string exchangeSetZipPath, string correlationId);
        Task<IEnumerable<BatchFile>> SearchFolderDetails(string batchId, string correlationId, string uri);
        Task<bool> DownloadFolderDetails(string batchId, string correlationId, IEnumerable<BatchFile> fileDetails, string exchangeSetPath);
        Task<bool> CommitBatchToFss(string batchId, string correlationId, string exchangeSetZipPath, string fileName = "zip");
        Task<string> SearchIhoPubFilePath(string batchId, string correlationId);
        Task<string> SearchIhoCrtFilePath(string batchId, string correlationId);
        Task<bool> DownloadIhoCrtFile(string ihoCrtFilePath, string batchId, string exchangeSetRootPath, string correlationId);
        Task<bool> DownloadIhoPubFile(string ihoPubFilePath, string batchId, string exchangeSetRootPath, string correlationId);
    }
}
