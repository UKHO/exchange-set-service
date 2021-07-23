using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentCallBackService
    {
        Task<bool> CreateCallBackReponse(SalesCatalogueProductResponse salesCatalogueProductResponse, SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage);
    }
}