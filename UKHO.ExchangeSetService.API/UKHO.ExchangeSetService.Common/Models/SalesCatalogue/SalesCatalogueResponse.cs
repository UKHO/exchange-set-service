using System;
using System.Net;

namespace UKHO.ExchangeSetService.Common.Models.SalesCatalogue
{
    public class SalesCatalogueResponse
    {
        public SalesCatalogueProductResponse ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
        public DateTime? LastModified { get; set; }
        public DateTime ScsRequestDateTime { get; set; }
    }    
}
