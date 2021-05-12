using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface ISalesCatalougeService
    {
        public Task<SalesCatalougeResponse> GetProductsFromSpecificDateAsync(string sinceDateTime);
        public Task<SalesCatalougeResponse> PostProductIdentifiersAsync(List<string> ProductIdentifiers);
        public Task<SalesCatalougeResponse> PostProductVersionsAsync(List<ProductVersionRequest> productVersions);
    }
}
