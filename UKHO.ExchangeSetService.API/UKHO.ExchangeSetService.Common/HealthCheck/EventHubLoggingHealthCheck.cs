using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class EventHubLoggingHealthCheck : IHealthCheck
    {
        private readonly IEventHubLoggingHealthClient eventHubLoggingHealthClient;
        private readonly ILogger<EventHubLoggingHealthCheck> logger;

        public EventHubLoggingHealthCheck(IEventHubLoggingHealthClient eventHubLoggingHealthClient, ILogger<EventHubLoggingHealthCheck> logger)
        {
            this.eventHubLoggingHealthClient = eventHubLoggingHealthClient;
            this.logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var healthCheckResult = await eventHubLoggingHealthClient.CheckHealthAsync(context);
            watch.Stop();
            if (healthCheckResult.Status == HealthStatus.Healthy)
            {
                logger.LogInformation(EventIds.EventHubLoggingIsHealthy.ToEventId(), $"Event hub is healthy, time spent to check this is {watch.ElapsedMilliseconds}ms");
            }
            else
            {
                logger.LogError(EventIds.EventHubLoggingIsUnhealthy.ToEventId(), healthCheckResult.Exception, $"Event hub is unhealthy responded with error {healthCheckResult.Exception.Message}, time spent to check this is {watch.ElapsedMilliseconds}ms");
            }
            return healthCheckResult;
        }
    }
}
