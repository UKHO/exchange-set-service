namespace UKHO.ExchangeSetService.Common.Models.V2.Request
{
    public class ProductNameRequest
    {
        public string[] ProductNames { get; set; }
        public string CallbackUri { get; set; }
        public string CorrelationId { get; set; }
    }
}
