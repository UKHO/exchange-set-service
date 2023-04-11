using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class Links
    {
        [JsonProperty("exchangeSetBatchStatusUri")]
        public LinkSetBatchStatusUri ExchangeSetBatchStatusUri { get; set; }

        [JsonProperty("exchangeSetBatchDetailsUri")]
        public LinkSetBatchDetailsUri ExchangeSetBatchDetailsUri { get; set; }

        [JsonProperty("exchangeSetFileUri")]
        public LinkSetFileUri ExchangeSetFileUri { get; set; }

        [JsonProperty("aioExchangeSetFileUri", NullValueHandling = NullValueHandling.Ignore)]
        public LinkSetFileUri AioExchangeSetFileUri { get; set; } = null;
        
        [JsonProperty("errorFileUri", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore]
        public LinkSetErrorFileUri ExchangeSetErrorFileUri { get; set; }
    }
}