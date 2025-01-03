using System;
using System.Net;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.V2.Response
{
    public class SalesCatalogueResponse
    {
        public string Id { get; set; }
        public SalesCatalogueProductResponse ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
