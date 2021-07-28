using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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
            try
            {
                string webJobStatus = string.Empty;
                string userNameKey, passwordKey, webJobUri, webJobDetail = string.Empty;
                int instanceCount = 2;
                List<string> exchangeSetTypes = new List<string>() { "sxs", "mxs", "lxs" };
                foreach (string exchangeSetType in exchangeSetTypes)
                {
                    for (int i = 1; i < instanceCount; i++)
                    {
                        userNameKey = string.Format(essWebJobsConfiguration.Value.EssWebAppName + "-scm-username", webHostEnvironment.EnvironmentName, exchangeSetType, i);
                        passwordKey = string.Format(essWebJobsConfiguration.Value.EssWebAppName + "-scm-password", webHostEnvironment.EnvironmentName, exchangeSetType, i);
                        webJobUri = string.Format("https://" + essWebJobsConfiguration.Value.EssWebAppName + ".scm.azurewebsites.net/api/continuouswebjobs/ESSFulfilmentWebJob", webHostEnvironment.EnvironmentName, exchangeSetType, i);
                        logger.LogInformation("Azure webjob username key is " + userNameKey);
                        logger.LogInformation("Azure webjob password key is " + passwordKey);
                        logger.LogInformation("Azure webjob uri is " + webJobUri);
                        string userPswd = await FetchKeyVaultSecret(userNameKey) + ":" + await FetchKeyVaultSecret(passwordKey);
                        userPswd = Convert.ToBase64String(Encoding.Default.GetBytes(userPswd));
                        var httpClient = new HttpClient();
                        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, webJobUri);
                        httpClient.DefaultRequestHeaders.Accept.Clear();
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", userPswd);
                        var response = httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).Result;
                        logger.LogInformation("Azure webjob response is " + response.StatusCode);
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            logger.LogInformation("Azure webjob response is OK");
                            var webJobDetails = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                            webJobStatus = webJobDetails["status"];
                            logger.LogInformation("Azure webjob status is " + webJobStatus + " of " + exchangeSetType + "-" + i.ToString());
                            if (webJobStatus != "Running")
                            {
                                webJobDetail = "Webjob " + string.Format(essWebJobsConfiguration.Value.EssWebAppName, webHostEnvironment.EnvironmentName, exchangeSetType, i) + " with status " + webJobStatus;
                                logger.LogInformation("Azure webjob detail is " + webJobDetail);
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
            catch (Exception ex)
            {
                logger.LogError("Azure webjob is unhealthy with error {message}", ex.Message);
                return HealthCheckResult.Unhealthy("Azure webjob is unhealthy");
            }
        }

        private async Task<string> FetchKeyVaultSecret(string secretName)
        {
            try
            {
                logger.LogInformation("Secretname is " + secretName);
                var builder = new ConfigurationBuilder()
                    .SetBasePath(webHostEnvironment.ContentRootPath)
                    .AddJsonFile("appsettings.json", false, true)
                    .AddJsonFile($"appsettings.{webHostEnvironment.EnvironmentName}.json", true, true);

                builder.AddEnvironmentVariables();

                var tempConfig = builder.Build();
                string kvServiceUri = tempConfig["KeyVaultSettings:ServiceUri"];

                string value = string.Empty;
                logger.LogInformation("Keyvault uri is " + kvServiceUri);
                if (!string.IsNullOrWhiteSpace(kvServiceUri))
                {
                    logger.LogInformation("Key vault uri is not empty");
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