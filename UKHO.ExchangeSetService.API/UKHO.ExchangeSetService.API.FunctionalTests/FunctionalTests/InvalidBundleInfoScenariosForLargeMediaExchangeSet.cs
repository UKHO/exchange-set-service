using NUnit.Framework;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using System.Net.Http;
using System.Collections.Generic;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;


namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    public class InvalidBundleInfoScenariosForLargeMediaExchangeSet
    {
        private string EssJwtToken { get; set; }
        private string FssJwtToken { get; set; }
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper DataHelper { get; set; }
        private HttpResponseMessage ApiEssResponse { get; set; }
        public ProductIdentifierModel ProductIdentifierModel { get; set; }

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssJwtToken = await authTokenProvider.GetFssToken();
            DataHelper = new DataHelper();
            ProductIdentifierModel = new ProductIdentifierModel();
        }

        [Test]
        [TestCase("DE521900", TestName = "bundle >> bundletype as 'ABC' instead of 'DVD'")]
        [TestCase("CA172005", TestName = "bundle >> bundletype as '<empty>' instead of 'DVD'")]
        [TestCase("US5CN13M", TestName = "bundle >> location as 'M0;B5' instead of 'M1;B5 or M2;B5'")]
        [TestCase("NO3B2020", TestName = "bundle >> location as 'MA;B2' instead of 'M(1-2);B2'")]
        [TestCase("NZ300661", TestName = "bundle >> location as 'M2;B100' instead of 'M2;B(1-99)'")]
        [TestCase("RU3P0ZM0", TestName = "bundle >> location as 'M1;BA' instead of 'M1;B(1-99)'")]
        [Category("QCOnlyTest")]
        public async Task WhenICallExchangeSetApiWithInvalidBundleInfoProperties_ThenAnErrorTxtFileIsGenerated(string Product)
        {
            ProductIdentifierModel.ProductIdentifier = new List<string>() { Product };
            ApiEssResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(ProductIdentifierModel.ProductIdentifier, accessToken: EssJwtToken);

            var response = await FileContentHelper.CreateErrorFileValidation(ApiEssResponse, FssJwtToken);
            Assert.AreEqual(200, (int)response.StatusCode, $"Incorrect status code for creation of Error.txt: {response.StatusCode}, instead of the expected 200.");

            ProductIdentifierModel.ProductIdentifier.Clear();
        }
    }
}
