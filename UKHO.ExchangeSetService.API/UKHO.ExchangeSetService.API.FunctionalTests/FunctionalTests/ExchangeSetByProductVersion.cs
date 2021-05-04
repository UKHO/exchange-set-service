using NUnit.Framework;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class ExchangeSetByProductVersion
    {
        private ExchangeSetApiClient ExchangesetApiClient { get; set; }
        private TestConfiguration Config { get; set; }

        [SetUp]
        public void Setup()
        {
            Config = new TestConfiguration();
            ExchangesetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
        }
    }
}
