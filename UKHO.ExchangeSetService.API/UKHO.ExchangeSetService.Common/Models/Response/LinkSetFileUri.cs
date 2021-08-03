using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class LinkSetFileUri
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }
}