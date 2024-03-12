using System.Threading.Tasks;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    public class ExchangeSetProductDataValidateIdentifierTests
    {
        private TestConfiguration Config;
        private ExchangeSetApiClient ExchangeSetApiClient;
        private string EssJwtToken;
        private DataHelper DataHelper;

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            var authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            DataHelper = new DataHelper();
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallTheApiWithValidToken_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetValidateIdentifierAsync(EssJwtToken, DataHelper.GetOnlyProductIdentifierData());
            Assert.AreEqual(200, (int)apiResponse.StatusCode);
        }
    }
}