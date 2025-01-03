using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Helpers;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class CommonHelperTest
    {
        private ILogger<FileShareService> fakeLogger;
        public int retryCount = 3;
        private const double sleepDuration = 2;
        const string TestClient = "TestClient";
        private bool _isRetryCalled;

        [SetUp]
        public void Setup()
        {
            fakeLogger = A.Fake<ILogger<FileShareService>>();
        }

        [Test]
        public void CheckMethodReturns_CorrectWeekNumer()
        {
            var week1 = CommonHelper.GetCurrentWeekNumber(Convert.ToDateTime("2021/01/07"));
            var week26 = CommonHelper.GetCurrentWeekNumber(Convert.ToDateTime("2021/07/01"));
            var week53 = CommonHelper.GetCurrentWeekNumber(Convert.ToDateTime("2021/01/01"));

            Assert.That(week1, Is.EqualTo(1));
            Assert.That(week26, Is.EqualTo(26));
            Assert.That(week53, Is.EqualTo(53));
        }

        [Test]
        public void CheckConversionOfBytesToMegabytes()
        {
            var fileSize = CommonHelper.ConvertBytesToMegabytes((long)4194304);
            Assert.That(fileSize, Is.EqualTo(4));
        }

        [Test]
        public async Task WhenTooManyRequests_GetRetryPolicy()
        {
            // Arrange 
            IServiceCollection services = new ServiceCollection();
            _isRetryCalled = false;
            retryCount = 1;

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(CommonHelper.GetRetryPolicy(fakeLogger, "File Share", Common.Logging.EventIds.RetryHttpClientFSSRequest, retryCount, sleepDuration))
                .AddHttpMessageHandler(() => new TooManyRequestsDelegatingHandler());

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            // Act
            var result = await configuredClient.GetAsync("https://test.com");

            // Assert
            Assert.That(_isRetryCalled, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        }

        [Test]
        public async Task WhenServiceUnavailable_GetRetryPolicy()
        {
            // Arrange 
            IServiceCollection services = new ServiceCollection();
            _isRetryCalled = false;

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(CommonHelper.GetRetryPolicy(fakeLogger, "Sales Catalogue", Common.Logging.EventIds.RetryHttpClientSCSRequest, retryCount, sleepDuration))
                .AddHttpMessageHandler(() => new ServiceUnavailableDelegatingHandler());

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            // Act
            var result = await configuredClient.GetAsync("https://test.com");

            // Assert
            Assert.That(_isRetryCalled, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        }

        [Test]
        public void CheckIsNumericReturnsTrueForNumbers()
        {
            bool isNum = CommonHelper.IsNumeric(1234);
            Assert.That(isNum, Is.True);
        }

        [Test]
        public void CheckIsNumericReturnsFalseForNonNumericValue()
        {
            bool isNum = CommonHelper.IsNumeric("1234a");
            Assert.That(isNum, Is.False);
        }
    }
}
