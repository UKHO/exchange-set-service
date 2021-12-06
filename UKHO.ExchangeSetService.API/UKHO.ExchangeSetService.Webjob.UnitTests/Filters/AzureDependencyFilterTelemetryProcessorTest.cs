using FakeItEasy;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using NUnit.Framework;
using UKHO.ExchangeSetService.FulfilmentService.Filters;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Filters
{
    [TestFixture]
    public class AzureDependencyFilterTelemetryProcessorTest
    {
        private ITelemetryProcessor fakeTelemetryProcessor;
        private AzureDependencyFilterTelemetryProcessor fakeAzureDependencyFilterTelemetryProcessor;
        private const string fakeRequestUri = "https://test.blob.core.windows.net/test/test.TXT?skoid=000&sktid=00&ske=000";
        private const string fakeRequestUriWithoutQueryString = "https://test.blob.core.windows.net/test/test.TXT";

        [SetUp]
        public void Setup()
        {
            fakeTelemetryProcessor = A.Fake<ITelemetryProcessor>();
            fakeAzureDependencyFilterTelemetryProcessor = new AzureDependencyFilterTelemetryProcessor(fakeTelemetryProcessor);
        }

        [Test]
        public void WhenDependencyTelemetryIsNotOfTypeAzureBlob_ThenReturnActualRequestUri()
        {
            DependencyTelemetry item = new DependencyTelemetry() {Data = fakeRequestUri, Type = "No blob"};

            fakeAzureDependencyFilterTelemetryProcessor.Process(item);
            var result = item.Data;

            Assert.AreEqual(fakeRequestUri, result);
        }

        [Test]
        public void WhenDependencyTelemetryIsOfTypeAzureBlob_ThenReturnRequestUriWithoutQueryString()
        {
            DependencyTelemetry item = new DependencyTelemetry() { Data = fakeRequestUri, Type = "Azure blob"};

            fakeAzureDependencyFilterTelemetryProcessor.Process(item);
            var result = item.Data;

            Assert.AreEqual(fakeRequestUriWithoutQueryString, result);
        }
    }
}
