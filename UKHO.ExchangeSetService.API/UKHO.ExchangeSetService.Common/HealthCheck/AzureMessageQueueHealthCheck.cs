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
                string[] exchangeSetTypes = essFulfilmentStorageConfiguration.Value.ExchangeSetTypes.Split(",");
                string storageAccountConnectionString = string.Empty;
                
                var queueName = string.Format(essFulfilmentStorageConfiguration.Value.DynamicQueueName, 1);
                HealthCheckResult messageQueueHealthStatus = new HealthCheckResult(HealthStatus.Healthy);
                foreach (string exchangeSetType in exchangeSetTypes)
                {
                    var storageAccountWithKey = GetStorageAccountNameAndKey(exchangeSetType);
                    storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString(storageAccountWithKey.Item1, storageAccountWithKey.Item2);
                    messageQueueHealthStatus = await azureMessageQueueHelper.CheckMessageQueueHealth(storageAccountConnectionString, queueName);
                    if (messageQueueHealthStatus.Status == HealthStatus.Unhealthy)
                        break;
                }
                watch.Stop();

                if (messageQueueHealthStatus.Status == HealthStatus.Healthy)
                {
                    logger.LogInformation(EventIds.AzureMessageQueueIsHealthy.ToEventId(), $"Azure message queue is healthy, time spent to check this is {watch.ElapsedMilliseconds}ms");
                    return HealthCheckResult.Healthy("Azure message queue is healthy");
                }
                else
                {
                    logger.LogError(EventIds.AzureMessageQueueIsUnhealthy.ToEventId(), $"Azure message queue is unhealthy with error {messageQueueHealthStatus.Exception.Message}, time spent to check this is {watch.ElapsedMilliseconds}ms");
                    return HealthCheckResult.Unhealthy("Azure message queue is unhealthy");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.AzureMessageQueueIsUnhealthy.ToEventId(), $"Azure message queue is unhealthy with error {ex.Message}");
                return HealthCheckResult.Unhealthy("Azure message queue is unhealthy");
            }
        }

        private (string, string) GetStorageAccountNameAndKey(string exchangeSetType)
        {
            if (string.Compare(exchangeSetType, "sxs", true) == 0)
            {
                return (essFulfilmentStorageConfiguration.Value.SmallExchangeSetAccountName, essFulfilmentStorageConfiguration.Value.SmallExchangeSetAccountKey);
            }
            else if (string.Compare(exchangeSetType, "mxs", true) == 0)
            {
                return (essFulfilmentStorageConfiguration.Value.MediumExchangeSetAccountName, essFulfilmentStorageConfiguration.Value.MediumExchangeSetAccountKey);
            }
            else
            {
                return (essFulfilmentStorageConfiguration.Value.LargeExchangeSetAccountName, essFulfilmentStorageConfiguration.Value.LargeExchangeSetAccountKey);
            }
        }
    }
}
