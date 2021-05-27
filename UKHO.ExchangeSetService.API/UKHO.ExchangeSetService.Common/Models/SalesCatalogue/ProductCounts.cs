using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models.SalesCatalogue
{
    public class ProductCounts
    {
        public int? RequestedProductCount { get; set; }
        public int? ReturnedProductCount { get; set; }
        public int? RequestedProductsAlreadyUpToDateCount { get; set; }

        public List<RequestedProductsNotReturned> RequestedProductsNotReturned { get; set; }
    }
}
