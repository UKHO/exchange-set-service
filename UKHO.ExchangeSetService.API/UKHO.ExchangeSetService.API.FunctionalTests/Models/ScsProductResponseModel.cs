using System.Collections.Generic;


namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    public class ScsProductResponseModel
    {
        public List<Product> Products { get; set; }
        public ProductCounts ProductCounts { get; set; }

    }

    public class Product
    {
        public string ProductName { get; set; }
        public int EditionNumber { get; set; }
        public List<int> UpdateNumbers { get; set; }
        public int FileSize { get; set; }
    }

    public class ProductCounts
    {
        public int RequestedProductCount { get; set; }
        public int ReturnedProductCount { get; set; }
        public int RequestedProductsAlreadyUpToDateCount { get; set; }
        public List<object> RequestedProductsNotReturned { get; set; }
    }
}
