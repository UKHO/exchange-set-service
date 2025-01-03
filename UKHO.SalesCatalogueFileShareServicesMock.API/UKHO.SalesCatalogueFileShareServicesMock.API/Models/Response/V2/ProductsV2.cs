using System.Collections.Generic;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response.V2
{
    public class ProductsV2
    {
        public string ProductName { get; set; }
        public int? EditionNumber { get; set; }
        public List<int?> UpdateNumbers { get; set; }
        public List<Dates> Dates { get; set; }
        public Cancellation Cancellation { get; set; }
        public int? FileSize { get; set; }
    }
}
