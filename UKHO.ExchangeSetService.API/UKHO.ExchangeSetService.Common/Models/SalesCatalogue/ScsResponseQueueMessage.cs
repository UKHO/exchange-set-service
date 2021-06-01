namespace UKHO.ExchangeSetService.Common.Models.SalesCatalogue
{
    public class ScsResponseQueueMessage
    {
        public string BatchId { get; set; }
        public long FileSize { get; set; }
        public string ScsResponseUri { get; set; }
    }
}
