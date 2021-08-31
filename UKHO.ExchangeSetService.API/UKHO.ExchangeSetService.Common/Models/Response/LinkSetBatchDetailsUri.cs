using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class LinkSetBatchDetailsUri
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }
}