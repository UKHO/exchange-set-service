using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Helpers.Zip;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class FileShareService(
        IFileShareBatchService fileShareBatchService,
        IFileShareDownloadService fileShareDownloadService,
        IFileShareSearchService fileShareSearchService,
        IFileShareUploadService fileShareUploadService,
        IZip zipService) : IFileShareService
    {
        public Task<bool> CommitAndGetBatchStatusForLargeMediaExchangeSet(string batchId, string exchangeSetZipPath, string correlationId)
        {
            return fileShareBatchService.CommitAndGetBatchStatusForLargeMediaExchangeSet(batchId, exchangeSetZipPath, correlationId);
        }

        public Task<bool> CommitBatchToFss(string batchId, string correlationId, string exchangeSetZipPath, string fileName = "zip")
        {
            return fileShareBatchService.CommitBatchToFss(batchId, correlationId, exchangeSetZipPath, fileName);
        }

        public Task<CreateBatchResponse> CreateBatch(string userOid, string correlationId)
        {
            return fileShareBatchService.CreateBatch(userOid, correlationId);
        }

        public Task<bool> CreateZipFileForExchangeSet(string batchId, string exchangeSetZipRootPath, string correlationId)
        {
            return zipService.CreateZipFileForExchangeSet(batchId, exchangeSetZipRootPath, correlationId);
        }

        public Task<bool> DownloadBatchFiles(BatchDetail entry, IEnumerable<string> uri, string downloadPath, SalesCatalogueServiceResponseQueueMessage queueMessage, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            return fileShareDownloadService.DownloadBatchFiles(entry, uri, downloadPath, queueMessage, cancellationTokenSource, cancellationToken);
        }

        public Task<bool> DownloadFolderDetails(string batchId, string correlationId, IEnumerable<BatchFile> fileDetails, string exchangeSetPath)
        {
            return fileShareDownloadService.DownloadFolderDetails(batchId, correlationId, fileDetails, exchangeSetPath);
        }

        public Task<bool> DownloadIhoCrtFile(string ihoCrtFilePath, string batchId, string aioExchangeSetPath, string correlationId)
        {
            return fileShareDownloadService.DownloadIhoCrtFile(ihoCrtFilePath, batchId, aioExchangeSetPath, correlationId);
        }

        public Task<bool> DownloadIhoPubFile(string ihoPubFilePath, string batchId, string aioExchangeSetPath, string correlationId)
        {
            return fileShareDownloadService.DownloadIhoPubFile(ihoPubFilePath, batchId, aioExchangeSetPath, correlationId);
        }

        public Task<bool> DownloadReadMeFileFromCacheAsync(string batchId, string exchangeSetRootPath, string correlationId)
        {
            return fileShareDownloadService.DownloadReadMeFileFromCacheAsync(batchId, exchangeSetRootPath, correlationId);
        }

        public Task<bool> DownloadReadMeFileFromFssAsync(string readMeFilePath, string batchId, string exchangeSetRootPath, string correlationId)
        {
            return fileShareDownloadService.DownloadReadMeFileFromFssAsync(readMeFilePath, batchId, exchangeSetRootPath, correlationId);
        }

        public Task<SearchBatchResponse> GetBatchInfoBasedOnProducts(List<Products> products, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath, string businessUnit)
        {
            return fileShareBatchService.GetBatchInfoBasedOnProducts(products, message, cancellationTokenSource, cancellationToken, exchangeSetRootPath, businessUnit);
        }

        public Task<IEnumerable<BatchFile>> SearchFolderDetails(string batchId, string correlationId, string uri)
        {
            return fileShareSearchService.SearchFolderDetails(batchId, correlationId, uri);
        }

        public Task<string> SearchIhoCrtFilePath(string batchId, string correlationId)
        {
            return fileShareSearchService.SearchIhoCrtFilePath(batchId, correlationId);
        }

        public Task<string> SearchIhoPubFilePath(string batchId, string correlationId)
        {
            return fileShareSearchService.SearchIhoPubFilePath(batchId, correlationId);
        }

        public Task<string> SearchReadMeFilePath(string batchId, string correlationId)
        {
            return fileShareSearchService.SearchReadMeFilePath(batchId, correlationId);
        }

        public Task<bool> UploadFileToFileShareService(string batchId, string exchangeSetZipRootPath, string correlationId, string fileName)
        {
            return fileShareUploadService.UploadFileToFileShareService(batchId, exchangeSetZipRootPath, correlationId, fileName);
        }

        public Task<bool> UploadLargeMediaFileToFileShareService(string batchId, string exchangeSetZipPath, string correlationId, string fileName)
        {
            return fileShareUploadService.UploadLargeMediaFileToFileShareService(batchId, exchangeSetZipPath, correlationId, fileName);
        }
    }
}
