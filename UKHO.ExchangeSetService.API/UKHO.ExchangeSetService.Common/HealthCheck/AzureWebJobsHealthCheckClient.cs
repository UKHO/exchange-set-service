using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    [ExcludeFromCodeCoverage]
    public class AzureWebJobsHealthCheckClient : IAzureWebJobsHealthCheck
    {
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IOptions<EssWebJobsConfiguration> essWebJobsConfiguration;

        public AzureWebJobsHealthCheckClient(IConfiguration configuration,
                                             IWebHostEnvironment webHostEnvironment,
                                             IOptions<EssWebJobsConfiguration> essWebJobsConfiguration)
        {
            this.configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
            this.essWebJobsConfiguration = essWebJobsConfiguration;
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                string webJobStatus = string.Empty;
                string userNameKey, passwordKey, webJobUri, webJobDetail = string.Empty;
                int instanceCount = 2;
                List<string> exchangeSetTypes = new List<string>() { "sxs", "mxs", "lxs" };
                foreach (string exchangeSetType in exchangeSetTypes)
                {
                    for (int i = 1; i <= instanceCount; i++)
                    {
                        userNameKey = $"{essWebJobsConfiguration.Value.EssWebAppName} + -scm-username, {webHostEnvironment.EnvironmentName}, {exchangeSetType}, {i}";
                        passwordKey = $"{essWebJobsConfiguration.Value.EssWebAppName} + -scm-password, {webHostEnvironment.EnvironmentName}, {exchangeSetType}, {i}";
                        webJobUri = $"https:// + {essWebJobsConfiguration.Value.EssWebAppName}.scm.azurewebsites.net/api/continuouswebjobs/ESSFulfilmentWebJob,{webHostEnvironment.EnvironmentName},{exchangeSetType},{i}";

                        string userPswd = configuration[userNameKey] + ":" + configuration[passwordKey];
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
                                webJobDetail = "Webjob " + string.Format(essWebJobsConfiguration.Value.EssWebAppName, webHostEnvironment.EnvironmentName, exchangeSetType, i) + " with status " + webJobStatus;
                                break;
                            }
                        }
                    }
                    if (webJobStatus != "Running")
                        break;
                }
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
