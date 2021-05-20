
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class CheckFssBatch
    {
        private ExchangeSetApiClient ExchangesetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper Datahelper { get; set; }
        public ProductIdentifierModel ProductIdentifiermodel { get; set; }
        public ProductVersionModel ProductVersionmodel { get; set; }
        private string EssJwtToken { get; set; }
        private string EssJwtTokenNoRole { get; set; }
        private string EssJwtCustomizedToken { get; set; }

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangesetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductIdentifiermodel = new ProductIdentifierModel();
            ProductVersionmodel = new ProductVersionModel();
            Datahelper = new DataHelper();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            EssJwtTokenNoRole = await authTokenProvider.GetEssTokenNoAuth();
            EssJwtCustomizedToken = authTokenProvider.GenerateCustomToken();

        }

        [Test]
        public async Task WhenICallTheApiWithAValidRFC1123DateTime_ThenACorrectResponseIsReturned()
        {
            string sinceDatetime = "Mon, 01 Mar 2021 00:00:00 GMT";
            var apiResponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDatetime, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 200.");

            //verify FssBatchResponse
            await apiResponse.CheckFssBatchResponse();
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductIdentifiers_ThenACorrectResponseIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "PE62263A", "FR373890", "CN584233" };

            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify FssBatchResponse
            await apiResponse.CheckFssBatchResponse();
        }

        [Test]
        public async Task WhenICallTheApiWithInvalidProductIdentifiers_ThenACorrectResponseIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "GB123789" };

            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify FssBatchResponse
            await apiResponse.CheckFssBatchResponse();
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductVersion_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersionmodel.ProductName = "GR3CFZMG";
            ProductVersionmodel.EditionNumber = 2;
            ProductVersionmodel.UpdateNumber = 30;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            ProductVersionmodel.ProductName = "GB50184F";
            ProductVersionmodel.EditionNumber = 5;
            ProductVersionmodel.UpdateNumber = 1;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

            //verify FssBatchResponse
            await apiResponse.CheckFssBatchResponse();
        }

        [Test]
        public async Task WhenICallTheApiWithValidAndInvalidProductVersion_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersionmodel.ProductName = "GR3CFZMG";
            ProductVersionmodel.EditionNumber = 2;
            ProductVersionmodel.UpdateNumber = 30;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            ProductVersionmodel.ProductName = "GR3FZMG1";
            ProductVersionmodel.EditionNumber = 2;
            ProductVersionmodel.UpdateNumber = 30;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify FssBatchResponse
            await apiResponse.CheckFssBatchResponse();

        }

    }
}
