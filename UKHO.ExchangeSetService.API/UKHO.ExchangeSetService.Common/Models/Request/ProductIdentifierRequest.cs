namespace UKHO.ExchangeSetService.Common.Models.Request
{
    public class ProductIdentifierRequest
    {
        public string[] ProductIdentifier { get; set; }
        public string CallbackUri { get; set; }
        public string ExchangeSetStandard { get; set; }
        public string CorrelationId { get; set; }
        public string ExchangeSetLayout { get; set; }
    }
}
