using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class LinkSetErrorFileUri
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }
}