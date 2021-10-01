using System.Collections.Generic;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response
{
    public class SalesCatalogueProductResponse
    {
        public List<Products> Products { get; set; }

        public ProductCounts ProductCounts { get; set; }
    }
}