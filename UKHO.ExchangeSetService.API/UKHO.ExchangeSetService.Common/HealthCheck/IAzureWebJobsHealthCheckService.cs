using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public interface IAzureWebJobsHealthCheckService
    {
        public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    }
}