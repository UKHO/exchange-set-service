using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Helpers;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers
{
    public class HttpRetryPoliciesTest
    {
        private static HttpClient CreateClient(Func<int, HttpStatusCode> statusForCall, Action onCall)
        {
            var calls = 0;
            var services = new ServiceCollection();
            services.AddHttpClient("TestClient")
                .AddPolicyHandler(HttpRetryPolicies.NonSuccess(3, TimeSpan.Zero))
                .ConfigurePrimaryHttpMessageHandler(() => new StubHandler(() =>
                {
                    onCall();
                    return new HttpResponseMessage(statusForCall(++calls));
                }));

            return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient("TestClient");
        }

        [Test]
        public async Task WhenResponseIsNotSuccess_ThenRetriesConfiguredTimes()
        {
            var calls = 0;
            var client = CreateClient(_ => HttpStatusCode.Unauthorized, () => calls++);

            var result = await client.GetAsync("https://test.com");

            Assert.That(calls, Is.EqualTo(4));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task WhenResponseBecomesSuccess_ThenStopsRetrying()
        {
            var calls = 0;
            var client = CreateClient(call => call == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK, () => calls++);

            var result = await client.GetAsync("https://test.com");

            Assert.That(calls, Is.EqualTo(2));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task WhenResponseIsSuccess_ThenDoesNotRetry()
        {
            var calls = 0;
            var client = CreateClient(_ => HttpStatusCode.OK, () => calls++);

            await client.GetAsync("https://test.com");

            Assert.That(calls, Is.EqualTo(1));
        }

        private sealed class StubHandler(Func<HttpResponseMessage> responder) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(responder());
        }
    }
}
