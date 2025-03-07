using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.Common.Helpers.Auth
{
    [ExcludeFromCodeCoverage] ////Excluded from code coverage as it has AD interaction
    public class AuthScsTokenProvider : AuthTokenProvider, IAuthScsTokenProvider
    {
        public AuthScsTokenProvider(IOptions<EssManagedIdentityConfiguration> essManagedIdentityConfiguration, IDistributedCache _cache, ILogger<AuthScsTokenProvider> logger) :
            base(essManagedIdentityConfiguration, _cache, logger)
        {
        }
    }
}
