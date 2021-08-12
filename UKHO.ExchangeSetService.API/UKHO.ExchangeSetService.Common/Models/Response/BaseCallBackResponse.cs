using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class BaseCallBackResponse
    {
        [JsonProperty("specversion")]
        public string SpecVersion { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("source")]
        public string Source { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("time")]
        public string Time { get; set; }
        [JsonProperty("subject")]
        public string Subject { get; set; }
        [JsonProperty("datacontenttype")]
        public string DataContentType { get; set; }
    }
}