using System.Collections.Generic;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response
{
    public class SalesCatalogueProductResponse
    {
        public ProductCounts ProductCounts { get; set; }
        public List<Products> Products { get; set; }
    }
}