using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    [ExcludeFromCodeCoverage]
    public class AzureWebJobsHealthCheckClient : IAzureWebJobsHealthCheck
    {
        private readonly IWebJobsAccessKeyProvider webJobsAccessKeyProvider;
        private readonly IWebHostEnvironment webHostEnvironment;

        public AzureWebJobsHealthCheckClient(IWebJobsAccessKeyProvider webJobsAccessKeyProvider,
                                             IWebHostEnvironment webHostEnvironment)
        {
            this.webJobsAccessKeyProvider = webJobsAccessKeyProvider;
            this.webHostEnvironment = webHostEnvironment;
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                string webJobStatus = string.Empty;
                string webJobUri, userNameKey, passwordKey, webJobDetail = string.Empty;
                int instanceCount = 2;
                List<string> exchangeSetTypes = new List<string>() { "sxs", "mxs", "lxs" };

                foreach (string exchangeSetType in exchangeSetTypes)
                {
                    for (int i = 1; i <= instanceCount; i++)
                    {
                        userNameKey = $"ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{i}-webapp-scm-username";
                        passwordKey = $"ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{i}-webapp-scm-password";
                        webJobUri = $"https://ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{i}-webapp.scm.azurewebsites.net/api/continuouswebjobs/ESSFulfilmentWebJob";

                        string userPswd = webJobsAccessKeyProvider.GetWebJobsAccessKey(userNameKey) + ":" + webJobsAccessKeyProvider.GetWebJobsAccessKey(passwordKey);
                        userPswd = Convert.ToBase64String(Encoding.Default.GetBytes(userPswd));
                        var httpClient = new HttpClient();
                        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, webJobUri);
                        httpClient.DefaultRequestHeaders.Accept.Clear();
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", userPswd);
                        var response = httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).Result;

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var webJobDetails = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                            webJobStatus = webJobDetails["status"];
                            if (webJobStatus != "Running")
                            {
                                webJobDetail = $"Webjob ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{i} status is {webJobStatus}";
                                break;
                            }
                        }
                    }
                    if (webJobStatus != "Running")
                        break;
                }
                ////}
                if (webJobStatus == "Running")
                {
                    return HealthCheckResult.Healthy("Azure webjob is healthy");
                }
                else
                {
                    return HealthCheckResult.Unhealthy("Azure webjob is unhealthy", new Exception(webJobDetail));
                }
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Azure webjob is unhealthy with error {ex.Message}", new Exception(ex.Message));
            }
        }
    }
}
