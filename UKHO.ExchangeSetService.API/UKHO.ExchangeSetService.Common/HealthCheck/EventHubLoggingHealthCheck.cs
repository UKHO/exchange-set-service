using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
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
            var healthCheckResult = await eventHubLoggingHealthClient.CheckHealthAsync(context, cancellationToken);
            if (healthCheckResult.Status == HealthStatus.Healthy)
            {
                logger.LogDebug(EventIds.EventHubLoggingIsHealthy.ToEventId(), "Event hub is healthy");
            }
            else
            {
                logger.LogError(EventIds.EventHubLoggingIsUnhealthy.ToEventId(), healthCheckResult.Exception, "Event hub is unhealthy responded with error {Message}", healthCheckResult.Exception.Message);
            }
            return healthCheckResult;
        }
    }
}
