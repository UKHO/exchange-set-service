using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class ExchangeSetByProductIdentifiers
    {
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        private string EssJwtToken { get; set; }
        private string EssJwtTokenNoRole { get; set; }
        private string EssJwtCustomizedToken { get; set; }
        public ProductIdentifierModel ProductIdentifierModel { get; set; }
        public DataHelper Datahelper { get; set; }
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
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            EssJwtTokenNoRole = await authTokenProvider.GetEssTokenNoAuth();
            EssJwtCustomizedToken = authTokenProvider.GenerateCustomToken();
            Datahelper = new DataHelper();
            FssJwtToken = await authTokenProvider.GetFssToken();
        }

        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallTheApiWithOutAuthToken_ThenAnUnauthorisedResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetProductIdentifierData());

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallTheApiWithTamperedToken_ThenAnUnauthorisedResponseIsReturned()
        {
            string tamperedEssJwtToken = EssJwtToken.Remove(EssJwtToken.Length - 2);
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetProductIdentifierData(), accessToken: tamperedEssJwtToken);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallTheApiWithCustomToken_ThenAnUnauthorisedResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetProductIdentifierData(), accessToken: EssJwtCustomizedToken);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        [Category("QCOnlyTest")]
        public async Task WhenICallTheApiWithNoRoleToken_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetProductIdentifierData(), accessToken: EssJwtTokenNoRole);

            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //Get the BatchId
            var batchId = await apiResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);
        }

        [Test]
        [Category("QCOnlyTest")]
        public async Task WhenICallTheApiWithAValidProductIdentifiers_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetProductIdentifierData(), accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            //Get the BatchId
            var batchId = await apiResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);
        }

        [Test]
        [Category("SmokeTest-AIODisabled")]
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
        [Category("QCOnlyTest")]
        public async Task WhenICallTheApiWithDuplicateProductIdentifiers_ThenACorrectResponseIsReturned()
        {
            ProductIdentifierModel.ProductIdentifier = new List<string>() { "DE5NOBRK", "DE5NOBRK" };

            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(ProductIdentifierModel.ProductIdentifier, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureNotModifiedResponse();

            //Get the BatchId
            var batchId = await apiResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);

        }

        [Test]
        [Category("QCOnlyTest")]
        public async Task WhenICallTheApiWithValidAndInvalidProductIdentifiers_ThenACorrectResponseIsReturned()
        {
            ProductIdentifierModel.ProductIdentifier = new List<string>() { "DE4NO18Q", "GB123789" };

            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(ProductIdentifierModel.ProductIdentifier, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();
            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            Assert.AreEqual("GB123789", apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'GB123789'");
            Assert.AreEqual("invalidProduct", apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().Reason, $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

            //Get the BatchId
            var batchId = await apiResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);
        }

        [Test]
        [Category("QCOnlyTest")]
        public async Task WhenICallTheApiWithAValidProductIdentifierswithCallBackURI_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetProductIdentifierData(), "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272%22", accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned  {apiResponse.StatusCode}, instead of of the expected status 200.");

            //Get the BatchId
            var batchId = await apiResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);
        }

        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallTheApiWithAnEmptyProductIdentifier_ThenABadRequestStatusIsReturned()
        {
            ProductIdentifierModel.ProductIdentifier = new List<string>();

            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(ProductIdentifierModel.ProductIdentifier, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272%22", accessToken: EssJwtToken);
            Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 400.");

            var errorMessage = await apiResponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "requestBody"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Either body is null or malformed."));
        }

        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallTheApiWithANullProductIdentifier_ThenABadRequestStatusIsReturned()
        {
            ProductIdentifierModel.ProductIdentifier = new List<string>() { null };

            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(ProductIdentifierModel.ProductIdentifier, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272%22", accessToken: EssJwtToken);
            Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 400.");

            var errorMessage = await apiResponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "productIdentifier"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "productIdentifiers cannot be null or empty."));
        }

        [TestCase("fss.ukho.gov.uk", TestName = "Callback URL without https")]
        [TestCase("https:/fss.ukho.gov.uk", TestName = "Callback URL with wrong https request")]
        [TestCase("ftp://fss.ukho.gov.uk", TestName = "Callback URL with ftp request")]
        [TestCase("https://", TestName = "Callback URL with only https request")]
        [TestCase("http://fss.ukho.gov.uk", TestName = "Callback URL with http request")]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallTheApiWithAInvalidCallbackURIWithProductIdentifier_ThenABadRequestResponseIsReturned(string callBackUrl)
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetProductIdentifierData(), callBackUrl, accessToken: EssJwtToken);
            Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 400.");

            var errorMessage = await apiResponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "callbackUri"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Invalid callbackUri format."));
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