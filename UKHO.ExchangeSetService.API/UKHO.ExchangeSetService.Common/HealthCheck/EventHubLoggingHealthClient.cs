using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.Logging.EventHubLogProvider;

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
                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = LogLevel.Information.ToString(),
                    MessageTemplate = "Event Hub Logging Event Data For Health Check",
                    EventId = EventIds.EventHubLoggingEventDataForHealthCheck.ToEventId()
                };
                var jsonLogEntry = JsonConvert.SerializeObject(logEntry);

                await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(jsonLogEntry)));

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
