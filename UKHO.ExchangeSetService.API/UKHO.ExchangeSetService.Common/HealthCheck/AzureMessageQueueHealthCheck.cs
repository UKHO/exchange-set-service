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
    public class AzureMessageQueueHealthCheck : IHealthCheck
    {
        private readonly IAzureMessageQueueHelper azureMessageQueueHelper;
        private readonly ISalesCatalogueStorageService scsStorageService;
        private readonly IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration;
        private readonly ILogger<AzureMessageQueueHelper> logger;

        public AzureMessageQueueHealthCheck(IAzureMessageQueueHelper azureMessageQueueHelper,
                                            ISalesCatalogueStorageService scsStorageService,
                                            IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration,
                                            ILogger<AzureMessageQueueHelper> logger)
        {
            this.azureMessageQueueHelper = azureMessageQueueHelper;
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
                var messageQueuesHealth = CheckAllMessageQueuesHealth();
                await Task.WhenAll(messageQueuesHealth);
                watch.Stop();

                if (messageQueuesHealth.Result.Status == HealthStatus.Healthy)
                {
                    logger.LogInformation(EventIds.AzureMessageQueueIsHealthy.ToEventId(), $"Azure message queue is healthy, time spent to check this is {watch.ElapsedMilliseconds}ms");
                    return HealthCheckResult.Healthy("Azure message queue is healthy");
                }
                else
                {
                    logger.LogError(EventIds.AzureMessageQueueIsUnhealthy.ToEventId(), $"Azure message queue is unhealthy with error {messageQueuesHealth.Result.Exception.Message}, time spent to check this is {watch.ElapsedMilliseconds}ms");
                    return HealthCheckResult.Unhealthy("Azure message queue is unhealthy");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.AzureMessageQueueIsUnhealthy.ToEventId(), $"Azure message queue is unhealthy with error {ex.Message}");
                return HealthCheckResult.Unhealthy("Azure message queue is unhealthy");
            }
        }

        private async Task<HealthCheckResult> CheckAllMessageQueuesHealth()
        {
            string[] exchangeSetTypes = essFulfilmentStorageConfiguration.Value.ExchangeSetTypes.Split(",");
            string storageAccountConnectionString = string.Empty;

            var queueName = string.Empty;

            HealthCheckResult messageQueueHealthStatus = new HealthCheckResult(HealthStatus.Healthy);
            foreach (string exchangeSetType in exchangeSetTypes)
            {
                for (int i = 1; i <= GetInstanceCount(exchangeSetType); i++)
                {
                    queueName = string.Format(essFulfilmentStorageConfiguration.Value.DynamicQueueName, i);
                    var storageAccountWithKey = GetStorageAccountNameAndKey(exchangeSetType);
                    storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString(storageAccountWithKey.Item1, storageAccountWithKey.Item2);
                    messageQueueHealthStatus = await azureMessageQueueHelper.CheckMessageQueueHealth(storageAccountConnectionString, queueName);
                    if (messageQueueHealthStatus.Status == HealthStatus.Unhealthy)
                        break;
                }
            }
            return messageQueueHealthStatus;
        }

        private (string, string) GetStorageAccountNameAndKey(string exchangeSetType)
        {
            switch (exchangeSetType)
            {
                case "sxs":
                    return (essFulfilmentStorageConfiguration.Value.SmallExchangeSetAccountName, essFulfilmentStorageConfiguration.Value.SmallExchangeSetAccountKey);
                case "mxs":
                    return (essFulfilmentStorageConfiguration.Value.MediumExchangeSetAccountName, essFulfilmentStorageConfiguration.Value.MediumExchangeSetAccountKey);
                case "lxs":
                    return (essFulfilmentStorageConfiguration.Value.LargeExchangeSetAccountName, essFulfilmentStorageConfiguration.Value.LargeExchangeSetAccountKey);
                default:
                    return (string.Empty, string.Empty);
            }
        }

        private int GetInstanceCount(string exchangeSetType)
        {
            switch (exchangeSetType)
            {
                case "sxs":
                    return essFulfilmentStorageConfiguration.Value.SmallExchangeSetInstance;
                case "mxs":
                    return essFulfilmentStorageConfiguration.Value.MediumExchangeSetInstance;
                case "lxs":
                    return essFulfilmentStorageConfiguration.Value.LargeExchangeSetInstance;
                default:
                    return 1;
            }
        }
    }
}
