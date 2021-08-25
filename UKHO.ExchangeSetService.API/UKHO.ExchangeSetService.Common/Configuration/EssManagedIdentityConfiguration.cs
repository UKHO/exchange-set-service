using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class EssManagedIdentityConfiguration
    {
        public string ClientId { get; set; }
        public int TokenExpiryTimeInMunites { get; set; }
    }
}
