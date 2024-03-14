using NUnit.Framework;
using System;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    public class ExchangeSetProductInformationTests
    {
        private TestConfiguration Config;
        private ExchangeSetApiClient ExchangeSetApiClient;
        private string EssJwtToken;

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            var authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
        }

        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallTheApiWithValidToken_ThenACorrectResponseIsReturned()
        {
            var date = DateTime.Now.AddDays(-10).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'");
            var apiResponse = await ExchangeSetApiClient.GetProductInformationByDateTimeAsync(EssJwtToken, date);
            Assert.AreEqual(200, (int)apiResponse.StatusCode);
        }
    }
}