using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class AzureAdB2CConfiguration
    {
        public string Instance { get; set; }
        public string Domain { get; set; }
        public string ClientId { get; set; }
        public string SignUpSignInPolicy { get; set; }
        public string TenantId { get; set; }
    }
}