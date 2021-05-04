using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    /// <summary>
    /// This is response model class
    /// </summary>
    public class ExchangeSetResponseModel
    {
        /// <summary>
        /// Json property links
        /// </summary>
        [JsonProperty("_links")]
        public Links Links { get; set; }

        /// <summary>
        /// Json property DateTime
        /// </summary>
        public DateTime? ExchangeSetUrlExpiryDateTime { get; set; }

        /// <summary>
        /// Json property RequestedProductCount
        /// </summary>
        [JsonProperty("requestedProductCount")]
        public int RequestedProductCount { get; set; }

        /// <summary>
        /// Json property ExchangeSetCellCount
        /// </summary>
        [JsonProperty("exchangeSetCellCount")]
        public int ExchangeSetCellCount { get; set; }

        /// <summary>
        /// Json property RequestedProductsAlreadyUpToDateCount
        /// </summary>
        [JsonProperty("requestedProductsAlreadyUpToDateCount")]
        public int RequestedProductsAlreadyUpToDateCount { get; set; }

        /// <summary>
        /// Json property RequestedProductsNotInExchangeSet
        /// </summary>
        public IEnumerable<RequestedProductsNotInExchangeSet> RequestedProductsNotInExchangeSet { get; set; }
    }

    /// <summary>
    /// class Links
    /// </summary>
    public class Links
    {
        /// <summary>
        /// class LinkSetBatchStatusUri instance
        /// </summary>
        public LinkSetBatchStatusUri ExchangeSetBatchStatusUri { get; set; }

        /// <summary>
        /// class LinkSetFileUri instance
        /// </summary>
        public LinkSetFileUri ExchangeSetFileUri { get; set; }
    }

    /// <summary>
    /// class LinkSetBatchStatusUri
    /// </summary>
    public class LinkSetBatchStatusUri
    {
        /// <summary>
        /// Json property Href
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// class LinkSetFileUri
    /// </summary>
    public class LinkSetFileUri
    {
        /// <summary>
        /// Json property Href
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// class RequestedProductsNotInExchangeSet
    /// </summary>
    public class RequestedProductsNotInExchangeSet
    {
        /// <summary>
        /// Json property productName
        /// </summary>
        [JsonProperty("productName")]
        public string ProductName { get; set; }


        /// <summary>
        /// Json property reason
        /// </summary>
        [JsonProperty("reason")]
        public string Reason { get; set; }

    }
}
