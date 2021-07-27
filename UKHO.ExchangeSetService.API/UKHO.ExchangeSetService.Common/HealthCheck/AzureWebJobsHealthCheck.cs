using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class AzureWebJobsHealthCheck : IHealthCheck
    {
        private readonly IOptions<EssManagedIdentityConfiguration> essManagedIdentityConfiguration;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IOptions<EssWebJobsConfiguration> essWebJobsConfiguration;
        private readonly ILogger<AzureBlobStorageService> logger;

        public AzureWebJobsHealthCheck(IOptions<EssManagedIdentityConfiguration> essManagedIdentityConfiguration,
                                       IWebHostEnvironment webHostEnvironment,
                                       IOptions<EssWebJobsConfiguration> essWebJobsConfiguration,
                                       ILogger<AzureBlobStorageService> logger)
        {
            this.essManagedIdentityConfiguration = essManagedIdentityConfiguration;
            this.webHostEnvironment = webHostEnvironment;
            this.essWebJobsConfiguration = essWebJobsConfiguration;
            this.logger = logger;
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            string webJobStatus = string.Empty;
            string userName, password, kvServiceUri, webJobUri, webJobDetail = string.Empty;
            int instanceCount = 2;
            List<string> exchangeSetTypes = new List<string>() { "sxs", "mxs", "lxs" };
            foreach (string exchangeSetType in exchangeSetTypes)
            {
                for (int i = 1; i < instanceCount; i++)
                {
                    userName = string.Format(essWebJobsConfiguration.Value.EssWebAppName + "-scm-username", webHostEnvironment.EnvironmentName, exchangeSetType, i);
                    password = string.Format(essWebJobsConfiguration.Value.EssWebAppName + "-scm-password", webHostEnvironment.EnvironmentName, exchangeSetType, i);
                    webJobUri = string.Format("https://" + essWebJobsConfiguration.Value.EssWebAppName + ".scm.azurewebsites.net/api/continuouswebjobs/ESSFulfilmentWebJob", webHostEnvironment.EnvironmentName, exchangeSetType, i);
                    kvServiceUri = string.Format("https://ess-{0}-{1}-kv.vault.azure.net", webHostEnvironment.EnvironmentName, exchangeSetType);

                    string userPswd = await FetchKeyVaultSecret(userName, kvServiceUri) + ":" + await FetchKeyVaultSecret(password, kvServiceUri);
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
                {
                    break;
                }
            }
            if (webJobStatus == "Running")
            {
                logger.LogInformation("Azure webjob is healthy");
                return HealthCheckResult.Healthy("Azure webjob is healthy");
            }
            else
            {
                logger.LogError("Azure webjob is unhealthy for {webJobDetail}", webJobDetail);
                return HealthCheckResult.Unhealthy("Azure webjob is unhealthy");
            }
        }

        private async Task<string> FetchKeyVaultSecret(string secretName, string kvServiceUri)
        {
            try
            {
                string value = string.Empty;
                if (!string.IsNullOrWhiteSpace(kvServiceUri))
                {
                    var client = new SecretClient(vaultUri: new Uri(kvServiceUri), credential: new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = essManagedIdentityConfiguration.Value.ClientId }));
                    KeyVaultSecret secret = await client.GetSecretAsync(secretName);
                    value = secret.Value;
                }
                return value;
            }
            catch (Exception ex)
            {
                logger.LogError("Error in reading webjob key vault secrets with error {message}", ex.Message);
                return string.Empty;
            }
        }
    }
}