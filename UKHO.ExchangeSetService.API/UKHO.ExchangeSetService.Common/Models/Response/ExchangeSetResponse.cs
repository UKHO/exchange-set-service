using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class ExchangeSetResponse : BaseExchangeSetResponse
    {
        [JsonProperty("_links")]
        public Links Links { get; set; }
    }
}