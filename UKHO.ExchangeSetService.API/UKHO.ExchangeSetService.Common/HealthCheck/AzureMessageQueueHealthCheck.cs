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
                string storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();
                var messageQueueHealthStatus = await azureMessageQueueHelper.CheckMessageQueueHealth(storageAccountConnectionString, essFulfilmentStorageConfiguration.Value.QueueName);
                watch.Stop();
                if (messageQueueHealthStatus.Status == HealthStatus.Healthy)
                {
                    logger.LogInformation(EventIds.AzureMessageQueueIsHealthy.ToEventId(), $"Azure message queue is healthy, time spent to check this is {watch.ElapsedMilliseconds}ms");
                    return HealthCheckResult.Healthy("Azure message queue is healthy");
                }
                else
                {
                    logger.LogError(EventIds.AzureMessageQueueIsUnhealthy.ToEventId(), $"Azure message queue is unhealthy, time spent to check this is {watch.ElapsedMilliseconds}ms");
                    return HealthCheckResult.Unhealthy("Azure message queue is unhealthy");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.AzureMessageQueueIsUnhealthy.ToEventId(), "Azure message queue is unhealthy with error {Message}" + ex.Message);
                return HealthCheckResult.Unhealthy("Azure message queue is unhealthy");
            }
        }
    }
}
