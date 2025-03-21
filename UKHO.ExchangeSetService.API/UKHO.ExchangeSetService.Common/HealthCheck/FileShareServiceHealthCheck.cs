﻿using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Helpers.Auth;
using UKHO.ExchangeSetService.Common.Helpers;

namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public class FileShareServiceHealthCheck(IFileShareServiceClient fileShareService,
                                       IAuthFssTokenProvider authFssTokenProvider,
                                       IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                       ILogger<FileShareService> logger) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var uri = $"/batch?limit={fileShareServiceConfig.Value.Limit}&start={fileShareServiceConfig.Value.Start}&$filter=BusinessUnit eq 'invalid'";
                var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
                string payloadJson = string.Empty;
                var fileShareServiceResponse = await fileShareService.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, uri, CancellationToken.None);
                if (fileShareServiceResponse.StatusCode == HttpStatusCode.OK)
                {
                    logger.LogDebug(EventIds.FileShareServiceIsHealthy.ToEventId(), "File share service is healthy responded with {StatusCode}", fileShareServiceResponse.StatusCode);
                    return HealthCheckResult.Healthy("File share service is healthy");
                }

                logger.LogError(EventIds.FileShareServiceIsUnhealthy.ToEventId(), "File share service is unhealthy responded with {StatusCode} for request uri {RequestUri}", fileShareServiceResponse.StatusCode, fileShareServiceResponse.RequestMessage.RequestUri);
                return HealthCheckResult.Unhealthy("File share service is unhealthy");
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.FileShareServiceIsUnhealthy.ToEventId(), ex, "Health check for the File Share Service threw an exception");
                return HealthCheckResult.Unhealthy("Health check for the File Share Service threw an exception", ex);
            }
        }
    }
}
