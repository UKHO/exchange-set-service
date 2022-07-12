using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentDataService
    {
        Task<string> CreateExchangeSet(SalesCatalogueServiceResponseQueueMessage message, string currentUtcDate);
        Task<string> CreateLargeExchangeSet(SalesCatalogueServiceResponseQueueMessage message, string currentUtcDate, string largeExchangeSetFolderName);
        Task GetReadmeFiles(string batchId, string exchangeSetPath, string correlationId);
    }
}
