namespace UKHO.ExchangeSetService.Common.Models.Request
{
    public class ProductIdentifierRequest
    {
        public string[] ProductIdentifier { get; set; }
        public string CallbackUri { get; set; }
        public bool IsUnencrypted { get; set; }
        public string CorrelationId { get; set; }
    }
}