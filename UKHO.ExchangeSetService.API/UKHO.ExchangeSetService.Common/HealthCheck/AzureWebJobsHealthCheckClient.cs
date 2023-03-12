using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class AzureWebJobsHealthCheckClient : IAzureWebJobsHealthCheckClient
    {
        private readonly IAzureWebJobsHealthCheckHttpClient azureWebJobsHealthCheckHttpClient;

        public AzureWebJobsHealthCheckClient(IAzureWebJobsHealthCheckHttpClient azureWebJobsHealthCheckHttpClient)
        {
            this.azureWebJobsHealthCheckHttpClient = azureWebJobsHealthCheckHttpClient; 
        }

        public async Task<HealthCheckResult> CheckAllWebJobsHealth(List<WebJobDetails> webJobs)
        {
            try
            {
                var jobsHealthCheckResults = new ConcurrentBag<(WebJobDetails webJobDetails, HealthCheckResult healthCheckResult)>();

                await Parallel.ForEachAsync(webJobs, async (job, token) =>
                {
                    jobsHealthCheckResults.Add(new (job, await azureWebJobsHealthCheckHttpClient.CheckHealth(job)));
                });
                
                return GetHealthStatus(jobsHealthCheckResults);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Azure webjob is unhealthy", new Exception(ex.Message));
            }
        }

        private HealthCheckResult GetHealthStatus(ConcurrentBag<(WebJobDetails webJobDetails, HealthCheckResult healthCheckResult)> healthCheckResults)
        {
            if (healthCheckResults.All(h => h.healthCheckResult.Status == HealthStatus.Healthy))
                return healthCheckResults.First().healthCheckResult;

            var unhealthyResults = healthCheckResults
                .Where(h => h.healthCheckResult.Status != HealthStatus.Healthy)
                .OrderBy(a => a.webJobDetails.ExchangeSetType)
                .ThenBy(a => a.webJobDetails.Instance)
                .ToList();

            var description = string.Join(", ", unhealthyResults
                .Select(h => h.healthCheckResult.Description));

            var message = string.Join(", ", unhealthyResults
                .Select(h => h.healthCheckResult.Exception?.Message));

            var allUnhealthyInstancesOfSameType = healthCheckResults
                .GroupBy(w => w.Item1.ExchangeSetType)
                .Where(a => a.All(x => x.Item2.Status == HealthStatus.Unhealthy));

            return allUnhealthyInstancesOfSameType.Any()
                 ? HealthCheckResult.Unhealthy(description, new Exception(message))
                  : HealthCheckResult.Degraded(description, new Exception(message));
        }
    }
}