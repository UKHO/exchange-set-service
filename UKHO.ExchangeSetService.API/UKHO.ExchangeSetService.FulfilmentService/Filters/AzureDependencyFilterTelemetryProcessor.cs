using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.FulfilmentService.Filters
{
    [ExcludeFromCodeCoverage]
    public class AzureDependencyFilterTelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor inner;
        public AzureDependencyFilterTelemetryProcessor(ITelemetryProcessor inner)
        {
            this.inner = inner;
        }

        public void Process(ITelemetry item)
        {
            if (item is DependencyTelemetry dependency
                && dependency.Type == "Azure blob" && dependency.Data.Contains("skoid"))
            {
                dependency.Data = new Uri(dependency.Data).GetLeftPart(UriPartial.Path);
                inner.Process(item);
                return;
            }
            inner.Process(item);
        }
    }
}
