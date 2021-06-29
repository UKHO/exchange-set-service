using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentSalesCatalogueService
    {
        Task<SalesCatalogueDataResponse> GetSalesCatalogueDataResponse(string correlationId);
    }
}
