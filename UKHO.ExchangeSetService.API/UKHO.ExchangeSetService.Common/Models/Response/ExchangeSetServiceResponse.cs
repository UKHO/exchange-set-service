using Newtonsoft.Json;
using System.Net;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class ExchangeSetServiceResponse
    {
        public ExchangeSetResponse ExchangeSetResponse { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }

        public string LastModified { get; set; }

        public string BatchId { get; set; }
        [JsonIgnore]
        public bool IsExchangeSetTooLarge { get; set; }

    }
}
