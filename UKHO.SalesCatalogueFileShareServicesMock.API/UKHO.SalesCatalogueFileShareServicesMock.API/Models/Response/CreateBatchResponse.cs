using System.Net;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response
{
    public class CreateBatchResponse
    {
        public CreateBatchResponseModel ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
    }
}
