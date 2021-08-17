using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class AzureWebJobsHealthCheck : IHealthCheck
    {
        private readonly IAzureWebJobsHealthCheckClient azureWebJobsHealthCheckClient;
        private readonly ILogger<AzureWebJobsHealthCheck> logger;

        public AzureWebJobsHealthCheck(IAzureWebJobsHealthCheckClient azureWebJobsHealthCheckClient,
                                       ILogger<AzureWebJobsHealthCheck> logger)
        {
            this.azureWebJobsHealthCheckClient = azureWebJobsHealthCheckClient;
            this.logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var healthCheckResult = await azureWebJobsHealthCheckClient.CheckHealthAsync(context);
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
    }
}