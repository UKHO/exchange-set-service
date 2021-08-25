using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class AuthFssTokenProvider : AuthTokenProvider, IAuthFssTokenProvider
    {
        public AuthFssTokenProvider(IOptions<EssManagedIdentityConfiguration> essManagedIdentityConfiguration, IDistributedCache _cache, ILogger<AuthFssTokenProvider> logger) : 
            base(essManagedIdentityConfiguration, _cache, logger)
        {
        }
    }
}
