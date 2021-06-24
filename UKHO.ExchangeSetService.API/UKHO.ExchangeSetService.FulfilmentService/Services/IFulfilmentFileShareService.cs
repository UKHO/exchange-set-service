using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentFileShareService
    {
        Task<List<FulfilmentDataResponse>> QueryFileShareServiceData(List<Products> products, string correlationId);
        Task DownloadFileShareServiceFiles(SalesCatalogueServiceResponseQueueMessage message, List<FulfilmentDataResponse> fulfilmentDataResponses, string exchangeSetRootPath);
        Task<bool> DownloadReadMeFile(string filePath, string batchId, string exchangeSetRootPath, string correlationId);
        Task<string> SearchReadMeFilePath(string batchId, string correlationId);
        bool CreateZipFileForExchangeSet(SalesCatalogueServiceResponseQueueMessage message, string exchangeSetZipRootPath);
        Task<bool> UploadZipFileForExchangeSetToFileShareService(SalesCatalogueServiceResponseQueueMessage message, string exchangeSetZipRootPath,string correlationId);
    }
}
