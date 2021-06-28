using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Helpers;
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
        public async Task<SalesCatalogueDataResponse> CreateSalesCatalogueDataResponse()
        {
            SalesCatalogueDataResponse salesCatalogueTypeResponse = await salesCatalogueService.GetSalesCatalogueDataResponse();
            return salesCatalogueTypeResponse;
        }
    }
}
