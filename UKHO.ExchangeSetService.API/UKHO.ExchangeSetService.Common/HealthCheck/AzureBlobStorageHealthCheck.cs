using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class AzureBlobStorageHealthCheck : IHealthCheck
    {
        private readonly ISalesCatalogueStorageService scsStorageService;
        private readonly IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration;
        private readonly ILogger<AzureBlobStorageService> logger;

        public AzureBlobStorageHealthCheck(ISalesCatalogueStorageService scsStorageService,
                                           IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration,
                                           ILogger<AzureBlobStorageService> logger)
        {
            this.scsStorageService = scsStorageService;
            this.essFulfilmentStorageConfiguration = essFulfilmentStorageConfiguration;
            this.logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.CompletedTask;
                string storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();
                BlobContainerClient container = new BlobContainerClient(storageAccountConnectionString, essFulfilmentStorageConfiguration.Value.StorageContainerName);
                var blobContainerProperties = container.GetProperties().GetRawResponse();

                if (blobContainerProperties != null && blobContainerProperties.ReasonPhrase == "OK")
                {
                    logger.LogDebug("Azure blob storage is healthy");
                    return HealthCheckResult.Healthy("Azure blob storage is healthy");
                }
                else
                {
                    logger.LogError("Azure blob storage is unhealthy");
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
