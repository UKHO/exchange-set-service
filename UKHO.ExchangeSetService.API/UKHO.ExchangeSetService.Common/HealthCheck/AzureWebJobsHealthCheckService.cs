using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.Enums;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class AzureWebJobsHealthCheckService(
        IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration,
        IWebJobsAccessKeyProvider webJobsAccessKeyProvider,
        IWebHostEnvironment webHostEnvironment,
        IAzureBlobStorageService azureBlobStorageService,
        IAzureWebJobsHealthCheckClient azureWebJobsHealthCheckClient
        ) : IAzureWebJobsHealthCheckService
    {
        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var exchangeSetTypes = essFulfilmentStorageConfiguration.Value.ExchangeSetTypes.Split(",");
            var webAppVersion = essFulfilmentStorageConfiguration.Value.WebAppVersion;
            string userNameKey, passwordKey, webJobUri;
            var webJobs = new List<WebJobDetails>();

            foreach (var exchangeSetTypeName in exchangeSetTypes)
            {
                if (!Enum.TryParse(exchangeSetTypeName, out ExchangeSetType exchangeSetType))
                {
                    throw new ConfigurationErrorsException($"Invalid EssFulfilmentStorageConfiguration.ExchangeSetType: {exchangeSetTypeName}");
                }

                var zoneRedundantPartialName = GetZoneRedundantPartialName(exchangeSetType);

                for (var instance = 1; instance <= azureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(exchangeSetType); instance++)
                {
                    if (!webAppVersion.Equals("v2", StringComparison.InvariantCultureIgnoreCase))
                    {
                        userNameKey =
                            $"ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{instance}{zoneRedundantPartialName}-webapp-scm-username";
                        passwordKey =
                            $"ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{instance}{zoneRedundantPartialName}-webapp-scm-password";
                        webJobUri =
                            $"https://ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{instance}{zoneRedundantPartialName}-webapp.scm.azurewebsites.net/api/continuouswebjobs/ESSFulfilmentWebJob";
                    }
                    else
                    {
                        userNameKey =
                            $"ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{instance}{zoneRedundantPartialName}-webapp-v2-scm-username";
                        passwordKey =
                            $"ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{instance}{zoneRedundantPartialName}-webapp-v2-scm-password";
                        webJobUri =
                            $"https://ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{instance}{zoneRedundantPartialName}-webapp-v2.scm.azurewebsites.net/api/continuouswebjobs/ESSFulfilmentWebJob";
                    }

                    var userPassword = webJobsAccessKeyProvider.GetWebJobsAccessKey(userNameKey) + ":" + webJobsAccessKeyProvider.GetWebJobsAccessKey(passwordKey);
                    userPassword = Convert.ToBase64String(Encoding.Default.GetBytes(userPassword));
                    var webJobDetails = new WebJobDetails
                    {
                        UserPassword = userPassword,
                        WebJobUri = webJobUri,
                        ExchangeSetType = exchangeSetTypeName,
                        Instance = instance
                    };
                    webJobs.Add(webJobDetails);
                }
            }

            var webJobsHealth = azureWebJobsHealthCheckClient.CheckAllWebJobsHealth(webJobs);
            await webJobsHealth;

            return webJobsHealth.Result;
        }

        private string GetZoneRedundantPartialName(ExchangeSetType exchangeSetType)
        {
            if ((exchangeSetType == ExchangeSetType.sxs && essFulfilmentStorageConfiguration.Value.SmallExchangeSetZoneRedundant) ||
                (exchangeSetType == ExchangeSetType.mxs && essFulfilmentStorageConfiguration.Value.MediumExchangeSetZoneRedundant) ||
                (exchangeSetType == ExchangeSetType.lxs && essFulfilmentStorageConfiguration.Value.LargeExchangeSetZoneRedundant))
            {
                return "-zr";
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
