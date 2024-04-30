using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class MonitorHelper : IMonitorHelper
    {
        private readonly TelemetryClient telemetryClient;
        private const string requestStartedAt = "StartedAt";
        private const string requestCompletedAt = "CompletedAt";
        private const string runtimeDurationInMs = "DurationInMiliSecond";
        public MonitorHelper(TelemetryConfiguration telemetryConfiguration)
        {
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        public void MonitorRequest(string message, DateTime startedAt, DateTime completedAt, string correlationId, int? fileShareServiceSearchQueryCount = null, int? downloadedENCFileCount = null, long? fileSizeInBytes = null, string batchId = null, Dictionary<string, string> eventLogs = null)
        {
            Dictionary<string, string> eventDetails = new Dictionary<string, string>
            {
                { "CorrelationId", correlationId },
                { "BatchId", !string.IsNullOrWhiteSpace(batchId) ? batchId : string.Empty },
                { "FileSizeInBytes", $"{fileSizeInBytes}" },
                { "FileShareServiceSearchQueryCount", $"{fileShareServiceSearchQueryCount}" },
                { "DownloadedENCFileCount", $"{downloadedENCFileCount}" },
                { requestStartedAt, $"{startedAt:MM/dd/yyyy hh:mm:ss.fff tt}" },
                { requestCompletedAt, $"{completedAt:MM/dd/yyyy hh:mm:ss.fff tt}" },
                {
                    runtimeDurationInMs,
                    $"{completedAt.Subtract(startedAt.ToUniversalTime()).TotalMilliseconds}"
                }
            };

            if (eventLogs != null)
            {
                foreach (var eventLog in eventLogs)
                {
                    eventDetails.Add(eventLog.Key, eventLog.Value);
                }
            }

            this.telemetryClient.TrackEvent(message, eventDetails);
        }

    }
}
