using System.Collections.Generic;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response.V2
{
    public class V2SalesCatalogueProductResponse
    {
        public ProductCounts ProductCounts { get; set; }
        public List<V2Products> Products { get; set; }
    }
}
