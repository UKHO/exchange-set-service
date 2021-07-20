using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface ISalesCatalogueService
    {
        public Task<SalesCatalogueResponse> GetProductsFromSpecificDateAsync(string sinceDateTime);
        public Task<SalesCatalogueResponse> PostProductIdentifiersAsync(List<string> productIdentifiers);
        public Task<SalesCatalogueResponse> PostProductVersionsAsync(List<ProductVersionRequest> productVersions);
        public Task<SalesCatalogueDataResponse> GetSalesCatalogueDataResponse(string batchId, string correlationId);
    }
}
