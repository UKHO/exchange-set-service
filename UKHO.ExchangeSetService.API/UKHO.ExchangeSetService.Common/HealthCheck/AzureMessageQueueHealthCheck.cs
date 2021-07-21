using Azure.Storage.Queues;
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
    public class AzureMessageQueueHealthCheck : IHealthCheck
    {
        private readonly ISalesCatalogueStorageService scsStorageService;
        private readonly IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration;
        private readonly ILogger<AzureMessageQueueHelper> logger;

        public AzureMessageQueueHealthCheck(ISalesCatalogueStorageService scsStorageService,
                                           IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration,
                                           ILogger<AzureMessageQueueHelper> logger)
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
                QueueClient queueClient = new QueueClient(storageAccountConnectionString, essFulfilmentStorageConfiguration.Value.QueueName);
                var queueMessage = queueClient.PeekMessage();

                if (queueMessage != null && queueMessage.GetRawResponse().ReasonPhrase == "OK")
                {
                    logger.LogDebug("Azure message queue is healthy");
                    return HealthCheckResult.Healthy("Azure message queue is healthy");
                }
                else
                {
                    logger.LogError("Azure message queue is unhealthy");
                    return HealthCheckResult.Unhealthy("Azure message queue is unhealthy");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Azure message queue is unhealthy with error " + ex.Message);
                return HealthCheckResult.Unhealthy("Azure message queue is unhealthy");
            }
        }
    }
}
