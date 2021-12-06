using NUnit.Framework;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class ExchangeSetFssRedirectResponse
    {
        private string EssJwtToken { get; set; }
        private string FssJwtToken { get; set; }
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private FssApiClient FssApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper DataHelper { get; set; }

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            FssApiClient = new FssApiClient();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssJwtToken = await authTokenProvider.GetFssToken();
            DataHelper = new DataHelper();
           
            var ApiEssResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(new List<string>() { "DE260001" }, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)ApiEssResponse.StatusCode, $"Incorrect status code is returned {ApiEssResponse.StatusCode}, instead of the expected status 200.");
        }

        [Test]
        [Category("QCOnlyTest")]
        public async Task WhenICallTheApiWithProductIdentifiers_ThenARedirectResponseIsReturned()        {
           
            string fssDownLoadUrl = $"{Config.FssConfig.BaseUrl}/batch/e478ee2e-8602-44f6-b6fd-08075357c9f1/files/DE260001.000";

            var response = await FssApiClient.GetFileDownloadAsync(fssDownLoadUrl, accessToken: FssJwtToken);
            Assert.AreEqual(200, (int)response.StatusCode, $"Incorrect status code File Download api returned {response.StatusCode} for the url {fssDownLoadUrl}, instead of the expected 200.");
            
            Assert.IsTrue(response.Headers.Contains("x-redirect-status"));       
          
        }

        [Test]
        [Category("QCOnlyTest")]
        public async Task WhenICallTheApiWithProductIdentifiers_ThenNoRedirectResponseIsReturned()
        {
            string fssDownLoadUrl = $"{Config.FssConfig.BaseUrl}/batch/6c5e2434-5a4b-4b4a-b865-49586a9767c6/files/DEJ60001.000";

            var response = await FssApiClient.GetFileDownloadAsync(fssDownLoadUrl, accessToken: FssJwtToken);
            Assert.AreEqual(200, (int)response.StatusCode, $"Incorrect status code File Download api returned {response.StatusCode} for the url {fssDownLoadUrl}, instead of the expected 200.");
            
            Assert.IsFalse(response.Headers.Contains("x-redirect-status"));
        }

    }
}
