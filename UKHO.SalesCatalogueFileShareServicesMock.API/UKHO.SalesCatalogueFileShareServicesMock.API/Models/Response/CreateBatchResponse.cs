using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response
{
    public class CreateBatchResponse
    {
        public CreateBatchResponseModel ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
    }
}
