using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentAncillaryFiles
    {
        Task<bool> CreateSalesCatalogueDataProductFile(string batchId, string exchangeSetInfoPath, string correlationId);
    }
}
