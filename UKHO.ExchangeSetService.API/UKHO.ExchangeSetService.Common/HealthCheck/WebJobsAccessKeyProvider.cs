using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    [ExcludeFromCodeCoverage]
    public class WebJobsAccessKeyProvider : IWebJobsAccessKeyProvider
    {
        private readonly IDictionary<string, string> WebJobsAccessKey;
        public WebJobsAccessKeyProvider(IConfiguration config)
        {
            WebJobsAccessKey = config.AsEnumerable()
                                .ToList()
                                .FindAll(kv => kv.Key != null)
                                .ToDictionary(x => x.Key, x => x.Value);
        }

        public string GetWebJobsAccessKey(string keyName)
        {
            if (WebJobsAccessKey.TryGetValue(keyName, out var accessKey))
            {
                return accessKey;
            }
            return null;
        }
    }
}
