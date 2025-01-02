using System.Collections.Generic;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response.V2
{
    public class SalesCatalogueProductV2Response
    {
        public ProductCounts ProductCounts { get; set; }
        public List<ProductsV2> Products { get; set; }
    }
}
