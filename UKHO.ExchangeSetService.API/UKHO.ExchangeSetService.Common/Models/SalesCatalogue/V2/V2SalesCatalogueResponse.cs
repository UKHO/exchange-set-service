using System;
using System.Net;

namespace UKHO.ExchangeSetService.Common.Models.SalesCatalogue.V2
{
    public class V2SalesCatalogueResponse
    {
        public V2SalesCatalogueProductResponse ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
        public DateTime? LastModified { get; set; }
        public DateTime ScsRequestDateTime { get; set; }
    }    
}
