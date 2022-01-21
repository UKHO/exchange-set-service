
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAzureAdB2CHelper
    {
        public bool IsAzureB2CUser(AzureAdB2C azureAdB2C, string correlationId);
    }
}
