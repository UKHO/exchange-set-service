using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class ExchangeSetResponse
    {
        [JsonProperty("_links")]
        public Links Links { get; set; }

        [JsonProperty("exchangeSetUrlExpiryDateTime")]
        public DateTime? ExchangeSetUrlExpiryDateTime { get; set; }

        [JsonProperty("requestedProductCount")]
        public int RequestedProductCount { get; set; }

        [JsonProperty("exchangeSetCellCount")]
        public int ExchangeSetCellCount { get; set; }

        [JsonProperty("requestedProductsAlreadyUpToDateCount")]
        public int RequestedProductsAlreadyUpToDateCount { get; set; }

        [JsonProperty("requestedAioProductCount", NullValueHandling = NullValueHandling.Ignore)]
        public int? RequestedAioProductCount { get; set; } = null;

        [JsonProperty("aioExchangeSetCellCount", NullValueHandling = NullValueHandling.Ignore)]
        public int? AioExchangeSetCellCount { get; set; } = null;

        [JsonProperty("RequestedAioProductsAlreadyUpToDateCount", NullValueHandling = NullValueHandling.Ignore)]
        public int? RequestedAioProductsAlreadyUpToDateCount { get; set; } = null;

        [JsonProperty("requestedProductsNotInExchangeSet")]
        public List<RequestedProductsNotInExchangeSet> RequestedProductsNotInExchangeSet { get; set; }
        [JsonProperty("fssBatchId")]
        public string BatchId { get; set; }
    }
}