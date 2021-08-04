using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class AzureWebJobsHealthCheck : IHealthCheck
    {
        private readonly IAzureWebJobsHealthCheck azureWebJobsHealthCheckClient;
        private readonly ILogger<AzureWebJobsHealthCheck> logger;

        public AzureWebJobsHealthCheck(IAzureWebJobsHealthCheck azureWebJobsHealthCheckClient,
                                       ILogger<AzureWebJobsHealthCheck> logger)
        {
            this.azureWebJobsHealthCheckClient = azureWebJobsHealthCheckClient;
            this.logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var healthCheckResult = await azureWebJobsHealthCheckClient.CheckHealthAsync(context);
            watch.Stop();
            if (healthCheckResult.Status == HealthStatus.Healthy)
            {
                logger.LogInformation(EventIds.AzureWebJobsIsHealthy.ToEventId(), $"Azure webjob is healthy, time spent to check this is {watch.ElapsedMilliseconds}ms");
            }
            else
            {
                logger.LogError(EventIds.AzureWebJobsIsUnhealthy.ToEventId(), healthCheckResult.Exception, $"Azure webjob is unhealthy with error {healthCheckResult.Exception.Message}, time spent to check this is {watch.ElapsedMilliseconds}ms");
            }
            return healthCheckResult;
        }
    }
}