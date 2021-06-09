using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    public class PagingLinks
    {
        public Link Self { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Link First { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Link Previous { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Link Next { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Link Last { get; set; }
    }
}
