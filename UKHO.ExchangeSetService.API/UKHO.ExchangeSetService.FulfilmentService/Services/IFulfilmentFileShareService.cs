using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentFileShareService
    {
        Task<List<FulfillmentDataResponse>> QueryFileShareServiceData(List<Products> products);
        Task<string> UploadFileShareServiceData(string uploadFileName, List<FulfillmentDataResponse> fulfillmentDataResponse, string storageAccountConnectionString, string containerName);
    }
}
