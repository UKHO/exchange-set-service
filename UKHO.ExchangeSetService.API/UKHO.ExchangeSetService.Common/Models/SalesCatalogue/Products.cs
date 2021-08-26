using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models.SalesCatalogue
{
    public class Products
    {
        public string ProductName { get; set; }
        public int? EditionNumber { get; set; }
        public List<int?> UpdateNumbers { get; set; }
        public List<Dates> Dates { get; set; }
        public Cancellation Cancellation { get; set; }
        public int? FileSize { get; set; }
    }    
}
