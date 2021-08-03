using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class Links
    {
        [JsonProperty("exchangeSetBatchStatusUri")]
        public LinkSetBatchStatusUri ExchangeSetBatchStatusUri { get; set; }

        [JsonProperty("exchangeSetFileUri")]
        public LinkSetFileUri ExchangeSetFileUri { get; set; }
    }
}