using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Helpers.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentSalesCatalogueService : IFulfilmentSalesCatalogueService
    {
        private readonly ISalesCatalogueService salesCatalogueService;

        public FulfilmentSalesCatalogueService(ISalesCatalogueService salesCatalogueService)
        {
            this.salesCatalogueService = salesCatalogueService;
        }
        public async Task<SalesCatalogueDataResponse> GetSalesCatalogueDataResponse(string batchId, string correlationId)
        {
            SalesCatalogueDataResponse salesCatalogueTypeResponse = await salesCatalogueService.GetSalesCatalogueDataResponse(batchId, correlationId);
            return salesCatalogueTypeResponse;
        }
    }
}
