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
    [ExcludeFromCodeCoverage]
    public class AzureWebJobsHealthCheckClient : IAzureWebJobsHealthCheckClient
    {
        static HttpClient httpClient = new HttpClient();
        private readonly IWebHostEnvironment webHostEnvironment;
        
        public AzureWebJobsHealthCheckClient(IWebHostEnvironment webHostEnvironment)
        {
            this.webHostEnvironment = webHostEnvironment;
        }
        public async Task<HealthCheckResult> CheckAllWebJobsHealth(List<WebJobDetails> webJobs)
        {
            try
            {
                string webJobDetail, webJobStatus = string.Empty;
                foreach (var webJob in webJobs)
                {
                    using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, webJob.WebJobUri);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", webJob.UserPassword);
                    var response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var webJobDetails = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                        webJobStatus = webJobDetails["status"];
                        if (webJobStatus != "Running")
                        {
                            webJobDetail = $"Webjob ess-{webHostEnvironment.EnvironmentName}-{webJob.ExchangeSetType}-{webJob.Instance} status is {webJobStatus}";
                            return HealthCheckResult.Unhealthy("Azure webjob is unhealthy", new Exception(webJobDetail));
                        }
                    }
                    else
                    {
                        return HealthCheckResult.Unhealthy("Azure webjob is unhealthy", new Exception($"Webjob ess-{webHostEnvironment.EnvironmentName}-{webJob.ExchangeSetType}-{webJob.Instance} status code is {response.StatusCode}"));
                    }
                }
                return HealthCheckResult.Healthy("Azure webjob is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Azure webjob is unhealthy", new Exception(ex.Message));
            }
        }
    }
}