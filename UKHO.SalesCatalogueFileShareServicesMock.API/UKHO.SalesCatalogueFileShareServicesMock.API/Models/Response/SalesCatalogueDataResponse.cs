using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response
{
    public class SalesCatalogueDataResponse
    {
        public List<SalesCatalogueDataProductResponse> ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
