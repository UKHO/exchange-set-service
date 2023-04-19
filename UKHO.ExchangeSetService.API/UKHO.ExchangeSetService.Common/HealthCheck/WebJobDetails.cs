using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    [ExcludeFromCodeCoverage]
    public class WebJobDetails
    {
        public string UserPassword { get; set; }
        public string WebJobUri { get; set; }
        public string ExchangeSetType { get; set; }
        public int Instance { get; set; }
    }
}
