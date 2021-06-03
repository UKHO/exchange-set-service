namespace UKHO.ExchangeSetService.Common.Models.SalesCatalogue
{
    public class SalesCatalogueServiceResponseQueueMessage
    {
        public string BatchId { get; set; }
        public long FileSize { get; set; }
        public string ScsResponseUri { get; set; }
        public string CallbackUri { get; set; }
        public string CorrelationId { get; set; }
    }
}
