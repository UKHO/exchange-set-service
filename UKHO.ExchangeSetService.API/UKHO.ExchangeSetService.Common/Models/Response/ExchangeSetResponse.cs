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
}
