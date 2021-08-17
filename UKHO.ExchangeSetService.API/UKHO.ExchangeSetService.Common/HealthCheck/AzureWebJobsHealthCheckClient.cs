using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.Enums;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    [ExcludeFromCodeCoverage]
    public class AzureWebJobsHealthCheckClient : IAzureWebJobsHealthCheckClient
    {
        static HttpClient httpClient = new HttpClient();
        private readonly IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration;
        private readonly IWebJobsAccessKeyProvider webJobsAccessKeyProvider;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IAzureBlobStorageService azureBlobStorageService;

        public AzureWebJobsHealthCheckClient(IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration,
                                             IWebJobsAccessKeyProvider webJobsAccessKeyProvider,
                                             IWebHostEnvironment webHostEnvironment,
                                             IAzureBlobStorageService azureBlobStorageService)
        {
            this.essFulfilmentStorageConfiguration = essFulfilmentStorageConfiguration;
            this.webJobsAccessKeyProvider = webJobsAccessKeyProvider;
            this.webHostEnvironment = webHostEnvironment;
            this.azureBlobStorageService = azureBlobStorageService;
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                string webJobUri, userNameKey, passwordKey = string.Empty;
                string[] exchangeSetTypes = essFulfilmentStorageConfiguration.Value.ExchangeSetTypes.Split(",");
                List<Tuple<string, string, string, int>> webJobs = new List<Tuple<string, string, string, int>>();
                foreach (string exchangeSetTypeName in exchangeSetTypes)
                {
                    Enum.TryParse(exchangeSetTypeName, out ExchangeSetType exchangeSetType);
                    for (int i = 1; i <= azureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(exchangeSetType); i++)
                    {
                        userNameKey = $"ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{i}-webapp-scm-username";
                        passwordKey = $"ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{i}-webapp-scm-password";
                        webJobUri = $"https://ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{i}-webapp.scm.azurewebsites.net/api/continuouswebjobs/ESSFulfilmentWebJob";

                        string userPswd = webJobsAccessKeyProvider.GetWebJobsAccessKey(userNameKey) + ":" + webJobsAccessKeyProvider.GetWebJobsAccessKey(passwordKey);
                        userPswd = Convert.ToBase64String(Encoding.Default.GetBytes(userPswd));
                        webJobs.Add(Tuple.Create(userPswd, webJobUri, exchangeSetTypeName, i));
                    }
                }
                var webJobsHealth = CheckAllWebJobsHealth(webJobs);
                await Task.WhenAll(webJobsHealth);

                return webJobsHealth.Result;
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Azure webjob is unhealthy", new Exception(ex.Message));
            }
        }

        private async Task<HealthCheckResult> CheckAllWebJobsHealth(List<Tuple<string, string, string, int>> webJobs)
        {
            string webJobDetail, webJobStatus = string.Empty;
            foreach (var webJob in webJobs)
            {
                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, webJob.Item2);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", webJob.Item1);
                var response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var webJobDetails = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                    webJobStatus = webJobDetails["status"];
                    if (webJobStatus != "Running")
                    {
                        webJobDetail = $"Webjob ess-{webHostEnvironment.EnvironmentName}-{webJob.Item3}-{webJob.Item4} status is {webJobStatus}";
                        return HealthCheckResult.Unhealthy("Azure webjob is unhealthy", new Exception(webJobDetail));
                    }
                }
                else
                {
                    return HealthCheckResult.Unhealthy("Azure webjob is unhealthy", new Exception($"Webjob ess-{webHostEnvironment.EnvironmentName}-{webJob.Item3}-{webJob.Item4} status code is {response.StatusCode}"));
                }
            }
            return HealthCheckResult.Healthy("Azure webjob is healthy");
        }
    }
}