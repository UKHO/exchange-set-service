using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    public class ExchangeSetResponseModel
    {
        [JsonProperty("_links")]
        public Links Links { get; set; }

        public DateTime? ExchangeSetUrlExpiryDateTime { get; set; }

        [JsonProperty("requestedProductCount")]
        public int RequestedProductCount { get; set; }

        [JsonProperty("exchangeSetCellCount")]
        public int ExchangeSetCellCount { get; set; }

        [JsonProperty("requestedProductsAlreadyUpToDateCount")]
        public int RequestedProductsAlreadyUpToDateCount { get; set; }

        public IEnumerable<RequestedProductsNotInExchangeSet> RequestedProductsNotInExchangeSet { get; set; }

        [JsonProperty("requestedAioProductCount")]
        public int RequestedAioProductCount { get; set; }

        [JsonProperty("aioExchangeSetCellCount")]
        public int AioExchangeSetCellCount { get; set; }

        [JsonProperty("requestedAioProductsAlreadyUpToDateCount")]
        public int RequestedAioProductsAlreadyUpToDateCount { get; set; }
    }

    public class Links
    {

        public LinkSetBatchStatusUri ExchangeSetBatchStatusUri { get; set; }
        public LinkSetFileUri ExchangeSetFileUri { get; set; }
        public LinkSetAioFileUri AioExchangeSetFileUri { get; set; }

    }

    public class LinkSetBatchStatusUri
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }
    public class LinkSetFileUri
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    public class RequestedProductsNotInExchangeSet
    {
        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

    }

    public class LinkSetAioFileUri
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }
}
