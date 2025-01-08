using System;
using System.Net;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.V2.Response
{
    public class V2SalesCatalogueResponse
    {
        public string Id { get; set; }
        public V2SalesCatalogueProductResponse ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
