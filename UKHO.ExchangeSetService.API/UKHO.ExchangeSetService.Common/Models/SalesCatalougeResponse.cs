using System.Net;

namespace UKHO.ExchangeSetService.Common.Models
{
    public class SalesCatalougeResponse
    {
        public SalesCatalogueProductResponse ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
    }
}
