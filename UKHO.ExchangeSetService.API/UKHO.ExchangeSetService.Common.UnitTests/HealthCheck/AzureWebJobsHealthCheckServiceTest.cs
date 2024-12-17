using FakeItEasy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.HealthCheck;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.Enums;

namespace UKHO.ExchangeSetService.Common.UnitTests.HealthCheck
{
    public class AzureWebJobsHealthCheckServiceTest
    {
        private IOptions<EssFulfilmentStorageConfiguration> fakeEssFulfilmentStorageConfiguration;
        private IWebJobsAccessKeyProvider fakeWebJobsAccessKeyProvider;
        private IWebHostEnvironment fakeWebHostEnvironment;
        private IAzureBlobStorageService fakeAzureBlobStorageService;
        private IAzureWebJobsHealthCheckClient fakeAzureWebJobsHealthCheckClient;
        private AzureWebJobsHealthCheckService azureWebJobsHealthCheckService;

        [SetUp]
        public void Setup()
        {
            this.fakeEssFulfilmentStorageConfiguration = Options.Create(new EssFulfilmentStorageConfiguration() { ExchangeSetTypes = "sxs,mxs,lxs", WebAppVersion = ""});
            this.fakeWebJobsAccessKeyProvider = A.Fake<IWebJobsAccessKeyProvider>();
            this.fakeWebHostEnvironment = A.Fake<IWebHostEnvironment>();
            this.fakeAzureBlobStorageService = A.Fake<IAzureBlobStorageService>();
            this.fakeAzureWebJobsHealthCheckClient = A.Fake<IAzureWebJobsHealthCheckClient>();

            azureWebJobsHealthCheckService = new AzureWebJobsHealthCheckService(fakeEssFulfilmentStorageConfiguration, fakeWebJobsAccessKeyProvider, fakeWebHostEnvironment, fakeAzureBlobStorageService, fakeAzureWebJobsHealthCheckClient);
        }

        [Test]
        public async Task WhenAzureWebJobStatusIsNotRunning_ThenReturnUnhealthy()
        {
            A.CallTo(() => fakeAzureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(A<ExchangeSetType>.Ignored)).Returns(1);
            A.CallTo(() => fakeAzureWebJobsHealthCheckClient.CheckAllWebJobsHealth(A<List<WebJobDetails>>.Ignored))
               .Returns(new HealthCheckResult(HealthStatus.Unhealthy, "Azure message queue is unhealthy"));

            var response = await azureWebJobsHealthCheckService.CheckHealthAsync();

            Assert.That(HealthStatus.Unhealthy, Is.EqualTo(response.Status));
        }

        [Test]
        public async Task WhenAzureWebJobStatusIsRunning_ThenReturnHealthy()
        {
            A.CallTo(() => fakeAzureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(A<ExchangeSetType>.Ignored)).Returns(1);
            A.CallTo(() => fakeAzureWebJobsHealthCheckClient.CheckAllWebJobsHealth(A<List<WebJobDetails>>.Ignored))
               .Returns(new HealthCheckResult(HealthStatus.Healthy, "Azure message queue is healthy"));

            var response = await azureWebJobsHealthCheckService.CheckHealthAsync();

            Assert.That(HealthStatus.Healthy, Is.EqualTo(response.Status));
        }

        [Test]
        public async Task WhenAzureWebJobStatusIsNotRunningForV2_ThenReturnUnhealthy()
        {
            this.fakeEssFulfilmentStorageConfiguration.Value.WebAppVersion = "v2";

            A.CallTo(() => fakeAzureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(A<ExchangeSetType>.Ignored)).Returns(1);
            A.CallTo(() => fakeAzureWebJobsHealthCheckClient.CheckAllWebJobsHealth(A<List<WebJobDetails>>.Ignored))
                .Returns(new HealthCheckResult(HealthStatus.Unhealthy, "Azure message queue is unhealthy"));

            var response = await azureWebJobsHealthCheckService.CheckHealthAsync();

            Assert.That(HealthStatus.Unhealthy, Is.EqualTo(response.Status));
        }

        [Test]
        public async Task WhenAzureWebJobStatusIsRunningForV2_ThenReturnHealthy()
        {
            this.fakeEssFulfilmentStorageConfiguration.Value.WebAppVersion = "v2";

            A.CallTo(() => fakeAzureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(A<ExchangeSetType>.Ignored)).Returns(1);
            A.CallTo(() => fakeAzureWebJobsHealthCheckClient.CheckAllWebJobsHealth(A<List<WebJobDetails>>.Ignored))
                .Returns(new HealthCheckResult(HealthStatus.Healthy, "Azure message queue is healthy"));

            var response = await azureWebJobsHealthCheckService.CheckHealthAsync();

            Assert.That(HealthStatus.Healthy, Is.EqualTo(response.Status));
        }
    }
}
