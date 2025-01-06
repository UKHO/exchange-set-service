using System.Net;
using System;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response.V2
{
    public class V2SalesCatalogueResponse
    {
        public string Id { get; set; }
        public V2SalesCatalogueProductResponse ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
