using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class ExchangeSetErrorResponse
    {
        [JsonProperty("_links")]
        public CallBackUri Links { get; set; }

        [JsonProperty("exchangeSetUrlExpiryDateTime")]
        public DateTime? ExchangeSetUrlExpiryDateTime { get; set; }

        [JsonProperty("requestedProductCount")]
        public int RequestedProductCount { get; set; }

        [JsonProperty("exchangeSetCellCount")]
        public int ExchangeSetCellCount { get; set; }

        [JsonProperty("requestedProductsAlreadyUpToDateCount")]
        public int RequestedProductsAlreadyUpToDateCount { get; set; }

        [JsonProperty("requestedProductsNotInExchangeSet")]
        public IEnumerable<RequestedProductsNotInExchangeSet> RequestedProductsNotInExchangeSet { get; set; }
    }
}
