using System.Collections.Generic;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    public class Cancellation
    {
        public int EditionNumber { get; set; }
        public int UpdateNumber { get; set; }
    }



    public class ProductDetails
    {
        public string ProductName { get; set; }
        public int EditionNumber { get; set; }
        public List<int> UpdateNumbers { get; set; }
        public Cancellation Cancellation { get; set; }
        public int FileSize { get; set; }
    }



    public class ProductDetailsCounts
    {
        public int RequestedProductCount { get; set; }
        public int ReturnedProductCount { get; set; }
        public int RequestedProductsAlreadyUpToDateCount { get; set; }
        public List<object> RequestedProductsNotReturned { get; set; }
    }



    public class CancellationResponseModel
    {
        public List<ProductDetails> Products { get; set; }
        public ProductDetailsCounts ProductCounts { get; set; }
    }
}
