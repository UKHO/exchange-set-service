using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class CallBackUri
    {
        [JsonProperty("exchangeSetBatchStatusUri")]
        public LinkSetBatchStatusUri ExchangeSetBatchStatusUri { get; set; }

        [JsonProperty("exchangeSetFileUri")]
        public LinkSetFileUri ExchangeSetFileUri { get; set; }

        [JsonProperty("errorFileUri")]
        public LinkSetErrorFileUri ExchangeSetErrorFileUri { get; set; }
    }
}