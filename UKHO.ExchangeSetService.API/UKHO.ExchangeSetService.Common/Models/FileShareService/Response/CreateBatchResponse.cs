using System.Net;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    public class CreateBatchResponse
    {
        public CreateBatchResponseModel ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
    }
}
