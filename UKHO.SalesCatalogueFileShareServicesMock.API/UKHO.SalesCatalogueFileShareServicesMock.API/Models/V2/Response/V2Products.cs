using System.Collections.Generic;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.V2.Response
{
    public class V2Products
    {
        public string ProductName { get; set; }
        public int? EditionNumber { get; set; }
        public List<int?> UpdateNumbers { get; set; }
        public List<Dates> Dates { get; set; }
        public Cancellation Cancellation { get; set; }
        public int? FileSize { get; set; }
    }    
}
