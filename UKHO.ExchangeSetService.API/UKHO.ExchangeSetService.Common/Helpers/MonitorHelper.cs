using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class MonitorHelper : IMonitorHelper
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IConfiguration configuration;
        private const string requestStartedAt = "StartedAt";
        private const string requestCompletedAt = "CompletedAt";
        private const string runtimeDurationInMs = "DurationInMiliSecond";
        public MonitorHelper(IConfiguration configuration, TelemetryConfiguration telemetryConfiguration)
        {
            this.configuration = configuration;
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        public void MonitorRequest(string message,DateTime startAt, DateTime completedAt, string correlationId, int? totalHitCountsToFileShareServiceForQuery = null, int? fileCount=null, long? fileSize= null,string batchId = null)
        {
            string instrumentationKey = configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
            this.telemetryClient.InstrumentationKey = instrumentationKey;
            this.telemetryClient.TrackEvent(message,
                               new Dictionary<string, string>
                               {
                                    {"CorrelationId:",correlationId },
                                    {"BatchId:", !string.IsNullOrWhiteSpace(batchId) ? batchId: string.Empty},
                                    {"FileSize:", $"{fileSize}" },
                                    {"FileShareServiceSearchQueryCount:", $"{totalHitCountsToFileShareServiceForQuery}" },
                                    {"DownloadedENCFileCount:", $"{fileCount}" },
                                    {requestStartedAt,$"{startAt:MM/dd/yyyy hh:mm:ss.fff tt}" },
                                    {requestCompletedAt,$"{completedAt:MM/dd/yyyy hh:mm:ss.fff tt}" },
                                    {runtimeDurationInMs,$"{completedAt.Subtract(startAt.ToUniversalTime()).TotalMilliseconds}" }
                               });
        }
    }
}
