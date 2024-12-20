// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.Common.Models.V2.Response
{
    public class ExchangeSetStandardResponse
    {
        [JsonProperty("_links")]
        public Links Links { get; set; }

        [JsonProperty("exchangeSetUrlExpiryDateTime")]
        public DateTime? ExchangeSetUrlExpiryDateTime { get; set; }

        [JsonProperty("requestedProductCount")]
        public int RequestedProductCount { get; set; }

        [JsonProperty("exchangeSetProductCount")]
        public int ExchangeSetProductCount { get; set; }

        [JsonProperty("requestedProductsAlreadyUpToDateCount")]
        public int RequestedProductsAlreadyUpToDateCount { get; set; }

        [JsonProperty("requestedProductsNotInExchangeSet")]
        public List<RequestedProductsNotInExchangeSet> RequestedProductsNotInExchangeSet { get; set; }

        [JsonProperty("fssBatchId")]
        public string BatchId { get; set; }
    }
}
