using System;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.HealthCheck;

namespace UKHO.ExchangeSetService.Common.UnitTests.HealthCheck
{
    public class EventHubLoggingHealthCheckTest
    {
        private IEventHubLoggingHealthClient fakeEventHubLoggingHealthClient;
        private EventHubLoggingHealthCheck eventHubLoggingHealthCheck;
        private ILogger<EventHubLoggingHealthCheck> fakeLogger;

        [SetUp]
        public void Setup()
        {
            this.fakeLogger = A.Fake<ILogger<EventHubLoggingHealthCheck>>();
            this.fakeEventHubLoggingHealthClient = A.Fake<IEventHubLoggingHealthClient>();

            eventHubLoggingHealthCheck = new EventHubLoggingHealthCheck(fakeEventHubLoggingHealthClient, fakeLogger);
        }

        [Test]
        public async Task WhenEventHubLoggingIsHealthy_ThenReturnsHealthy()
        {
            A.CallTo(() => fakeEventHubLoggingHealthClient.CheckHealthAsync(A<HealthCheckContext>.Ignored, A<CancellationToken>.Ignored)).Returns(new HealthCheckResult(HealthStatus.Healthy));

            var response = await eventHubLoggingHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(response.Status, Is.EqualTo(HealthStatus.Healthy));
        }

        [Test]
        public async Task WhenEventHubLoggingIsUnhealthy_ThenReturnsUnhealthy()
        {
            A.CallTo(() => fakeEventHubLoggingHealthClient.CheckHealthAsync(A<HealthCheckContext>.Ignored, A<CancellationToken>.Ignored)).Returns(new HealthCheckResult(HealthStatus.Unhealthy, "Event hub is unhealthy", new Exception("Event hub is unhealthy")));

            var response = await eventHubLoggingHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(response.Status, Is.EqualTo(HealthStatus.Unhealthy));
        }
    }
}
