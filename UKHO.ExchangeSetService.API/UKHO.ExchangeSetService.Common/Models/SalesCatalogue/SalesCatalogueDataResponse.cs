using System;
using System.Collections.Generic;
using System.Net;

namespace UKHO.ExchangeSetService.Common.Models.SalesCatalogue
{
    public class SalesCatalogueDataResponse
    {
        public List<SalesCatalogueDataProductResponse> ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
        public DateTime? LastModified { get; set; }
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

}
