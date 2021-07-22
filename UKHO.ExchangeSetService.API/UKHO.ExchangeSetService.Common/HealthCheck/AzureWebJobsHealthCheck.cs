using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class AzureWebJobsHealthCheck : IHealthCheck
    {
        private readonly IOptions<EssManagedIdentityConfiguration> essManagedIdentityConfiguration;
        private readonly IOptions<EssWebJobsConfiguration> essWebJobsConfiguration;
        private readonly ILogger<AzureBlobStorageService> logger;

        public AzureWebJobsHealthCheck(IOptions<EssManagedIdentityConfiguration> essManagedIdentityConfiguration,
                                       IOptions<EssWebJobsConfiguration> essWebJobsConfiguration,
                                       ILogger<AzureBlobStorageService> logger)
        {
            this.essManagedIdentityConfiguration = essManagedIdentityConfiguration;
            this.essWebJobsConfiguration = essWebJobsConfiguration;
            this.logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var credentials = SdkContext.AzureCredentialsFactory.FromUserAssigedManagedServiceIdentity(essManagedIdentityConfiguration.Value.ClientId, MSIResourceType.AppService, AzureEnvironment.AzureGlobalCloud);

                var client = RestClient
                    .Configure()
                    .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                    .WithCredentials(credentials)
                    .Build();

                string uri = essWebJobsConfiguration.Value.MxsWebJobApiUri;

                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                await client.Credentials.ProcessHttpRequestAsync(request, cancellationToken);
                var httpClient = new HttpClient();
                var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var webJobDetails = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                    string webJobStatus = webJobDetails["status"];
                    if (webJobStatus == "Running")
                    {
                        logger.LogDebug("Azure webjob is healthy");
                        return HealthCheckResult.Healthy("Azure webjob is healthy");
                    }
                    else
                    {
                        logger.LogError("Azure webjob is unhealthy with status {webJobStatus}", webJobStatus);
                        return HealthCheckResult.Unhealthy("Azure webjob is unhealthy");
                    }
                }
                else
                {
                    logger.LogError("Azure webjob is unhealthy with status {StatusCode}", response.StatusCode);
                    return HealthCheckResult.Unhealthy("Azure webjob is unhealthy");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Azure blob storage is unhealthy with error " + ex.Message);
                return HealthCheckResult.Unhealthy("Azure blob storage is unhealthy", ex);
            }
        }
    }
}