using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentDataService
    {
        Task<string> CreateExchangeSet(SalesCatalogueServiceResponseQueueMessage message, string currentUtcDateTime);
    }
}
