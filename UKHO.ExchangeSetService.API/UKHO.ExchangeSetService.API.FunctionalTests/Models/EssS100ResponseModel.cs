using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    public class Link
    {
        public string Href { get; set; }
    }

    public class S100Links
    {
        public Link ExchangeSetBatchStatusUri { get; set; }
        public Link ExchangeSetBatchDetailsUri { get; set; }
        public Link ExchangeSetFileUri { get; set; }
    }


    public class RequestedProductNotInExchangeSet
    {
        public string ProductName { get; set; }
        public string Reason { get; set; }
    }

    public class ExchangeSetBatch
    {
        [JsonProperty("_links")]
        public S100Links Link { get; set; }

        [JsonProperty("exchangeSetUrlExpiryDateTime")]
        public DateTime? ExchangeSetUrlExpiryDateTime { get; set; }

        [JsonProperty("requestedProductCount")]
        public int RequestedProductCount { get; set; }

        [JsonProperty("exchangeSetProductCount")]
        public int ExchangeSetProductCount { get; set; }

        [JsonProperty("requestedProductsAlreadyUpToDateCount")]
        public int RequestedProductsAlreadyUpToDateCount { get; set; }

        [JsonProperty("requestedProductsNotInExchangeSet")]
        public List<RequestedProductNotInExchangeSet> RequestedProductsNotInExchangeSet { get; set; }

        [JsonProperty("fssBatchId")]
        public string FssBatchId { get; set; }
    }
}
