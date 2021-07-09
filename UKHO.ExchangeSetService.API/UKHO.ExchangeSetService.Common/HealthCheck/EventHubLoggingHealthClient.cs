using Azure.Messaging.EventHubs.Consumer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;

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
            try
            {
                bool eventHubReadSuccessful = false;
                EventHubConsumerClient eventHubConsumerClient = new EventHubConsumerClient(eventHubLoggingConfiguration.Value.ConsumerGroup, eventHubLoggingConfiguration.Value.ConnectionString);
                await foreach (PartitionEvent partitionEvent in eventHubConsumerClient.ReadEventsAsync())
                {
                    if (Encoding.UTF8.GetString(partitionEvent.Data.EventBody) != null)
                    {
                        eventHubReadSuccessful = true;
                        break;
                    }
                }

                if (eventHubReadSuccessful)
                {
                    return HealthCheckResult.Healthy("Event hub is healthy");
                }
                else
                {
                    return HealthCheckResult.Unhealthy("Event hub is unhealthy");
                }
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Event hub is unhealthy", ex);
            }
        }
    }
}
