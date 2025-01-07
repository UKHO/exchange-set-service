using System.Collections.Generic;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.V2.Response
{
    public class V2SalesCatalogueProductResponse
    {
        public ProductCounts ProductCounts { get; set; }
        public List<V2Products> Products { get; set; }
    }
}
