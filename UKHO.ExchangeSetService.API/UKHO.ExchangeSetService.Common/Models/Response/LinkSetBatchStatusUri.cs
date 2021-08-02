using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class LinkSetBatchStatusUri
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }
}