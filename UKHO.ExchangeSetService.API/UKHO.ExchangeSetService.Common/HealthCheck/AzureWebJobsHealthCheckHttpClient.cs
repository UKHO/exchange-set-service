using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    [ExcludeFromCodeCoverage]
    public class AzureWebJobsHealthCheckHttpClient : IAzureWebJobsHealthCheckHttpClient
    {
        static HttpClient httpClient = new HttpClient();
        private readonly IWebHostEnvironment webHostEnvironment;

        public AzureWebJobsHealthCheckHttpClient(IWebHostEnvironment webHostEnvironment)
        {
            this.webHostEnvironment = webHostEnvironment;
        }

        public async Task<HealthCheckResult> CheckHealth(WebJobDetails webJobDetails)
        {
            try
            {
                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, webJobDetails.WebJobUri);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", webJobDetails.UserPassword);
                var response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var webJobHealthResponse = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                    var webJobStatus = webJobHealthResponse?["status"];
                    if (webJobStatus != "Running")
                    {
                        var webJobDetail = $"Webjob ess-{webHostEnvironment.EnvironmentName}-{webJobDetails.ExchangeSetType}-{webJobDetails.Instance} status is {webJobStatus}";
                        return HealthCheckResult.Unhealthy("Azure webjob is unhealthy", new Exception(webJobDetail));
                    }
                }
                else
                {
                    return HealthCheckResult.Unhealthy("Azure webjob is unhealthy", new Exception($"Webjob ess-{webHostEnvironment.EnvironmentName}-{webJobDetails.ExchangeSetType}-{webJobDetails.Instance} status code is {response.StatusCode}"));
                }

                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Azure webjob is unhealthy", new Exception(ex.Message));
            }
        }
    }
}
