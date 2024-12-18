
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests.AIODisabled
{
    class CreateFssBatchAndFile
    {
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper DataHelper { get; set; }
        public ProductIdentifierModel ProductIdentifierModel { get; set; }
        private string EssJwtToken { get; set; }
        private FssApiClient FssApiClient { get; set; }
        private string FssJwtToken { get; set; }
        private readonly List<string> CleanUpBatchIdList = new List<string>();

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            FssApiClient = new FssApiClient();
            ProductIdentifierModel = new ProductIdentifierModel();
            DataHelper = new DataHelper();
            var authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssJwtToken = await authTokenProvider.GetFssToken();
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallTheApiWithAValidRFC1123DateTime_ThenACorrectResponseIsReturned()
        {
            var sinceDatetime = DateTime.Now.AddDays(-5).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDatetime, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 200.");

            //verify FssBatchResponse
            await apiResponse.CheckFssBatchResponse();

            //Get the BatchId
            var batchId = await apiResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);


        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallTheApiWithAValidProductIdentifiers_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetOnlyProductIdentifierData(), accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify FssBatchResponse
            await apiResponse.CheckFssBatchResponse();

            //Get the BatchId
            var batchId = await apiResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallTheApiWithInvalidProductIdentifiers_ThenACorrectResponseIsReturned()
        {
            ProductIdentifierModel.ProductIdentifier = new List<string>() { "GB123789" };

            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(ProductIdentifierModel.ProductIdentifier, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Verify requested product count
            Assert.AreEqual(1, apiResponseData.RequestedProductCount, $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 1.");

            Assert.AreEqual(0, apiResponseData.RequestedProductsAlreadyUpToDateCount, $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");
            // Verify ExchangeSetCellCount
            Assert.AreEqual(0, apiResponseData.ExchangeSetCellCount, $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 0.");

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.IsNotEmpty(apiResponseData.RequestedProductsNotInExchangeSet, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");
            Assert.AreEqual("invalidProduct", apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallTheApiWithAValidProductVersion_ThenTheCorrectResponseIsReturned()
        {
            var ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 1));
            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE4NO18Q", 2, 0));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

            //verify FssBatchResponse
            await apiResponse.CheckFssBatchResponse();

            //Get the BatchId
            var batchId = await apiResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallTheApiWithValidAndInvalidProductVersion_ThenTheCorrectResponseIsReturned()
        {
            var ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 1));
            ProductVersionData.Add(DataHelper.GetProductVersionModelData("GB123789", 1, 0));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify FssBatchResponse
            await apiResponse.CheckFssBatchResponse();

            //Get the BatchId
            var batchId = await apiResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);

        }

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
