using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentDataService
    {
        Task<string> DownloadSalesCatalogueResponse(string ScsResponseUri, string batchid);
    }
}
