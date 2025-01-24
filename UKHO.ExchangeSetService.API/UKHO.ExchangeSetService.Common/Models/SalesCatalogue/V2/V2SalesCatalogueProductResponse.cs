using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models.SalesCatalogue.V2
{
    public class V2SalesCatalogueProductResponse
    {
        public List<V2Products> Products { get; set; }
        public ProductCounts ProductCounts { get; set; }

    }
}
