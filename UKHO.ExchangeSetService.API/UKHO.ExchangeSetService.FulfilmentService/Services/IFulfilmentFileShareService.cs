using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentFileShareService
    {
        Task<List<FulfilmentDataResponse>> QueryFileShareServiceData(List<Products> products);
        Task DownloadFileShareServiceFiles(SalesCatalogueServiceResponseQueueMessage message, List<FulfilmentDataResponse> fulfilmentDataResponses, string exchangeSetRootPath);
    }
}
