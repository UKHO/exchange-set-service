using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.HealthCheck;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.Enums;

namespace UKHO.ExchangeSetService.Common.UnitTests.HealthCheck
{
    public class AzureWebJobsHealthCheckServiceTest
    {
        private EssFulfilmentStorageConfiguration _essFulfilmentStorageConfiguration;
        private IOptions<EssFulfilmentStorageConfiguration> _fakeEssFulfilmentStorageConfiguration;
        private IWebJobsAccessKeyProvider _fakeWebJobsAccessKeyProvider;
        private IWebHostEnvironment _fakeWebHostEnvironment;
        private IAzureBlobStorageService _fakeAzureBlobStorageService;
        private IAzureWebJobsHealthCheckClient _fakeAzureWebJobsHealthCheckClient;
        private AzureWebJobsHealthCheckService _azureWebJobsHealthCheckService;

        [SetUp]
        public void Setup()
        {
            _essFulfilmentStorageConfiguration = new EssFulfilmentStorageConfiguration { ExchangeSetTypes = "sxs,mxs,lxs", WebAppVersion = "" };
            _fakeEssFulfilmentStorageConfiguration = A.Fake<IOptions<EssFulfilmentStorageConfiguration>>();
            A.CallTo(() => _fakeEssFulfilmentStorageConfiguration.Value).ReturnsLazily(() => _essFulfilmentStorageConfiguration);
            _fakeWebJobsAccessKeyProvider = A.Fake<IWebJobsAccessKeyProvider>();
            _fakeWebHostEnvironment = A.Fake<IWebHostEnvironment>();
            _fakeAzureBlobStorageService = A.Fake<IAzureBlobStorageService>();
            _fakeAzureWebJobsHealthCheckClient = A.Fake<IAzureWebJobsHealthCheckClient>();

            _azureWebJobsHealthCheckService = new AzureWebJobsHealthCheckService(_fakeEssFulfilmentStorageConfiguration, _fakeWebJobsAccessKeyProvider, _fakeWebHostEnvironment, _fakeAzureBlobStorageService, _fakeAzureWebJobsHealthCheckClient);
        }

        [Test]
        public async Task WhenAzureWebJobStatusIsNotRunning_ThenReturnUnhealthy()
        {
            A.CallTo(() => _fakeAzureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(A<ExchangeSetType>.Ignored)).Returns(1);
            A.CallTo(() => _fakeAzureWebJobsHealthCheckClient.CheckAllWebJobsHealth(A<List<WebJobDetails>>.Ignored))
                .Returns(new HealthCheckResult(HealthStatus.Unhealthy, "Azure message queue is unhealthy"));

            var response = await _azureWebJobsHealthCheckService.CheckHealthAsync();

            Assert.That(response.Status, Is.EqualTo(HealthStatus.Unhealthy));
        }

        [Test]
        public async Task WhenAzureWebJobStatusIsRunning_ThenReturnHealthy()
        {
            A.CallTo(() => _fakeAzureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(A<ExchangeSetType>.Ignored)).Returns(1);
            A.CallTo(() => _fakeAzureWebJobsHealthCheckClient.CheckAllWebJobsHealth(A<List<WebJobDetails>>.Ignored))
                .Returns(new HealthCheckResult(HealthStatus.Healthy, "Azure message queue is healthy"));

            var response = await _azureWebJobsHealthCheckService.CheckHealthAsync();

            Assert.That(response.Status, Is.EqualTo(HealthStatus.Healthy));
        }

        [Test]
        public async Task WhenAzureWebJobStatusIsNotRunningForV2_ThenReturnUnhealthy()
        {
            _fakeEssFulfilmentStorageConfiguration.Value.WebAppVersion = "v2";

            A.CallTo(() => _fakeAzureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(A<ExchangeSetType>.Ignored)).Returns(1);
            A.CallTo(() => _fakeAzureWebJobsHealthCheckClient.CheckAllWebJobsHealth(A<List<WebJobDetails>>.Ignored))
                .Returns(new HealthCheckResult(HealthStatus.Unhealthy, "Azure message queue is unhealthy"));

            var response = await _azureWebJobsHealthCheckService.CheckHealthAsync();

            Assert.That(response.Status, Is.EqualTo(HealthStatus.Unhealthy));
        }

        [Test]
        public async Task WhenAzureWebJobStatusIsRunningForV2_ThenReturnHealthy()
        {
            _fakeEssFulfilmentStorageConfiguration.Value.WebAppVersion = "v2";

            A.CallTo(() => _fakeAzureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(A<ExchangeSetType>.Ignored)).Returns(1);
            A.CallTo(() => _fakeAzureWebJobsHealthCheckClient.CheckAllWebJobsHealth(A<List<WebJobDetails>>.Ignored))
                .Returns(new HealthCheckResult(HealthStatus.Healthy, "Azure message queue is healthy"));

            var response = await _azureWebJobsHealthCheckService.CheckHealthAsync();

            Assert.That(response.Status, Is.EqualTo(HealthStatus.Healthy));
        }

        [Test]
        public void WhenAnInvalidExchangeSetTypeExists_ThenThrowAnException()
        {
            _essFulfilmentStorageConfiguration = new EssFulfilmentStorageConfiguration { ExchangeSetTypes = "osx,sxs,mxs,lxs", WebAppVersion = "" };

            Assert.ThrowsAsync<ConfigurationErrorsException>(() => _azureWebJobsHealthCheckService.CheckHealthAsync());
        }
    }
}
