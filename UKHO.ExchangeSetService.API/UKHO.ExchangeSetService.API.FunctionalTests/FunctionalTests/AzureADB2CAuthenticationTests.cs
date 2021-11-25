using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class AzureADB2CAuthenticationTests
    {
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private FssApiClient FssApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        private string EssB2CToken { get; set; }
        private string EssB2CCustomizedToken { get; set; }
        public DataHelper DataHelper { get; set; }
        private string FssJwtToken { get; set; }
        private readonly List<string> CleanUpBatchIdList = new List<string>();
        private readonly string SinceDateTime = DateTime.Now.AddDays(-5).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            FssApiClient = new FssApiClient();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            AzureB2CAuthTokenProvider b2cAuthTokenProvider = new AzureB2CAuthTokenProvider();
            EssB2CToken = await b2cAuthTokenProvider.GetToken();
            FssJwtToken = await authTokenProvider.GetFssToken();
            EssB2CCustomizedToken = b2cAuthTokenProvider.GenerateCustomToken();
            DataHelper = new DataHelper();
        }

        #region Set DateTime Api
        [Test]
        [Category("SmokeTest")]
        public async Task WhenICallTheDateTimeApiWithOutAzureB2cToken_ThenAnUnauthorisedResponseIsReturned()
        {

            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(SinceDateTime);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }


        [Test]
        [Category("SmokeTest")]
        public async Task WhenICallTheDateTimeApiWithInvalidB2cToken_ThenAnUnauthorisedResponseIsReturned()
        {
            string invalidB2cToken = EssB2CToken.Remove(EssB2CToken.Length - 2).Insert(EssB2CToken.Length - 2, "AA");
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(SinceDateTime, accessToken: invalidB2cToken);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        [Category("SmokeTest")]
        public async Task WhenICallTheDateTimeApiWithCustomB2cToken_ThenAnUnauthorisedResponseIsReturned()
        {

            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(SinceDateTime, accessToken: EssB2CCustomizedToken);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        [Category("QCOnlyTest")]
        public async Task WhenICallTheDateTimeApiWithAValidB2cToken_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(SinceDateTime, accessToken: EssB2CToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            //Get the BatchId
            var batchId = await apiResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);

        }
        #endregion

        #region ProductIdentifier Api
        [Test]
        [Category("SmokeTest")]
        public async Task WhenICallTheProductIdentifierApiWithOutAzureB2cToken_ThenAnUnauthorisedResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifierData());

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        [Category("SmokeTest")]
        public async Task WhenICallTheProductIdentifierApiWithInvalidB2cToken_ThenAnUnauthorisedResponseIsReturned()
        {
            string invalidB2cToken = EssB2CToken.Remove(EssB2CToken.Length - 2).Insert(EssB2CToken.Length - 2, "AA");
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifierData(), accessToken: invalidB2cToken);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        [Category("SmokeTest")]
        public async Task WhenICallTheProductIdentifierApiWithCustomB2cToken_ThenAnUnauthorisedResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifierData(), accessToken: EssB2CCustomizedToken);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        [Category("QCOnlyTest")]
        public async Task WhenICallTheProductIdentifiersApiWithAValidB2cToken_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifierData(), accessToken: EssB2CToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            //Get the BatchId
            var batchId = await apiResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);
        }
        #endregion

        #region ProductVersion Api
        [Test]
        [Category("SmokeTest")]
        public async Task WhenICallTheProductVersionApiWithOutB2cToken_ThenAnUnauthorisedResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 6));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        [Category("SmokeTest")]
        public async Task WhenICallTheProductVersionApiWithInvalidB2cToken_ThenAnUnauthorisedResponseIsReturned()
        {
            string invalidB2cToken = EssB2CToken.Remove(EssB2CToken.Length - 2).Insert(EssB2CToken.Length - 2, "AA");

            List<ProductVersionModel> ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 6));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: invalidB2cToken);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        [Category("SmokeTest")]
        public async Task WhenICallTheProductVersionApiWithCustomB2cToken_ThenAnUnauthorisedResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE4NO18Q", 1, 0));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssB2CCustomizedToken);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        [Category("QCOnlyTest")]
        public async Task WhenICallTheProductVersionApiWithAValidB2cToken_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 1));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssB2CToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            //Get the BatchId
            var batchId = await apiResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);
        }
        #endregion

        [OneTimeTearDown]
        public async Task GlobalTeardown()
        {
            if (CleanUpBatchIdList != null && CleanUpBatchIdList.Count > 0)
            {
                //Clean up batches from local foldar 
                var apiResponse = await FssApiClient.CleanUpBatchesAsync(Config.FssConfig.BaseUrl, CleanUpBatchIdList, FssJwtToken);
                Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned for clean up batches, instead of the expected 200.");
            }
        }
    }
}