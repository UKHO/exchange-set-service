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
        private IAzureWebJobsHealthCheck fakeAzureWebJobsHealthCheckClient;
        private AzureWebJobsHealthCheck azureWebJobsHealthCheck;
        private ILogger<AzureWebJobsHealthCheck> fakeLogger;

        [SetUp]
        public void Setup()
        {
            this.fakeLogger = A.Fake<ILogger<AzureWebJobsHealthCheck>>();
            this.fakeAzureWebJobsHealthCheckClient = A.Fake<IAzureWebJobsHealthCheck>();

            azureWebJobsHealthCheck = new AzureWebJobsHealthCheck(fakeAzureWebJobsHealthCheckClient, fakeLogger);
        }

        [Test]
        public async Task WhenAzureWebJobsRunning_ThenAzureWebJobsIsHealthy()
        {
            A.CallTo(() => fakeAzureWebJobsHealthCheckClient.CheckHealthAsync(A<HealthCheckContext>.Ignored, A<CancellationToken>.Ignored)).Returns(new HealthCheckResult(HealthStatus.Healthy, "Azure webjob is healthy"));

            var response = await azureWebJobsHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.AreEqual(HealthStatus.Healthy, response.Status);
        }

        [Test]
        public async Task WhenAzureWebJobsNotRunning_ThenAzureWebJobsIsUnhealthy()
        {
            A.CallTo(() => fakeAzureWebJobsHealthCheckClient.CheckHealthAsync(A<HealthCheckContext>.Ignored, A<CancellationToken>.Ignored)).Returns(new HealthCheckResult(HealthStatus.Unhealthy, "Azure webjob is unhealthy", new Exception("Azure webjob is unhealthy")));

            var response = await azureWebJobsHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.AreEqual(HealthStatus.Unhealthy, response.Status);
        }
    }
}
