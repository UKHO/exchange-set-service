using FakeItEasy;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.HealthCheck;

namespace UKHO.ExchangeSetService.Common.UnitTests.HealthCheck
{
    public class AzureWebJobsHealthCheckTest
    {
        private IAzureWebJobsHealthCheckService fakeAzureWebJobsHealthCheckService;
        private AzureWebJobsHealthCheck azureWebJobsHealthCheck;
        private ILogger<AzureWebJobsHealthCheck> fakeLogger;

        [SetUp]
        public void Setup()
        {
            this.fakeLogger = A.Fake<ILogger<AzureWebJobsHealthCheck>>();
            this.fakeAzureWebJobsHealthCheckService = A.Fake<IAzureWebJobsHealthCheckService>();

            azureWebJobsHealthCheck = new AzureWebJobsHealthCheck(fakeAzureWebJobsHealthCheckService, fakeLogger);
        }

        [Test]
        public async Task WhenAzureWebJobStatusIsRunning_ThenAzureWebJobsIsHealthy()
        {
            A.CallTo(() => fakeAzureWebJobsHealthCheckService.CheckHealthAsync(A<CancellationToken>.Ignored)).Returns(new HealthCheckResult(HealthStatus.Healthy, "Azure webjob is healthy"));

            var response = await azureWebJobsHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(HealthStatus.Healthy, Is.EqualTo(response.Status));
        }

        [Test]
        public async Task WhenAzureWebJobStatusIsNotRunning_ThenAzureWebJobsIsUnhealthy()
        {
            A.CallTo(() => fakeAzureWebJobsHealthCheckService.CheckHealthAsync(A<CancellationToken>.Ignored)).Returns(new HealthCheckResult(HealthStatus.Unhealthy, "Azure webjob is unhealthy", new Exception("Azure webjob is unhealthy")));

            var response = await azureWebJobsHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(HealthStatus.Unhealthy, Is.EqualTo(response.Status));
        }

        [Test]
        public async Task WhenCheckHealthAsyncThrowException_ThenReturnUnhealthy()
        {
            A.CallTo(() => fakeAzureWebJobsHealthCheckService.CheckHealthAsync(A<CancellationToken>.Ignored)).Throws<Exception>();

            var response = await azureWebJobsHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(HealthStatus.Unhealthy, Is.EqualTo(response.Status));
        }
    }
}
