using System.Collections.Generic;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.V2.Response
{
    public class SalesCatalogueProductResponse
    {
        public ProductCounts ProductCounts { get; set; }
        public List<Products> Products { get; set; }
    }
}
