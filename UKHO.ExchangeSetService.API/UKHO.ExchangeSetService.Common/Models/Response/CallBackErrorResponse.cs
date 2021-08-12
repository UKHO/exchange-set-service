using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class CallBackErrorResponse : BaseCallBackResponse
    {
        [JsonProperty("data")]
        public ExchangeSetErrorResponse Data { get; set; }
    }
}