using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    public class FileShareServiceHealthCheck : IHealthCheck
    {
        private readonly IFileShareServiceClient fileShareServiceClient;
        private readonly IAuthTokenProvider authTokenProvider;
        private readonly ILogger<FileShareService> logger;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;

        public FileShareServiceHealthCheck(IFileShareServiceClient fileShareService,
                                           IAuthTokenProvider authTokenProvider,
                                           IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                           ILogger<FileShareService> logger)
        {
            this.fileShareServiceClient = fileShareService;
            this.authTokenProvider = authTokenProvider;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var uri = $"/batch?limit={fileShareServiceConfig.Value.Limit}&start={fileShareServiceConfig.Value.Start}";
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            string payloadJson = string.Empty;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var fileShareServiceResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, uri);
            watch.Stop();
            if (fileShareServiceResponse.StatusCode == HttpStatusCode.OK)
            {
                logger.LogInformation(EventIds.FileShareServiceIsHealthy.ToEventId(), $"File share service is healthy responded with {fileShareServiceResponse.StatusCode}, time spent to check this is {watch.ElapsedMilliseconds}ms");
                return HealthCheckResult.Healthy("File share service is healthy");
            }
            else
            {
                logger.LogError(EventIds.FileShareServiceIsUnhealthy.ToEventId(), $"File share service is unhealthy responded with {fileShareServiceResponse.StatusCode} for request uri {fileShareServiceResponse.RequestMessage.RequestUri}, time spent to check this is {watch.ElapsedMilliseconds}ms");
                return HealthCheckResult.Unhealthy("File share service is unhealthy");
            }
        }
    }
}
