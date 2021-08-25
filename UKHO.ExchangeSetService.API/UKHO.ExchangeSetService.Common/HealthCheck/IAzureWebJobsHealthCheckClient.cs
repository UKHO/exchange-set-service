using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public interface IAzureWebJobsHealthCheckClient
    {
        public Task<HealthCheckResult> CheckAllWebJobsHealth(List<WebJobDetails> webJobs);
    }
}
