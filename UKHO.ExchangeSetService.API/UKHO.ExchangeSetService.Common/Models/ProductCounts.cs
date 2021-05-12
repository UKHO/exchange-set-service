using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models
{
    public class ProductCounts
    {
        public int? RequestedProductCount { get; set; }
        public int? ReturnedProductCount { get; set; }
        public int? RequestedProductsAlreadyUpToDateCount { get; set; }

        public List<RequestedProductsNotReturned> RequestedProductsNotReturned { get; set; }
    }

    public class RequestedProductsNotReturned
    {
        public string ProductName { get; set; }
        public string Reason { get; set; }
    }
}
