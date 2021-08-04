using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
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
        private readonly IOptions<EssManagedIdentityConfiguration> essManagedIdentityConfiguration;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IOptions<EssWebJobsConfiguration> essWebJobsConfiguration;

        public AzureWebJobsHealthCheckClient(IOptions<EssManagedIdentityConfiguration> essManagedIdentityConfiguration,
                                             IWebHostEnvironment webHostEnvironment,
                                             IOptions<EssWebJobsConfiguration> essWebJobsConfiguration)
        {
            this.essManagedIdentityConfiguration = essManagedIdentityConfiguration;
            this.webHostEnvironment = webHostEnvironment;
            this.essWebJobsConfiguration = essWebJobsConfiguration;
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                string webJobStatus = string.Empty;
                string webJobUri, userNameKey, passwordKey, webJobDetail = string.Empty;
                int instanceCount = 2;
                List<string> exchangeSetTypes = new List<string>() { "sxs", "mxs", "lxs" };

                var builder = new ConfigurationBuilder()
                   .SetBasePath(webHostEnvironment.ContentRootPath)
                   .AddJsonFile("appsettings.json", false, true)
                   .AddJsonFile($"appsettings.{webHostEnvironment.EnvironmentName}.json", true, true);

                builder.AddEnvironmentVariables();

                var tempConfig = builder.Build();
                string kvServiceUri = tempConfig["KeyVaultSettings:ServiceUri"];
                SecretClient client = null;
                if (!string.IsNullOrWhiteSpace(kvServiceUri))
                {
                    client = new SecretClient(vaultUri: new Uri(kvServiceUri), credential: new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = essManagedIdentityConfiguration.Value.ClientId }));

                    foreach (string exchangeSetType in exchangeSetTypes)
                    {
                        for (int i = 1; i <= instanceCount; i++)
                        {
                            userNameKey = $"ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{i}-webapp-scm-username";
                            passwordKey = $"ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{i}-webapp-scm-username";
                            webJobUri = $"https://{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{i}.scm.azurewebsites.net/api/continuouswebjobs/ESSFulfilmentWebJob";
                            KeyVaultSecret userNameSecret = await client.GetSecretAsync(userNameKey);
                            KeyVaultSecret passwordSecret = await client.GetSecretAsync(passwordKey);
                            string userPswd = userNameSecret.Value + ":" + passwordSecret.Value;
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
