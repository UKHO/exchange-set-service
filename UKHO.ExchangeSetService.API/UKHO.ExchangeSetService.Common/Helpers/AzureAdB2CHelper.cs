
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;

namespace UKHO.ExchangeSetService.Common.Helpers
{
   public class AzureAdB2CHelper :IAzureAdB2CHelper
    {
        private readonly ILogger<AzureAdB2CHelper> logger;
        private readonly IOptions<AzureAdB2CConfiguration> azureAdB2CConfiguration;
        private readonly IOptions<AzureADConfiguration> azureAdConfiguration;

        public AzureAdB2CHelper(ILogger<AzureAdB2CHelper> logger, IOptions<AzureAdB2CConfiguration> azureAdB2CConfiguration, IOptions<AzureADConfiguration> azureAdConfiguration)
        {
            this.logger = logger;
            this.azureAdB2CConfiguration = azureAdB2CConfiguration;
            this.azureAdConfiguration = azureAdConfiguration;
        }
        public bool IsAzureB2CUser(AzureAdB2C azureAdB2C, string correlationId)
        {
            bool isAzureB2CUser = false;
            string b2CAuthority = $"{azureAdB2CConfiguration.Value.Instance}{azureAdB2CConfiguration.Value.TenantId}/v2.0/";// for B2C Token
            string adB2CAuthority = $"{azureAdConfiguration.Value.MicrosoftOnlineLoginUrl}{azureAdB2CConfiguration.Value.TenantId}/v2.0";// for AdB2C Token
            string audience = azureAdB2CConfiguration.Value.ClientId;
            if (azureAdB2C.IssToken == b2CAuthority && azureAdB2C.AudToken == audience)
            {
                logger.LogInformation(EventIds.ESSB2CUserValidationEvent.ToEventId(), "Token passed was B2C token and its Azure B2C user for ClientId:{audience} for _X-Correlation-ID:{CorrelationId}", audience, correlationId);
                isAzureB2CUser = true;
            }
            else if (azureAdB2C.IssToken == adB2CAuthority && azureAdB2C.AudToken == audience)
            {
                logger.LogInformation(EventIds.ESSB2CUserValidationEvent.ToEventId(), "Token passed was AdB2C token and its Azure ADB2C user for ClientId:{audience} for _X-Correlation-ID:{CorrelationId}", audience,  correlationId);
                isAzureB2CUser = true;
            }
            return isAzureB2CUser;
        }
    }
}
