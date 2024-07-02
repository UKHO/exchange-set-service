namespace UKHO.ExchangeSetService.Common.Models.Request
{
    public class ScsProductIdentifierRequest
    {
        public string[] ProductIdentifier { get; set; }
        public string CorrelationId { get; set; }
    }
}