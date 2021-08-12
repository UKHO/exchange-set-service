using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class CallBackResponse : BaseCallBackResponse
    {
        [JsonProperty("data")]
        public ExchangeSetResponse Data { get; set; }
    }
}