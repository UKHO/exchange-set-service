using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    public class ExchangeSetAuthorizationUnencryptedParameterTests : ObjectStorage
    {
        private readonly List<string> cleanUpBatchIdList = new();
        private readonly string sinceDateTime = DateTime.Now.AddDays(-5).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);

        [SetUp]
        public void SetupAsync()
        {
            DataHelper = new DataHelper();
        }

        #region Set DateTime Api
        // PBI 140109 : ESS API : Add authorization to allow only UKHO people to create unencrypted ES 
        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallTheSinceDateTimeApiWithAValidB2cTokenAndUnencryptedParameterAsTrue_ThenAnUnauthorizedResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDateTime, null, accessToken: EssB2CToken, "True");

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        // PBI 140109 : ESS API : Add authorization to allow only UKHO people to create unencrypted ES 
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallTheSinceDateTimeApiWithAValidADTokenAndUnencryptedParameterAsTrue_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDateTime, null, accessToken: EssJwtToken, "True");
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            //Get the BatchId
            var fssBatchId = await apiResponse.GetBatchId();
            cleanUpBatchIdList.Add(fssBatchId);
        }
        #endregion

        #region ProductIdentifier Api
        // PBI 140109 : ESS API : Add authorization to allow only UKHO people to create unencrypted ES 
        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallTheProductIdentifierApiWithAValidB2cTokenAndUnencryptedParameterAsTrue_ThenAnUnauthorizedResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifierData(), null, accessToken: EssB2CToken, "True");

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        // PBI 140109 : ESS API : Add authorization to allow only UKHO people to create unencrypted ES 
        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallTheProductIdentifierApiWithAValidADTokenAndUnencryptedParameterAsTrue_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifierData(), null, accessToken: EssJwtToken, "True");

            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            //Get the BatchId
            var fssBatchId = await apiResponse.GetBatchId();
            cleanUpBatchIdList.Add(fssBatchId);
        }
        #endregion

        #region ProductVersion Api
        // PBI 140109 : ESS API : Add authorization to allow only UKHO people to create unencrypted ES 
        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallTheProductVersionApiWithAValidB2cTokenAndUnencryptedParameterAsTrue_ThenAnUnauthorizedResponseIsReturned()
        {
            List<ProductVersionModel> productVersionData = new()
            {
                DataHelper.GetProductVersionModelData("DE4NO18Q", 1, 0)
            };

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(productVersionData, null, accessToken: EssB2CToken, "True");

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        // PBI 140109 : ESS API : Add authorization to allow only UKHO people to create unencrypted ES 
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallTheProductVersionApiWithAValidADTokenAndUnencryptedParameterAsTrue_ThenACorrectResponseIsReturned()
        {
            List<ProductVersionModel> productVersionData = new()
            {
                DataHelper.GetProductVersionModelData("DE416080", 9, 1)
            };

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(productVersionData, null, accessToken: EssJwtToken, "True");
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            //Get the BatchId
            var fssBatchId = await apiResponse.GetBatchId();
            cleanUpBatchIdList.Add(fssBatchId);
        }
        #endregion

        [OneTimeTearDown]
        public async Task GlobalTeardown()
        {
            if (cleanUpBatchIdList != null && cleanUpBatchIdList.Count > 0)
            {
                //Clean up batches from local folder 
                var apiResponse = await FssApiClient.CleanUpBatchesAsync(Config.FssConfig.BaseUrl, cleanUpBatchIdList, FssJwtToken);
                Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned for clean up batches, instead of the expected 200.");
            }
        }
    }
}