using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers.SalesCatalogue
{
    public interface ISalesCatalogueService
    {
        public Task<SalesCatalogueResponse> GetProductsFromSpecificDateAsync(string sinceDateTime, string correlationId);
        public Task<SalesCatalogueResponse> PostProductIdentifiersAsync(List<string> productIdentifiers, string correlationId);
        public Task<SalesCatalogueResponse> PostProductVersionsAsync(List<ProductVersionRequest> productVersions, string correlationId);
        public Task<SalesCatalogueDataResponse> GetSalesCatalogueDataResponse(string batchId, string correlationId);
    }
}
