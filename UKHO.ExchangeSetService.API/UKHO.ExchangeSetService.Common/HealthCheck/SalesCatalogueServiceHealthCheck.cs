﻿using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers.Auth;
using UKHO.ExchangeSetService.Common.Helpers.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class SalesCatalogueServiceHealthCheck : IHealthCheck
    {
        private readonly ISalesCatalogueClient salesCatalogueClient;
        private readonly IAuthScsTokenProvider authScsTokenProvider;
        private readonly IOptions<SalesCatalogueConfiguration> salesCatalogueConfig;
        private readonly ILogger<SalesCatalogueService> logger;

        public SalesCatalogueServiceHealthCheck(ISalesCatalogueClient salesCatalogueClient,
                                              IAuthScsTokenProvider authScsTokenProvider,
                                              IOptions<SalesCatalogueConfiguration> salesCatalogueConfig,
                                              ILogger<SalesCatalogueService> logger)
        {
            this.salesCatalogueClient = salesCatalogueClient;
            this.authScsTokenProvider = authScsTokenProvider;
            this.salesCatalogueConfig = salesCatalogueConfig;
            this.logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                string sinceDateTime = DateTime.UtcNow.AddDays(-salesCatalogueConfig.Value.SinceDays).ToString("R");
                var accessToken = await authScsTokenProvider.GetManagedIdentityAuthAsync(salesCatalogueConfig.Value.ResourceId);
                var uri = $"/{salesCatalogueConfig.Value.Version}/productData/{salesCatalogueConfig.Value.ProductType}/products?sinceDateTime={sinceDateTime}";
                var salesCatalogueServiceResponse = await salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Get, null, accessToken, uri);
                if (salesCatalogueServiceResponse.StatusCode == HttpStatusCode.OK || salesCatalogueServiceResponse.StatusCode == HttpStatusCode.NotModified)
                {
                    logger.LogDebug(EventIds.SalesCatalogueServiceIsHealthy.ToEventId(), "Sales catalogue service is healthy responded with {StatusCode}", salesCatalogueServiceResponse.StatusCode);
                    return HealthCheckResult.Healthy("Sales catalogue service is healthy");
                }

                logger.LogError(EventIds.SalesCatalogueServiceIsUnhealthy.ToEventId(), "Sales catalogue service is unhealthy responded with {StatusCode} for request uri {RequestUri}", salesCatalogueServiceResponse.StatusCode, salesCatalogueServiceResponse.RequestMessage.RequestUri);
                return HealthCheckResult.Unhealthy("Sales catalogue service is unhealthy");
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.SalesCatalogueServiceIsUnhealthy.ToEventId(), ex, "Health check for the Sales Catalogue Service threw an exception");
                return HealthCheckResult.Unhealthy("Health check for the Sales Catalogue Service threw an exception", ex);
            }
        }
    }
}
