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
    public class AzureMessageQueueHealthCheck : IHealthCheck
    {
        private readonly IAzureMessageQueueHelper azureMessageQueueHelper;
        private readonly ISalesCatalogueStorageService scsStorageService;
        private readonly IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration;
        private readonly ILogger<AzureMessageQueueHelper> logger;
        private readonly IAzureBlobStorageService azureBlobStorageService;

        public AzureMessageQueueHealthCheck(IAzureMessageQueueHelper azureMessageQueueHelper,
                                            ISalesCatalogueStorageService scsStorageService,
                                            IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration,
                                            ILogger<AzureMessageQueueHelper> logger,
                                            IAzureBlobStorageService azureBlobStorageService)
        {
            this.azureMessageQueueHelper = azureMessageQueueHelper;
            this.scsStorageService = scsStorageService;
            this.essFulfilmentStorageConfiguration = essFulfilmentStorageConfiguration;
            this.logger = logger;
            this.azureBlobStorageService = azureBlobStorageService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var messageQueuesHealth = CheckAllMessageQueuesHealth();
                await Task.WhenAll(messageQueuesHealth);

                if (messageQueuesHealth.Result.Status == HealthStatus.Healthy)
                {
                    logger.LogDebug(EventIds.AzureMessageQueueIsHealthy.ToEventId(), "Azure message queue is healthy");
                    return HealthCheckResult.Healthy("Azure message queue is healthy");
                }

                logger.LogError(EventIds.AzureMessageQueueIsUnhealthy.ToEventId(), messageQueuesHealth.Exception, "Azure message queue is unhealthy with error {Message}", messageQueuesHealth.Result.Exception.Message);
                return HealthCheckResult.Unhealthy("Azure message queue is unhealthy", messageQueuesHealth.Exception);
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.EventHubLoggingIsUnhealthy.ToEventId(), ex, "Health check for Azure Message Queue threw an exception");
                return HealthCheckResult.Unhealthy("Health check for Azure Message Queue threw an exception", ex);
            }
        }

        private async Task<HealthCheckResult> CheckAllMessageQueuesHealth()
        {
            string[] exchangeSetTypes = essFulfilmentStorageConfiguration.Value.ExchangeSetTypes.Split(",");

            HealthCheckResult messageQueueHealthStatus = new HealthCheckResult(HealthStatus.Healthy, "Azure message queue is healthy");
            foreach (string exchangeSetTypeName in exchangeSetTypes)
            {
                Enum.TryParse(exchangeSetTypeName, out ExchangeSetType exchangeSetType);
                for (int i = 1; i <= azureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(exchangeSetType); i++)
                {
                    var queueName = string.Format(essFulfilmentStorageConfiguration.Value.DynamicQueueName, i);
                    var storageAccountWithKey = azureBlobStorageService.GetStorageAccountNameAndKeyBasedOnExchangeSetType(exchangeSetType);
                    var storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString(storageAccountWithKey.Item1, storageAccountWithKey.Item2);
                    messageQueueHealthStatus = await azureMessageQueueHelper.CheckMessageQueueHealth(storageAccountConnectionString, queueName);
                    if (messageQueueHealthStatus.Status == HealthStatus.Unhealthy)
                    {
                        logger.LogError(EventIds.AzureMessageQueueIsUnhealthy.ToEventId(), messageQueueHealthStatus.Exception, "Azure message queue {queueName} is unhealthy", queueName);
                        return messageQueueHealthStatus;
                    }
                }
            }
            logger.LogDebug(EventIds.AzureMessageQueueIsHealthy.ToEventId(), "Azure message queue is healthy");
            return messageQueueHealthStatus;
        }
    }
}
