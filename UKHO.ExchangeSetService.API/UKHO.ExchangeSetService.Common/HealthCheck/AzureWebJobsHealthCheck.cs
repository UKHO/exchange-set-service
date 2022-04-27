using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class AzureWebJobsHealthCheck : IHealthCheck
    {
        private readonly IAzureWebJobsHealthCheckService azureWebJobsHealthCheckService;
        private readonly ILogger<AzureWebJobsHealthCheck> logger;

        public AzureWebJobsHealthCheck(IAzureWebJobsHealthCheckService azureWebJobsHealthCheckService,
                                       ILogger<AzureWebJobsHealthCheck> logger)
        {
            this.azureWebJobsHealthCheckService = azureWebJobsHealthCheckService;
            this.logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var healthCheckResult = await azureWebJobsHealthCheckService.CheckHealthAsync(cancellationToken);
                if (healthCheckResult.Status == HealthStatus.Healthy)
                {
                    logger.LogDebug(EventIds.AzureWebJobIsHealthy.ToEventId(), "Azure webjob is healthy");
                }
                else
                {
                    logger.LogError(EventIds.AzureWebJobIsUnhealthy.ToEventId(), healthCheckResult.Exception, "Azure webjob is unhealthy with error {Message}", healthCheckResult.Exception.Message);
                }
                return healthCheckResult;
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.AzureWebJobIsUnhealthy.ToEventId(), ex, "Health check for Azure Webjob threw an exception");
                return HealthCheckResult.Unhealthy("Health check for Azure Webjob threw an exception", ex);
            }
        }
    }
}