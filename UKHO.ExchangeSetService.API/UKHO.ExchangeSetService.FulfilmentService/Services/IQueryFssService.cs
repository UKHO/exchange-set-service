using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IQueryFssService
    {
        Task<SearchBatchResponse> QueryFss(List<Products> products);
        Task<string> UploadFssDataToBlob(string uploadFileName, SearchBatchResponse searchBatchResponse, string storageAccountConnectionString, string containerName);
    }
}
