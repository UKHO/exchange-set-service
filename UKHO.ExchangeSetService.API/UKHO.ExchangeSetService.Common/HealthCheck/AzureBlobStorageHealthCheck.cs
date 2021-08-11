using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class AzureBlobStorageHealthCheck : IHealthCheck
    {
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly ISalesCatalogueStorageService scsStorageService;
        private readonly IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration;
        private readonly ILogger<AzureBlobStorageService> logger;

        public AzureBlobStorageHealthCheck(IAzureBlobStorageClient azureBlobStorageClient,
                                           ISalesCatalogueStorageService scsStorageService,
                                           IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration,
                                           ILogger<AzureBlobStorageService> logger)
        {
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.scsStorageService = scsStorageService;
            this.essFulfilmentStorageConfiguration = essFulfilmentStorageConfiguration;
            this.logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                string[] exchangeSetTypes = essFulfilmentStorageConfiguration.Value.ExchangeSetTypes.Split(",");
                string storageAccountConnectionString = string.Empty;
                HealthCheckResult azureBlobStorageHealthStatus = new HealthCheckResult(HealthStatus.Healthy);
                foreach (string exchangeSetType in exchangeSetTypes)
                {
                    var storageAccountWithKey = GetStorageAccountNameAndKey(exchangeSetType);
                    storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString(storageAccountWithKey.Item1, storageAccountWithKey.Item2);
                    azureBlobStorageHealthStatus = await azureBlobStorageClient.CheckBlobContainerHealth(storageAccountConnectionString, essFulfilmentStorageConfiguration.Value.StorageContainerName);
                    if (azureBlobStorageHealthStatus.Status == HealthStatus.Unhealthy)
                        break;
                }
                if (azureBlobStorageHealthStatus.Status == HealthStatus.Healthy)
                {
                    logger.LogDebug(EventIds.AzureBlobStorageIsHealthy.ToEventId(), $"Azure blob storage is healthy");
                    return HealthCheckResult.Healthy("Azure blob storage is healthy");
                }
                else
                {
                    logger.LogError(EventIds.AzureBlobStorageIsUnhealthy.ToEventId(), $"Azure blob storage is unhealthy with error {azureBlobStorageHealthStatus.Exception.Message}");
                    return HealthCheckResult.Unhealthy("Azure blob storage is unhealthy");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Azure blob storage is unhealthy with error " + ex.Message);
                return HealthCheckResult.Unhealthy("Azure blob storage is unhealthy");
            }
        }

        private (string, string) GetStorageAccountNameAndKey(string exchangeSetType)
        {
            Enum.TryParse(exchangeSetType, out ExchangeSetType exchangeSetTypeName);
            switch (exchangeSetTypeName)
            {
                case ExchangeSetType.sxs:
                    return (essFulfilmentStorageConfiguration.Value.SmallExchangeSetAccountName, essFulfilmentStorageConfiguration.Value.SmallExchangeSetAccountKey);
                case ExchangeSetType.mxs:
                    return (essFulfilmentStorageConfiguration.Value.MediumExchangeSetAccountName, essFulfilmentStorageConfiguration.Value.MediumExchangeSetAccountKey);
                case ExchangeSetType.lxs:
                    return (essFulfilmentStorageConfiguration.Value.LargeExchangeSetAccountName, essFulfilmentStorageConfiguration.Value.LargeExchangeSetAccountKey);
                default:
                    return (string.Empty, string.Empty);
            }
        }
    }
}
