using System.Net;
using System;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response.V2
{
    public class SalesCatalogueV2Response
    {
        public string Id { get; set; }
        public SalesCatalogueProductV2Response ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
