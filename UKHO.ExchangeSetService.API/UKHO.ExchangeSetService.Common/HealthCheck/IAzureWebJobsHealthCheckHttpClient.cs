using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public interface IAzureWebJobsHealthCheckHttpClient
    {
        public Task<HealthCheckResult> CheckHealth(WebJobDetails webJobDetails);
    }
}
