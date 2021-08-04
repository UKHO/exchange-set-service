using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
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
                Stopwatch watch = new Stopwatch();
                watch.Start();
                string storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();
                var azureBlobStorageHealthStatus = await azureBlobStorageClient.CheckBlobContainerHealth(storageAccountConnectionString, essFulfilmentStorageConfiguration.Value.StorageContainerName);
                watch.Stop();
                if (azureBlobStorageHealthStatus.Status == HealthStatus.Healthy)
                {
                    logger.LogInformation(EventIds.AzureBlobStorageIsHealthy.ToEventId(), $"Azure blob storage is healthy, time spent to check this is {watch.ElapsedMilliseconds}ms");
                    return HealthCheckResult.Healthy("Azure blob storage is healthy");
                }
                else
                {
                    logger.LogError(EventIds.AzureBlobStorageIsUnhealthy.ToEventId(), "Azure blob storage is unhealthy, time spent to check this is {watch.ElapsedMilliseconds}ms");
                    return HealthCheckResult.Unhealthy("Azure blob storage is unhealthy");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Azure blob storage is unhealthy with error " + ex.Message);
                return HealthCheckResult.Unhealthy("Azure blob storage is unhealthy");
            }
        }
    }
}
