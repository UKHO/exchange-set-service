using System;

namespace UKHO.ExchangeSetService.Common.Models.SalesCatalogue
{
    public class SalesCatalogueServiceResponseQueueMessage
    {
        public string BatchId { get; set; }
        public long FileSize { get; set; }
        public string ScsResponseUri { get; set; }
        public string CallbackUri { get; set; }
        public string ExchangeSetStandard { get; set; }
        public string CorrelationId { get; set; }
        public string ExchangeSetUrlExpiryDate { get; set; }
        public DateTime ScsRequestDateTime { get; set; }
        public bool IsEmptyEncExchangeSet { get; set; }
        public bool IsEmptyAioExchangeSet { get; set; }
        public int RequestedProductCount { get; set; }
        public int RequestedAioProductCount { get; set; }

        public int RequestedProductsAlreadyUpToDateCount { get; set; }

        public int RequestedAioProductsAlreadyUpToDateCount { get; set; }
    }
}