using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class ExchangeSetErrorResponse : BaseExchangeSetResponse
    {
        [JsonProperty("_links")]
        public ErrorLinks Links { get; set; }
    }
}