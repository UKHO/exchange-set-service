using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
namespace UKHO.ExchangeSetService.FulfilmentService.Configuration
{
    public class AzureDependencyFilterTelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _inner;

        public AzureDependencyFilterTelemetryProcessor(ITelemetryProcessor inner)
        {
            _inner = inner;
        }

        public void Process(ITelemetry item)
        {
            if (item is Microsoft.ApplicationInsights.DataContracts.DependencyTelemetry dependency
                && dependency.Success == true
                && dependency.Name == "GET addsfssqastorage"
                && dependency.Type == "Azure blob")
            {
                ////dependency.Data = "testing";
                return;
            }
            _inner.Process(item);
        }
    }
}
