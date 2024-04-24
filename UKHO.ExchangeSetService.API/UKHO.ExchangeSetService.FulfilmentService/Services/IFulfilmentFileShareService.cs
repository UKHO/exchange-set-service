using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentFileShareService
    {
        Task<List<FulfilmentDataResponse>> QueryFileShareServiceData(List<Products> products, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath);
        Task<(List<FulfilmentDataResponse>, List<(string fileName, string filePath, byte[] fileContent)>)> QueryFileShareServiceData1(List<Products> products, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath);
        Task<bool> DownloadReadMeFile(string filePath, string batchId, string exchangeSetRootPath, string correlationId);
        Task<List<(string fileName, string filePath, byte[] fileContent)>> DownloadReadMeFile1(string filePath, string batchId, string exchangeSetRootPath, string correlationId);
        Task<string> SearchReadMeFilePath(string batchId, string correlationId);
        Task<bool> CreateZipFileForExchangeSet(string batchId, string exchangeSetZipRootPath, string correlationId);
        Task<bool> UploadZipFileForExchangeSetToFileShareService(string batchId, string exchangeSetZipRootPath, string correlationId, string zipFileName);
        Task<bool> UploadZipFileForLargeMediaExchangeSetToFileShareService(string batchId, string exchangeSetZipRootPath, string correlationId, string mediaZipFileName);
        Task<bool> CommitLargeMediaExchangeSet(string batchId, string exchangeSetZipPath, string correlationId);
        Task<IEnumerable<BatchFile>> SearchFolderDetails(string batchId, string correlationId, string folderName);
        Task<bool> DownloadFolderDetails(string batchId, string correlationId, IEnumerable<BatchFile> fileDetails, string exchangeSetPath);
        Task<bool> CommitExchangeSet(string batchId, string correlationId, string exchangeSetZipPath);

        Task<string> SearchIhoPubFilePath(string batchId, string correlationId);
        Task<string> SearchIhoCrtFilePath(string batchId, string correlationId);
        Task<bool> DownloadIhoPubFile(string filePath, string batchId, string exchangeSetRootPath, string correlationId);
        Task<bool> DownloadIhoCrtFile(string filePath, string batchId, string exchangeSetRootPath, string correlationId);
    }
}