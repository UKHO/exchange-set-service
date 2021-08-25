using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.Enums;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class AzureWebJobsHealthCheckService : IAzureWebJobsHealthCheckService
    {
        private readonly IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration;
        private readonly IWebJobsAccessKeyProvider webJobsAccessKeyProvider;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IAzureWebJobsHealthCheckClient azureWebJobsHealthCheckClient;
        private readonly IAzureBlobStorageService azureBlobStorageService;

        public AzureWebJobsHealthCheckService(IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageConfiguration,
                                       IWebJobsAccessKeyProvider webJobsAccessKeyProvider,
                                       IWebHostEnvironment webHostEnvironment,
                                       IAzureBlobStorageService azureBlobStorageService,
                                       IAzureWebJobsHealthCheckClient azureWebJobsHealthCheckClient)
        {
            this.essFulfilmentStorageConfiguration = essFulfilmentStorageConfiguration;
            this.webJobsAccessKeyProvider = webJobsAccessKeyProvider;
            this.webHostEnvironment = webHostEnvironment;
            this.azureBlobStorageService = azureBlobStorageService;
            this.azureWebJobsHealthCheckClient = azureWebJobsHealthCheckClient;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                string webJobUri, userNameKey, passwordKey = string.Empty;
                string[] exchangeSetTypes = essFulfilmentStorageConfiguration.Value.ExchangeSetTypes.Split(",");
                List<WebJobDetails> webJobs = new List<WebJobDetails>();
                WebJobDetails webJobDetails = new WebJobDetails();
                foreach (string exchangeSetTypeName in exchangeSetTypes)
                {
                    Enum.TryParse(exchangeSetTypeName, out ExchangeSetType exchangeSetType);
                    for (int instance = 1; instance <= azureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(exchangeSetType); instance++)
                    {
                        userNameKey = $"ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{instance}-webapp-scm-username";
                        passwordKey = $"ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{instance}-webapp-scm-password";
                        webJobUri = $"https://ess-{webHostEnvironment.EnvironmentName}-{exchangeSetType}-{instance}-webapp.scm.azurewebsites.net/api/continuouswebjobs/ESSFulfilmentWebJob";

                        string userPassword = webJobsAccessKeyProvider.GetWebJobsAccessKey(userNameKey) + ":" + webJobsAccessKeyProvider.GetWebJobsAccessKey(passwordKey);
                        userPassword = Convert.ToBase64String(Encoding.Default.GetBytes(userPassword));
                        webJobDetails.UserPassword = userPassword;
                        webJobDetails.WebJobUri = webJobUri;
                        webJobDetails.ExchangeSetType = exchangeSetTypeName;
                        webJobDetails.Instance = instance;
                        webJobs.Add(webJobDetails);
                    }
                }
                var webJobsHealth = azureWebJobsHealthCheckClient.CheckAllWebJobsHealth(webJobs);
                await Task.WhenAll(webJobsHealth);

                return webJobsHealth.Result;
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Azure webjob is unhealthy", new Exception(ex.Message));
            }
        }
    }
}
