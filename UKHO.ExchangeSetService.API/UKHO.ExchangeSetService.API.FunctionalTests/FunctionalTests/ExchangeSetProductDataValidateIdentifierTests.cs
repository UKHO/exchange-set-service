using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

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
            var payload = DataHelper.GetProductIdentifierData();
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetProductIdentifiersAsync(EssJwtToken, payload);
            Assert.AreEqual(200, (int)apiResponse.StatusCode);
            var responseContent = await apiResponse.Content.ReadFromJsonAsync<ExchangeSetProductIdentifierResponse>();
            var productNames = responseContent.Products.Select(r => r.ProductName).ToList();
            Assert.IsTrue(productNames.All(r => payload.Contains(r)));
        }
    }
}