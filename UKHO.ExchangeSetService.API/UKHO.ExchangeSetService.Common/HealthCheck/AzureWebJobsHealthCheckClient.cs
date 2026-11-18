using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class AzureWebJobsHealthCheckClient : IAzureWebJobsHealthCheckClient
    {
        private const int MaxAttempts = 3;
        private readonly HttpClient httpClient;
        private readonly IWebHostEnvironment webHostEnvironment;

        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);

        public AzureWebJobsHealthCheckClient(HttpClient httpClient, IWebHostEnvironment webHostEnvironment)
        {
            this.httpClient = httpClient;
            this.webHostEnvironment = webHostEnvironment;
        }

        public async Task<HealthCheckResult> CheckAllWebJobsHealth(List<WebJobDetails> webJobs)
        {
            var failures = new List<string>();

            foreach (var webJob in webJobs)
            {
                var failure = await CheckWebJobHealthWithRetry(webJob);
                if (failure != null)
                {
                    failures.Add(failure);
                }
            }

            if (failures.Count > 0)
            {
                return HealthCheckResult.Unhealthy("Azure webjob is unhealthy", new Exception(string.Join("; ", failures)));
            }

            return HealthCheckResult.Healthy("Azure webjob is healthy");
        }

        private async Task<string> CheckWebJobHealthWithRetry(WebJobDetails webJob)
        {
            string failure = null;

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                failure = await CheckWebJobHealth(webJob);
                if (failure == null)
                {
                    return null;
                }

                if (attempt < MaxAttempts)
                {
                    await Task.Delay(RetryDelay * attempt);
                }
            }

            return $"{failure} after {MaxAttempts} attempts";
        }

        private async Task<string> CheckWebJobHealth(WebJobDetails webJob)
        {
            var webJobName = $"Webjob ess-{webHostEnvironment.EnvironmentName}-{webJob.ExchangeSetType}-{webJob.Instance}";

            try
            {
                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, webJob.WebJobUri);
                httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", webJob.UserPassword);
                using var response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return $"{webJobName} status code is {response.StatusCode} ({response.ReasonPhrase})";
                }

                var webJobDetails = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                string webJobStatus = webJobDetails?["status"];
                if (webJobStatus != "Running")
                {
                    return $"{webJobName} status is {webJobStatus}";
                }

                return null;
            }
            catch (Exception ex)
            {
                return $"{webJobName} health check failed with error {ex.Message}";
            }
        }
    }
}
