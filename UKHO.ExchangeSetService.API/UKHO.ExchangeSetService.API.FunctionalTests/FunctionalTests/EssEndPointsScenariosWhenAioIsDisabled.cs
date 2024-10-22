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

    public class EssEndPointsScenariosWhenAioIsDisabled: ObjectStorage
    {
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
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallTheProductIdentifiersApiWithValidAndGB800001ProductAndAioIsDisabled_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetAioProductIdentifierData(), accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.That(apiResponseData.RequestedProductCount, Is.EqualTo(4), $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 4.");

            //Verify requested product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");
            
            //Verify ExchangeSetCellCount
            Assert.That(apiResponseData.ExchangeSetCellCount, Is.EqualTo(3), $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 3.");

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Not.Empty, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");
            
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, Is.EqualTo(Config.AIOConfig.AioCellName), $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'GB800001'");
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason,Is.EqualTo("invalidProduct"), $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallTheProductVersionApiWithValidAndGB800001ProductAndAioIsDisabled_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new();

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(Config.AIOConfig.EncCellName, Config.AIOConfig.EncEditionNumber, Config.AIOConfig.EncUpdateNumber-1));
            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(Config.AIOConfig.AioCellName, Config.AIOConfig.AioEditionNumber, Config.AIOConfig.AioEditionNumber));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName, Is.EqualTo(Config.AIOConfig.AioCellName), $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName}, instead of expected Product Name 'GB800001'");
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, Is.EqualTo("invalidProduct"), $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallTheProductIdentifiersApiWithOnlyGB800001ProductAndAioIsDisabled_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetProductIdentifiersForAioOnly(), accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.That(apiResponseData.RequestedProductCount, Is.EqualTo(1), $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 1.");

            //Verify ExchangeSetCellCount
            Assert.That(apiResponseData.ExchangeSetCellCount, Is.EqualTo(0), $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 0.");

            //Verify requested product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Not.Empty, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");

            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, Is.EqualTo(Config.AIOConfig.AioCellName), $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'GB800001'");
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, Is.EqualTo("invalidProduct"), $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallTheProductVersionApiOnlyGB800001ProductAndAioIsDisabled_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new();

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(Config.AIOConfig.AioCellName, Config.AIOConfig.AioEditionNumber, Config.AIOConfig.AioUpdateNumber));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName, Is.EqualTo(Config.AIOConfig.AioCellName), $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName}, instead of expected Product Name 'GB800001'");
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, Is.EqualTo("invalidProduct"), $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallTheProductIdentifiersApiForLargeMediaExchangeSetAndGB800001ProductAndAioIsDisabled_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetProductIdentifiersForLargeMediaAndAio(), accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.That(apiResponseData.RequestedProductCount, Is.EqualTo(11), $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 11.");

            //Verify ExchangeSetCellCount
            Assert.That(apiResponseData.ExchangeSetCellCount, Is.EqualTo(10), $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 10.");

            //Verify requested product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Not.Empty, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");

            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, Is.EqualTo(Config.AIOConfig.AioCellName), $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'GB800001'");
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, Is.EqualTo("invalidProduct"), $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

        }
    }
}
