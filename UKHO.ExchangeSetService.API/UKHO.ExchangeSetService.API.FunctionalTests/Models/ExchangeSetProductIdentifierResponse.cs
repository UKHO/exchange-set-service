using System;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    public class ExchangeSetProductIdentifierResponse
    {
        public Products[] Products { get; set; }
        public Productcounts ProductCounts { get; set; }
    }

    public class Productcounts
    {
        public int RequestedProductCount { get; set; }
        public int ReturnedProductCount { get; set; }
        public int RequestedProductsAlreadyUpToDateCount { get; set; }
        public object[] RequestedProductsNotReturned { get; set; }
    }

    public class Products
    {
        public string ProductName { get; set; }
        public int EditionNumber { get; set; }
        public int[] UpdateNumbers { get; set; }
        public Dates[] Dates { get; set; }
        public object Cancellation { get; set; }
        public int FileSize { get; set; }
        public bool IgnoreCache { get; set; }
        public object Bundle { get; set; }
    }

    public class Dates
    {
        public int UpdateNumber { get; set; }
        public DateTime? UpdateApplicationDate { get; set; }
        public DateTime IssueDate { get; set; }
    }
}
