using System;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    public class EssS100ResponseModel
    {
        public class ExchangeSetLinks
        {
            public Uri ExchangeSetBatchStatusUri { get; set; }
            public Uri ExchangeSetBatchDetailsUri { get; set; }
            public Uri ExchangeSetFileUri { get; set; }
        }

        public class RequestedProductNotInExchangeSet
        {
            public string ProductName { get; set; }
            public string Reason { get; set; }
        }

        public class ExchangeSetBatch
        {
            public ExchangeSetLinks Links { get; set; }
            public DateTime? ExchangeSetUrlExpiryDateTime { get; set; }
            public int RequestedProductCount { get; set; }
            public int ExchangeSetProductCount { get; set; }
            public int RequestedProductsAlreadyUpToDateCount { get; set; }
            public List<RequestedProductNotInExchangeSet> RequestedProductsNotInExchangeSet { get; set; }
            public string FssBatchId { get; set; }
        }
    }
}
