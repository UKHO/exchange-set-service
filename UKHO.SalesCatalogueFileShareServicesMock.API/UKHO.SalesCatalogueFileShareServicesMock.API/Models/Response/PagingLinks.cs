using Newtonsoft.Json;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response
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