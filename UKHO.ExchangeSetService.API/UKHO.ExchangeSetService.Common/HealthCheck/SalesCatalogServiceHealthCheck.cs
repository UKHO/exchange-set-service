using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class SalesCatalogServiceHealthCheck : IHealthCheck
    {
        private readonly ISalesCatalogueClient salesCatalogueClient;
        private readonly IAuthTokenProvider authTokenProvider;
        private readonly IOptions<SalesCatalogueConfiguration> salesCatalogueConfig;
        private readonly ILogger<SalesCatalogueService> logger;

        public SalesCatalogServiceHealthCheck(ISalesCatalogueClient salesCatalogueClient,
                                              IAuthTokenProvider authTokenProvider,
                                              IOptions<SalesCatalogueConfiguration> salesCatalogueConfig,
                                              ILogger<SalesCatalogueService> logger)
        {
            this.salesCatalogueClient = salesCatalogueClient;
            this.authTokenProvider = authTokenProvider;
            this.salesCatalogueConfig = salesCatalogueConfig;
            this.logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            string sinceDateTime = DateTime.UtcNow.AddDays(-salesCatalogueConfig.Value.SinceDays).ToString("R");
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(salesCatalogueConfig.Value.ResourceId);
            var uri = $"/{salesCatalogueConfig.Value.Version}/productData/{salesCatalogueConfig.Value.ProductType}/products?sinceDateTime={sinceDateTime}";

            var salesCatalogueServiceResponse = await salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Get, null, accessToken, uri);

            if (salesCatalogueServiceResponse.StatusCode == HttpStatusCode.OK || salesCatalogueServiceResponse.StatusCode == HttpStatusCode.NotModified)
            {
                logger.LogInformation(EventIds.SalesCatalogueServiceIsHealthy.ToEventId(), $"Sales catalogue service is healthy responded with {(int)salesCatalogueServiceResponse.StatusCode} {salesCatalogueServiceResponse.StatusCode}");
                return HealthCheckResult.Healthy("Sales catalogue service is healthy");
            }
            else
            {
                logger.LogError(EventIds.SalesCatalogueServiceIsUnhealthy.ToEventId(), $"Sales catalogue service is unhealthy responded with {(int)salesCatalogueServiceResponse.StatusCode} {salesCatalogueServiceResponse.StatusCode} for request uri {salesCatalogueServiceResponse.RequestMessage.RequestUri}");
                return HealthCheckResult.Unhealthy("Sales catalogue service is unhealthy");
            }
        }
    }
}
