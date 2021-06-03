using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IQueryFssService
    {
        Task<List<FulfillmentDataResponse>> QueryFss(List<Products> products);
        Task<string> UploadFssDataToBlob(string uploadFileName, List<FulfillmentDataResponse> fulfillmentDataResponse, string storageAccountConnectionString, string containerName);
    }
}
