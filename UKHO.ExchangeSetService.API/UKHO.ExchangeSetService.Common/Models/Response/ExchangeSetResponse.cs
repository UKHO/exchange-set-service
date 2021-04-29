using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class ExchangeSetResponse
    {
       
        [JsonProperty("_links")]
        public Links Links { get; set; }
        
        public DateTime? ExchangeSetUrlExpiryDateTime { get; set; }

        public int RequestedProductCount { get; set; }

        public int ExchangeSetCellCount { get; set; }

        public int RequestedProductsAlreadyUpToDateCount { get; set; }

        public IEnumerable<RequestedProductsNotInExchangeSet> RequestedProductsNotInExchangeSet { get; set; }
    }

    public class Links
    {
        public LinkSetBatchStatusUri ExchangeSetBatchStatusUri { get; set; }

        public LinkSetFileUri ExchangeSetFileUri { get; set; }
    }

    public class LinkSetBatchStatusUri
    {
        public string Href { get; set; }
    }

    public class LinkSetFileUri
    {
        public string Href { get; set; }
    }

    public class RequestedProductsNotInExchangeSet
    {
        public string ProductName { get; set; }

        public string Reason { get; set; }
    }
}
