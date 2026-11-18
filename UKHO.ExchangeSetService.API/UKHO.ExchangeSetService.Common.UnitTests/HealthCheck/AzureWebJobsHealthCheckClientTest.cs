using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.HealthCheck;

namespace UKHO.ExchangeSetService.Common.UnitTests.HealthCheck
{
    public class AzureWebJobsHealthCheckClientTest
    {
        private IWebHostEnvironment _fakeWebHostEnvironment;

        [SetUp]
        public void Setup()
        {
            _fakeWebHostEnvironment = A.Fake<IWebHostEnvironment>();
            A.CallTo(() => _fakeWebHostEnvironment.EnvironmentName).Returns("prod");
        }

        private AzureWebJobsHealthCheckClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            return new AzureWebJobsHealthCheckClient(new HttpClient(new StubHandler(responder)), _fakeWebHostEnvironment)
            {
                RetryDelay = TimeSpan.Zero
            };
        }

        private static HttpResponseMessage Running() =>
            new(HttpStatusCode.OK) { Content = new StringContent("{\"status\":\"Running\"}") };

        private static WebJobDetails Job(string type, int instance, string credential) => new()
        {
            ExchangeSetType = type,
            Instance = instance,
            UserPassword = credential,
            WebJobUri = $"https://ess-prod-{type}-{instance}-webapp.scm.azurewebsites.net/api/continuouswebjobs/ESSFulfilmentWebJob"
        };

        [Test]
        public async Task WhenAllWebJobsAreRunning_ThenReturnHealthy()
        {
            var client = CreateClient(_ => Running());

            var result = await client.CheckAllWebJobsHealth([Job("sxs", 1, "a"), Job("lxs", 1, "b")]);

            Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
        }

        [Test]
        public async Task WhenUnauthorizedIsTransient_ThenRetryAndReturnHealthy()
        {
            var calls = 0;
            var client = CreateClient(_ => ++calls == 1 ? new HttpResponseMessage(HttpStatusCode.Unauthorized) : Running());

            var result = await client.CheckAllWebJobsHealth([Job("sxs", 1, "a")]);

            Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public async Task WhenUnauthorizedPersists_ThenReturnUnhealthyNamingTheWebJob()
        {
            var calls = 0;
            var client = CreateClient(_ => { calls++; return new HttpResponseMessage(HttpStatusCode.Unauthorized); });

            var result = await client.CheckAllWebJobsHealth([Job("sxs", 1, "a")]);

            Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
            Assert.That(result.Exception.Message, Does.Contain("Webjob ess-prod-sxs-1 status code is Unauthorized"));
            Assert.That(calls, Is.EqualTo(3));
        }

        [Test]
        public async Task WhenMultipleWebJobsFail_ThenAllFailuresAreReported()
        {
            var client = CreateClient(r => r.RequestUri.Host.Contains("sxs")
                ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"status\":\"Stopped\"}") });

            var result = await client.CheckAllWebJobsHealth([Job("sxs", 1, "a"), Job("lxs", 1, "b")]);

            Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
            Assert.That(result.Exception.Message, Does.Contain("ess-prod-sxs-1 status code is Unauthorized"));
            Assert.That(result.Exception.Message, Does.Contain("ess-prod-lxs-1 status is Stopped"));
        }

        [Test]
        public async Task WhenRequestThrows_ThenReturnUnhealthy()
        {
            var client = CreateClient(_ => throw new HttpRequestException("boom"));

            var result = await client.CheckAllWebJobsHealth([Job("sxs", 1, "a")]);

            Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
            Assert.That(result.Exception.Message, Does.Contain("boom"));
        }

        [Test]
        public async Task WhenCalledConcurrently_ThenEachRequestCarriesItsOwnCredentials()
        {
            var jobs = new List<WebJobDetails> { Job("sxs", 1, "small"), Job("mxs", 1, "medium"), Job("lxs", 1, "large") };
            var mismatches = new ConcurrentBag<string>();
            var client = CreateClient(r =>
            {
                var expected = jobs.Single(j => j.WebJobUri == r.RequestUri.ToString()).UserPassword;
                if (r.Headers.Authorization?.Parameter != expected)
                {
                    mismatches.Add(r.RequestUri.Host);
                }
                return Running();
            });

            var results = await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => Task.Run(() => client.CheckAllWebJobsHealth(jobs))));

            Assert.That(mismatches, Is.Empty);
            Assert.That(results.All(r => r.Status == HealthStatus.Healthy), Is.True);
        }

        private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return responder(request);
            }
        }
    }
}
