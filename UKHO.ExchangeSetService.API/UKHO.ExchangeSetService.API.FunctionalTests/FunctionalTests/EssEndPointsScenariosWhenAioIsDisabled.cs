using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    //Product Backlog Item 74919: ESS API Response when AIO feature is OFF and AIO cell is requested
    //// Below config should be set in the "UKHO.ExchangeSetService.API >> AppSettings.json 
    ////"AioConfiguration": {
    ////  "AioEnabled": false,
    ////  "AioCells": "GB800001" }

    public class EssEndPointsScenariosWhenAioIsDisabled
    {
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        private string EssJwtToken { get; set; }
        public ProductIdentifierModel ProductIdentifierModel { get; set; }
        public DataHelper Datahelper { get; set; }

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductIdentifierModel = new ProductIdentifierModel();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            Datahelper = new DataHelper();
        }


        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallTheProductIdentifiersApiWithValidAndGB800001ProductAndAioIsDisabled_ThenACorrectResponseIsReturned()
        {
            ProductIdentifierModel.ProductIdentifier = new List<string>() { "DE5NOBRK", "DE4NO18Q", "DE416080", "GB800001" };

            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(ProductIdentifierModel.ProductIdentifier, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.AreEqual(4, apiResponseData.RequestedProductCount, $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 4.");

            //Verify requested product AlreadyUpToDate count
            Assert.AreEqual(0, apiResponseData.RequestedProductsAlreadyUpToDateCount, $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");
            
            //Verify ExchangeSetCellCount
            Assert.AreEqual(3, apiResponseData.ExchangeSetCellCount, $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 3.");

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.IsNotEmpty(apiResponseData.RequestedProductsNotInExchangeSet, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");
            
            Assert.AreEqual("GB800001", apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'GB800001'");
            Assert.AreEqual("invalidProduct", apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

        }

        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallTheProductVersionApiWithValidAndGB800001ProductAndAioIsDisabled_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new();

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData("DE416080", 9, 1));
            ProductVersiondata.Add(Datahelper.GetProductVersionModelData("GB800001", 1, 0));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            Assert.AreEqual("GB800001", apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName, $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName}, instead of expected Product Name 'GB800001'");
            Assert.AreEqual("invalidProduct", apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

        }

        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallTheProductIdentifiersApiWithOnlyGB800001ProductAndAioIsDisabled_ThenACorrectResponseIsReturned()
        {
            ProductIdentifierModel.ProductIdentifier = new List<string>() { "GB800001" };

            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(ProductIdentifierModel.ProductIdentifier, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.AreEqual(1, apiResponseData.RequestedProductCount, $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 1.");

            //Verify ExchangeSetCellCount
            Assert.AreEqual(0, apiResponseData.ExchangeSetCellCount, $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 0.");

            //Verify requested product AlreadyUpToDate count
            Assert.AreEqual(0, apiResponseData.RequestedProductsAlreadyUpToDateCount, $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.IsNotEmpty(apiResponseData.RequestedProductsNotInExchangeSet, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");

            Assert.AreEqual("GB800001", apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'GB800001'");
            Assert.AreEqual("invalidProduct", apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

        }

        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallTheProductVersionApiOnlyGB800001ProductAndAioIsDisabled_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new();

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData("GB800001", 1, 0));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            Assert.AreEqual("GB800001", apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName, $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName}, instead of expected Product Name 'GB800001'");
            Assert.AreEqual("invalidProduct", apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallTheProductIdentifiersApiForLargeMediaExchangeSetAndGB800001ProductAndAioIsDisabled_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetProductIdentifiersForLargeMediaAndAio(), accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.AreEqual(11, apiResponseData.RequestedProductCount, $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 11.");

            //Verify ExchangeSetCellCount
            Assert.AreEqual(10, apiResponseData.ExchangeSetCellCount, $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 10.");

            //Verify requested product AlreadyUpToDate count
            Assert.AreEqual(0, apiResponseData.RequestedProductsAlreadyUpToDateCount, $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.IsNotEmpty(apiResponseData.RequestedProductsNotInExchangeSet, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");

            Assert.AreEqual("GB800001", apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'GB800001'");
            Assert.AreEqual("invalidProduct", apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

        }
    }
}