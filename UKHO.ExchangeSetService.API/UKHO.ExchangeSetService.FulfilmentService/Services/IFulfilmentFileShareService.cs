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
        Task<bool> DownloadReadMeFile(string filePath, string batchId, string exchangeSetRootPath, string correlationId);
        Task<string> SearchReadMeFilePath(string batchId, string correlationId);
        Task<bool> CreateZipFileForExchangeSet(string batchId, string exchangeSetZipRootPath, string correlationId);
        Task<bool> UploadZipFileForExchangeSetToFileShareService(string batchId, string exchangeSetZipRootPath, string correlationId);
        Task<bool> UploadZipFileForLargeMediaExchangeSetToFileShareService(string batchId, string exchangeSetZipRootPath, string correlationId, string mediaZipFileName);
        Task<bool> CommitLargeMediaExchangeSet(string batchId, string exchangeSetZipPath, string correlationId);
        Task<List<BatchFile>> SearchFolderDetails(string batchId, string correlationId, string folderName);
        Task<bool> DownloadFolderDetails(string batchId, string correlationId, List<BatchFile> fileDetails, string exchangeSetPath);       
    }
}