using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage] ////Excluded from code coverage as it has ADD interaction
    public class AuthTokenProvider
    {
        private readonly IOptions<EssManagedIdentityConfiguration> essManagedIdentityConfiguration;
        private readonly ILogger<AuthTokenProvider> logger;
        private static readonly Object _lock = new Object();
        private readonly IDistributedCache _cache;

        public AuthTokenProvider(IOptions<EssManagedIdentityConfiguration> essManagedIdentityConfiguration, IDistributedCache _cache, ILogger<AuthTokenProvider> logger)
        {
            this.essManagedIdentityConfiguration = essManagedIdentityConfiguration;
            this._cache = _cache;
            this.logger = logger;
        }


        public async Task<string> GetManagedIdentityAuthAsync(string resource)
        {
            var accessToken = GetFromCache(resource);

            if (accessToken.AccessToken != null && accessToken.ExpiresIn > DateTime.UtcNow)
            {
                return accessToken.AccessToken;
            }

            logger.LogInformation(EventIds.CachingExternalEndPointToken.ToEventId(), "Caching new token for external end point and expires in {ExpiresIn}ms", accessToken.ExpiresIn.Millisecond);

            var newAccessToken = await GetNewManagedIdentityAuthAsync(resource);
            AddToCache(resource, newAccessToken);

            return newAccessToken.AccessToken;
        }

        private async Task<AccessTokenItem> GetNewManagedIdentityAuthAsync(string resource)
        {
            var tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = essManagedIdentityConfiguration.Value.ClientId });
            var accessToken = await tokenCredential.GetTokenAsync(
                new TokenRequestContext(scopes: new string[] { resource + "/.default" }) { }
            );

            return new AccessTokenItem
            {
                ExpiresIn = DateTime.UtcNow.AddSeconds(accessToken.ExpiresOn.Millisecond),
                AccessToken = accessToken.Token
            };
        }

        private void AddToCache(string key, AccessTokenItem accessTokenItem)
        {
            var options = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(essManagedIdentityConfiguration.Value.TokenExpiryTimeInMunites));

            lock (_lock)
            {
                _cache.SetString(key, JsonConvert.SerializeObject(accessTokenItem), options);
            }
        }

        private AccessTokenItem GetFromCache(string key)
        {
            var item = _cache.GetString(key);
            if (item != null)
            {
                return JsonConvert.DeserializeObject<AccessTokenItem>(item);
            }

            return null;
        }
    }
}
