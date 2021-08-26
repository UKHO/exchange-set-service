using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    [ExcludeFromCodeCoverage]
    public class EventHubLoggingHealthClient : IEventHubLoggingHealthClient
    {
        private readonly IOptions<EventHubLoggingConfiguration> eventHubLoggingConfiguration;

        public EventHubLoggingHealthClient(IOptions<EventHubLoggingConfiguration> eventHubLoggingConfiguration)
        {
            this.eventHubLoggingConfiguration = eventHubLoggingConfiguration;
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var connectionStringBuilder = new EventHubsConnectionStringBuilder(eventHubLoggingConfiguration.Value.ConnectionString)
            {
                EntityPath = eventHubLoggingConfiguration.Value.EntityPath
            };

            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

            try
            {
                await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(EventIds.EventHubLoggingEventDataForHealthCheck.ToEventId() + " of Event Hub")));
                return HealthCheckResult.Healthy("Event hub is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Event hub is unhealthy", new Exception(ex.Message));
            }
            finally
            {
                await eventHubClient.CloseAsync();
            }
        }
    }
}
