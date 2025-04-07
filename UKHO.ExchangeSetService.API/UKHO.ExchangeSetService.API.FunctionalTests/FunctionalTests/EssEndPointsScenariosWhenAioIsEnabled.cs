using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    public class EssEndPointsScenariosWhenAioIsEnabled : ObjectStorage
    {
        private readonly string SinceDateTime = DateTime.Now.AddDays(-12).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);

        [SetUp]
        public async Task SetupAsync()
        {
            await Task.Delay(30000);//// Delay is required to allow the API to re-run post AIO value swapping
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductIdentifierModel = new ProductIdentifierModel();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            Datahelper = new DataHelper();
        }


        [Test]
        public async Task WhenICallTheProductIdentifiersApiWithValidAndGB800001ProductAndAioIsEnabled_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetAioProductIdentifierData(), accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode,Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.That(apiResponseData.RequestedProductCount, Is.EqualTo(3), $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 3.");

            //Verify requested product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify ExchangeSetCellCount
            Assert.That(apiResponseData.ExchangeSetCellCount, Is.EqualTo(3), $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 3.");

            //Verify requested AIO product count
            Assert.That(apiResponseData.RequestedAioProductCount, Is.EqualTo(1), $"Response body returned RequestedProductCount {apiResponseData.RequestedAioProductCount}, Instead of expected count is 1.");

            //Verify requested AIO product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedAioProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedAioProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify AIO ExchangeSetCellCount
            Assert.That(apiResponseData.AioExchangeSetCellCount, Is.EqualTo(1), $"Response body returned ExchangeSetCellCount {apiResponseData.AioExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 1.");

            //Check RequestedProductsNotInExchangeSet is empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Empty, "Response body returns Not Empty for RequestedProductsNotInExchangeSet, instead of Empty");

        }

        [Test]
        public async Task WhenICallTheProductVersionApiWithValidAndGB800001ProductAndAioIsEnabled_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(Config.AIOConfig.EncCellName, Config.AIOConfig.EncEditionNumber, 1));
            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(Config.AIOConfig.AioCellName, Config.AIOConfig.AioEditionNumber, 0));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check RequestedProductsNotInExchangeSet is empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Empty, "Response body returns Not Empty for RequestedProductsNotInExchangeSet, instead of Empty");

            ProductVersiondata.Clear();
        }

        [Test]
        public async Task WhenICallTheApiWithAValidRFC1123DateTimeAndAioIsEnabled_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(SinceDateTime, accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse(true, false);

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check RequestedProductsNotInExchangeSet is empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Empty, "Response body returns Not Empty for RequestedProductsNotInExchangeSet, instead of Empty");
        }

        [Test]
        public async Task WhenICallTheProductIdentifiersApiWithValidAndGB800001AndAnInvalidProductAndAioIsEnabled_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetAioProductIdentifierAndInvalidData(), accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.That(apiResponseData.RequestedProductCount, Is.EqualTo(4), $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 4.");

            //Verify requested product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify ExchangeSetCellCount
            Assert.That(apiResponseData.ExchangeSetCellCount, Is.EqualTo(3), $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 3.");

            //Verify requested AIO product count
            Assert.That(apiResponseData.RequestedAioProductCount, Is.EqualTo(1), $"Response body returned RequestedProductCount {apiResponseData.RequestedAioProductCount}, Instead of expected count is 1.");

            //Verify requested AIO product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedAioProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedAioProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify AIO ExchangeSetCellCount
            Assert.That(apiResponseData.AioExchangeSetCellCount, Is.EqualTo(1), $"Response body returned ExchangeSetCellCount {apiResponseData.AioExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 1.");

            //Check RequestedProductsNotInExchangeSet is empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Not.Empty, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");

            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName,Is.EqualTo("ABCDEFGH"), $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'ABCDEFGH'");
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, Is.EqualTo("invalidProduct"), $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

        }

        [Test]
        public async Task WhenICallTheProductVersionApiWithValidAndGB800001AndInvalidProductAndAioIsEnabled_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(Config.AIOConfig.EncCellName, Config.AIOConfig.EncEditionNumber, 1));
            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(Config.AIOConfig.InvalidEncCellName, 1, 5));
            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(Config.AIOConfig.AioCellName, Config.AIOConfig.AioEditionNumber, 6));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Not.Empty, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");

            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, Is.EqualTo(Config.AIOConfig.InvalidEncCellName), $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'US2ARCGD'");
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, Is.EqualTo("invalidProduct"), $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

            ProductVersiondata.Clear();

        }

        [Test]
        public async Task WhenICallTheProductIdentifiersApiWithValidEncAndNoAioProductAndAioIsEnabled_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetProductIdentifierData(), accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse(true, false);

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.That(apiResponseData.RequestedProductCount, Is.EqualTo(3), $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 3.");

            //Verify requested product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify ExchangeSetCellCount
            Assert.That(apiResponseData.ExchangeSetCellCount, Is.EqualTo(3), $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 3.");

            //Verify requested AIO product count
            Assert.That(apiResponseData.RequestedAioProductCount, Is.EqualTo(0), $"Response body returned RequestedProductCount {apiResponseData.RequestedAioProductCount}, Instead of expected count is 1.");

            //Verify requested AIO product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedAioProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedAioProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify AIO ExchangeSetCellCount
            Assert.That(apiResponseData.AioExchangeSetCellCount, Is.EqualTo(0), $"Response body returned ExchangeSetCellCount {apiResponseData.AioExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 1.");

            //Check RequestedProductsNotInExchangeSet is empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Empty, "Response body returns Not Empty for RequestedProductsNotInExchangeSet, instead of Empty");

        }

        [Test]
        public async Task WhenICallTheProductVersionApiWithValidEncAndNoAioProductAndAioIsEnabled_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(Config.AIOConfig.EncCellName, 9, 1));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse(true, false);

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check RequestedProductsNotInExchangeSet is empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Empty, "Response body returns Not Empty for RequestedProductsNotInExchangeSet, instead of Empty");

            ProductVersiondata.Clear();
        }

        [Test]
        public async Task WhenICallTheProductIdentifiersApiWithOnlyAioProductAndAioIsEnabled_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetProductIdentifiersForAioOnly(), accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse(false);

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.That(apiResponseData.RequestedProductCount, Is.EqualTo(0), $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 0.");

            //Verify requested product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify ExchangeSetCellCount
            Assert.That(apiResponseData.ExchangeSetCellCount, Is.EqualTo(0), $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 0.");

            //Verify requested AIO product count
            Assert.That(apiResponseData.RequestedAioProductCount, Is.EqualTo(1), $"Response body returned RequestedProductCount {apiResponseData.RequestedAioProductCount}, Instead of expected count is 1.");

            //Verify requested AIO product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedAioProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedAioProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify AIO ExchangeSetCellCount
            Assert.That(apiResponseData.AioExchangeSetCellCount, Is.EqualTo(1), $"Response body returned ExchangeSetCellCount {apiResponseData.AioExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 1.");

            //Check RequestedProductsNotInExchangeSet is empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Empty, "Response body returns Not Empty for RequestedProductsNotInExchangeSet, instead of Empty");
        }

        [Test]
        public async Task WhenICallTheProductVersionApiOnlyAioProductAndAioIsEnabled_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(Config.AIOConfig.AioCellName, 1, 0));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse(false);

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check RequestedProductsNotInExchangeSet is empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Empty, "Response body returns Not Empty for RequestedProductsNotInExchangeSet, instead of Empty");

            ProductVersiondata.Clear();
        }

        [Test]
        public async Task WhenICallTheProductIdentifiersApiWithValidEncAndDuplicateAioProductAndAioIsEnabled_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetDuplicateAioProductIdentifierData(), accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.That(apiResponseData.RequestedProductCount, Is.EqualTo(2), $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 0.");

            //Verify requested product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 2.");

            // Verify ExchangeSetCellCount
            Assert.That(apiResponseData.ExchangeSetCellCount, Is.EqualTo(1), $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 1.");

            //Verify requested AIO product count
            Assert.That(apiResponseData.RequestedAioProductCount, Is.EqualTo(1), $"Response body returned RequestedProductCount {apiResponseData.RequestedAioProductCount}, Instead of expected count is 1.");

            //Verify requested AIO product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedAioProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedAioProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify AIO ExchangeSetCellCount
            Assert.That(apiResponseData.AioExchangeSetCellCount, Is.EqualTo(1), $"Response body returned ExchangeSetCellCount {apiResponseData.AioExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 1.");

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Not.Empty, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");

            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, Is.EqualTo(Config.AIOConfig.AioCellName), $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'GB800001'");
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, Is.EqualTo("duplicateProduct"), $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'duplicateProduct'");

        }

        [Test]
        public async Task WhenICallTheProductVersionApiWithValidEncAndDuplicateAioProductAndAioIsEnabled_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(Config.AIOConfig.EncCellName, 9, 1));
            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(Config.AIOConfig.AioCellName, 1, 0));
            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(Config.AIOConfig.AioCellName, 1, 0));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Not.Empty, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");

            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, Is.EqualTo(Config.AIOConfig.AioCellName), $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'GB800001'");
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, Is.EqualTo("duplicateProduct"), $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'duplicateProduct'");

            ProductVersiondata.Clear();
        }

        [Test]
        public async Task WhenICallTheProductIdentifiersApiWithValidEncProductAndAioCellWhichIsNotAvailableAndAioIsEnabled_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetAdditionalAioProductIdentifierData(), accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse(shouldAioFileUriExist: false);

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.That(apiResponseData.RequestedProductCount, Is.EqualTo(3), $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 3.");

            //Verify requested product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify ExchangeSetCellCount
            Assert.That(apiResponseData.ExchangeSetCellCount, Is.EqualTo(3), $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 3.");

            //Verify requested AIO product count
            Assert.That(apiResponseData.RequestedAioProductCount, Is.EqualTo(1), $"Response body returned RequestedProductCount {apiResponseData.RequestedAioProductCount}, Instead of expected count is 1.");

            //Verify requested AIO product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedAioProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedAioProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify AIO ExchangeSetCellCount
            Assert.That(apiResponseData.AioExchangeSetCellCount, Is.EqualTo(0), $"Response body returned ExchangeSetCellCount {apiResponseData.AioExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 0.");

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Not.Empty, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");

            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, Is.EqualTo(Config.AIOConfig.InvalidAioCellName), $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'GZ800112'");
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, Is.EqualTo("invalidProduct"), $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");
            
        }

        [Test]
        public async Task WhenICallTheProductVersionApiWithValidEncAndAioIsEnabled_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata =
            [
                Datahelper.GetProductVersionModelData(Config.AIOConfig.EncCellName, 9, 1),
                Datahelper.GetProductVersionModelData(Config.AIOConfig.InvalidAioCellName, 4, 6),
            ];

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse(shouldAioFileUriExist: false);

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.That(apiResponseData.RequestedProductCount, Is.EqualTo(2), $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 2.");

            //Verify requested product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify ExchangeSetCellCount
            Assert.That(apiResponseData.ExchangeSetCellCount, Is.EqualTo(1), $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 1.");

            //Verify requested AIO product count
            Assert.That(apiResponseData.RequestedAioProductCount, Is.EqualTo(1), $"Response body returned RequestedProductCount {apiResponseData.RequestedAioProductCount}, Instead of expected count is 1.");

            //Verify requested AIO product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedAioProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedAioProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify AIO ExchangeSetCellCount
            Assert.That(apiResponseData.AioExchangeSetCellCount, Is.EqualTo(0), $"Response body returned ExchangeSetCellCount {apiResponseData.AioExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 0.");

            //Check RequestedProductsNotInExchangeSet is empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Not.Empty, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");

            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, Is.EqualTo(Config.AIOConfig.InvalidAioCellName), $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'GZ800112'");
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, Is.EqualTo("invalidProduct"), $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

            ProductVersiondata.Clear();
        }

        [Test]
        public async Task WhenICallTheProductIdentifiersApiWithLargeMediaCellsAndAioCellWhichIsNotAvailableAndAioIsEnabled_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetProductIdentifiersForLargeMediaAndAioNotPresent(), accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse(shouldAioFileUriExist:false);

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.That(apiResponseData.RequestedProductCount, Is.EqualTo(10), $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 10.");

            //Verify requested product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify ExchangeSetCellCount
            Assert.That(apiResponseData.ExchangeSetCellCount, Is.EqualTo(11), $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 3.");

            //Verify requested AIO product count
            Assert.That(apiResponseData.RequestedAioProductCount, Is.EqualTo(1), $"Response body returned RequestedProductCount {apiResponseData.RequestedAioProductCount}, Instead of expected count is 0.");

            //Verify requested AIO product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedAioProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedAioProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify AIO ExchangeSetCellCount
            Assert.That(apiResponseData.AioExchangeSetCellCount, Is.EqualTo(0), $"Response body returned ExchangeSetCellCount {apiResponseData.AioExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 0.");

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Not.Empty, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");

            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, Is.EqualTo(Config.AIOConfig.InvalidAioCellName), $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'GZ800112'");
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, Is.EqualTo("invalidProduct"), $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");
        }

        [Test]
        public async Task WhenICallTheProductIdentifiersApiWithLargeMediaCellsAndAioIsEnabled_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetProductIdentifiersForLargeMedia(), accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse(true, false);

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.That(apiResponseData.RequestedProductCount, Is.EqualTo(10), $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 10.");

            //Verify requested product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify ExchangeSetCellCount
            Assert.That(apiResponseData.ExchangeSetCellCount, Is.EqualTo(10), $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 3.");

            //Verify requested AIO product count
            Assert.That(apiResponseData.RequestedAioProductCount, Is.EqualTo(0), $"Response body returned RequestedProductCount {apiResponseData.RequestedAioProductCount}, Instead of expected count is 0.");

            //Verify requested AIO product AlreadyUpToDate count
            Assert.That(apiResponseData.RequestedAioProductsAlreadyUpToDateCount, Is.EqualTo(0), $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedAioProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify AIO ExchangeSetCellCount
            Assert.That(apiResponseData.AioExchangeSetCellCount, Is.EqualTo(0), $"Response body returned ExchangeSetCellCount {apiResponseData.AioExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 0.");

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Empty, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");
        }
    }
}
