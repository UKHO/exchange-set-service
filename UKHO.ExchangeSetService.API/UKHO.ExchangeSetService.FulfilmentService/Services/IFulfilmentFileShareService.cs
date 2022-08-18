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
        Task<List<BatchFile>> SearchInfoFilePath(string batchId, string correlationId);
        Task<bool> DownloadInfoFiles(string batchId, string correlationId, List<BatchFile> fileDetails, string exchangesetPath);
        Task<List<BatchFile>> SearchAdcFilePath(string batchId, string correlationId);
    }
}
