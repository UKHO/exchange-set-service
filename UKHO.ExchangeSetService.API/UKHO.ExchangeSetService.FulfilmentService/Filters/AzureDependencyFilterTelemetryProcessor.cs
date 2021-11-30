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
        private readonly string dependancyName;
        private readonly string dependancyType;

        public AzureDependencyFilterTelemetryProcessor(ITelemetryProcessor inner, string dependancyName, string dependancyType)
        {
            this.inner = inner;
            this.dependancyName = dependancyName;
            this.dependancyType = dependancyType;
        }

        public void Process(ITelemetry item)
        {
            if (item is DependencyTelemetry dependency
                && dependency.Name == dependancyName
                && dependency.Type == dependancyType)
            {
                dependency.Data = new Uri(dependency.Data.ToString()).GetLeftPart(UriPartial.Path);
                inner.Process(item);
                return;
            }
            inner.Process(item);
        }
    }
}
