using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentAncillaryFiles
    {
        Task<bool> CreateSerialEncFile(string batchId, string exchangeSetPath, string correlationId);
        bool CreateProductFile(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse);
    }
}