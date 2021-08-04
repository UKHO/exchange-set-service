using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class SalesCatalogueServiceHealthCheck : IHealthCheck
    {
        private readonly ISalesCatalogueClient salesCatalogueClient;
        private readonly IAuthTokenProvider authTokenProvider;
        private readonly IOptions<SalesCatalogueConfiguration> salesCatalogueConfig;
        private readonly ILogger<SalesCatalogueService> logger;

        public SalesCatalogueServiceHealthCheck(ISalesCatalogueClient salesCatalogueClient,
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
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var salesCatalogueServiceResponse = await salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Get, null, accessToken, uri);
            watch.Stop();
            if (salesCatalogueServiceResponse.StatusCode == HttpStatusCode.OK || salesCatalogueServiceResponse.StatusCode == HttpStatusCode.NotModified)
            {
                logger.LogInformation(EventIds.SalesCatalogueServiceIsHealthy.ToEventId(), $"Sales catalogue service is healthy responded with {salesCatalogueServiceResponse.StatusCode}, time spent to check this is {watch.ElapsedMilliseconds}ms");
                return HealthCheckResult.Healthy("Sales catalogue service is healthy");
            }
            else
            {
                logger.LogError(EventIds.SalesCatalogueServiceIsUnhealthy.ToEventId(), $"Sales catalogue service is unhealthy responded with {salesCatalogueServiceResponse.StatusCode} for request uri {salesCatalogueServiceResponse.RequestMessage.RequestUri}, time spent to check this is {watch.ElapsedMilliseconds}ms");
                return HealthCheckResult.Unhealthy("Sales catalogue service is unhealthy");
            }
        }
    }
}
