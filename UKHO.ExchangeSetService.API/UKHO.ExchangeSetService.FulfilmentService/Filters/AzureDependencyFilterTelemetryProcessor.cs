using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
namespace UKHO.ExchangeSetService.FulfilmentService.Filters
{
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
                && dependency.Name == "GET addsfssqastorage"
                && dependency.Type == "Azure blob")
            {
                ////dependency.Data = "testing";
                return;
            }
            inner.Process(item);
        }
    }
}
