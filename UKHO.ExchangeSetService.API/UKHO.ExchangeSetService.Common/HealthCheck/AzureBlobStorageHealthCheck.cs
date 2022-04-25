using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class AzureBlobStorageHealthCheck : IHealthCheck
    {
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly ISalesCatalogueStorageService scsStorageService;
        private readonly IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration;
        private readonly ILogger<AzureBlobStorageService> logger;
        private readonly IAzureBlobStorageService azureBlobStorageService;

        public AzureBlobStorageHealthCheck(IAzureBlobStorageClient azureBlobStorageClient,
                                           ISalesCatalogueStorageService scsStorageService,
                                           IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration,
                                           ILogger<AzureBlobStorageService> logger,
                                           IAzureBlobStorageService azureBlobStorageService)
        {
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.scsStorageService = scsStorageService;
            this.essFulfilmentStorageConfiguration = essFulfilmentStorageConfiguration;
            this.logger = logger;
            this.azureBlobStorageService = azureBlobStorageService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                string[] exchangeSetTypes = essFulfilmentStorageConfiguration.Value.ExchangeSetTypes.Split(",");
                string storageAccountConnectionString = string.Empty;
                HealthCheckResult azureBlobStorageHealthStatus = new HealthCheckResult(HealthStatus.Healthy, "Azure blob storage is healthy");
                foreach (string exchangeSetType in exchangeSetTypes)
                {
                    Enum.TryParse(exchangeSetType, out ExchangeSetType exchangeSetTypeName);
                    var storageAccountWithKey = azureBlobStorageService.GetStorageAccountNameAndKeyBasedOnExchangeSetType(exchangeSetTypeName);
                    storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString(storageAccountWithKey.Item1, storageAccountWithKey.Item2);
                    azureBlobStorageHealthStatus = await azureBlobStorageClient.CheckBlobContainerHealth(storageAccountConnectionString, essFulfilmentStorageConfiguration.Value.StorageContainerName);
                    if (azureBlobStorageHealthStatus.Status == HealthStatus.Unhealthy)
                    {
                        logger.LogError(EventIds.AzureBlobStorageIsUnhealthy.ToEventId(), azureBlobStorageHealthStatus.Exception, "Azure blob storage is unhealthy for exchangeSetType: {exchangeSetType} with error {Message}", exchangeSetType, azureBlobStorageHealthStatus.Exception.Message);
                        azureBlobStorageHealthStatus = HealthCheckResult.Unhealthy("Azure blob storage is unhealthy", azureBlobStorageHealthStatus.Exception);
                        return azureBlobStorageHealthStatus;
                    }
                }
                logger.LogDebug(EventIds.AzureBlobStorageIsHealthy.ToEventId(), "Azure blob storage is healthy");
                return azureBlobStorageHealthStatus;
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.EventHubLoggingIsUnhealthy.ToEventId(), ex, "Health check for Azure Blob Storage threw an exception");
                return HealthCheckResult.Unhealthy("Health check for Azure Blob Storage threw an exception", ex);
            }
        }
    }
}
