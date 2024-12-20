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
        private ITelemetryProcessor _fakeTelemetryProcessor;
        private AzureDependencyFilterTelemetryProcessor _fakeAzureDependencyFilterTelemetryProcessor;
        private const string FakeRequestUri = "https://test.blob.core.windows.net/test/test.TXT?skoid=000&sktid=00&ske=000";
        private const string FakeRequestUriWithoutQueryString = "https://test.blob.core.windows.net/test/test.TXT";

        [SetUp]
        public void Setup()
        {
            _fakeTelemetryProcessor = A.Fake<ITelemetryProcessor>();
            _fakeAzureDependencyFilterTelemetryProcessor = new AzureDependencyFilterTelemetryProcessor(_fakeTelemetryProcessor);
        }

        [Test]
        public void WhenDependencyTelemetryIsNotOfTypeAzureBlob_ThenReturnActualRequestUri()
        {
            var item = new DependencyTelemetry { Data = FakeRequestUri, Type = "No blob" };

            _fakeAzureDependencyFilterTelemetryProcessor.Process(item);
            var result = item.Data;

            Assert.That(result, Is.EqualTo(FakeRequestUri));
        }

        [Test]
        public void WhenDependencyTelemetryIsOfTypeAzureBlob_ThenReturnRequestUriWithoutQueryString()
        {
            var item = new DependencyTelemetry { Data = FakeRequestUri, Type = "Azure blob" };

            _fakeAzureDependencyFilterTelemetryProcessor.Process(item);
            var result = item.Data;

            Assert.That(result, Is.EqualTo(FakeRequestUriWithoutQueryString));
        }
    }
}
