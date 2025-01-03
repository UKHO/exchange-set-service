using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.HealthCheck;

namespace UKHO.ExchangeSetService.Common.UnitTests.HealthCheck
{
    public class WebJobsAccessKeyProviderTest
    {
        private IConfiguration configuration;
        private WebJobsAccessKeyProvider webJobsAccessKeyProvider;

        [SetUp]
        public void Setup()
        {
            var inMemorySettings = new Dictionary<string, string> {
                {"webjob1key", "webjob1value"},
                {"webjob2key", "webjob2value"}};

            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            webJobsAccessKeyProvider = new WebJobsAccessKeyProvider(configuration);
        }

        [Test]
        public void GetWebJobAccessKey_ReturnsCorrectKeyWhenExists()
        {
            var webJobsAccessKey = webJobsAccessKeyProvider.GetWebJobsAccessKey("webjob1key");

            var expectedAccessKey = configuration.GetValue<string>("webjob1key");

            Assert.That(expectedAccessKey, Is.EqualTo(webJobsAccessKey));

        }

        [Test]
        public void GetWebJobAccessKey_ReturnsNullWhenNotExists()
        {
            var actualAccessKey = webJobsAccessKeyProvider.GetWebJobsAccessKey("nonexistingkey");

            Assert.That(actualAccessKey, Is.EqualTo(null));
        }
    }
}
