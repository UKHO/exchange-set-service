﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    ////Product Backlog Item 74325: ESS API Response Structure when AIO feature is ON and AIO cell is requested
    //// Below config should be set in the "UKHO.ExchangeSetService.API >> AppSettings.json 
    ////"AioConfiguration": {
    ////  "AioEnabled": true,
    ////  "AioCells": "GB800001" }
    
    public class EssEndPointsScenariosWhenAioIsEnabled
    {
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        private string EssJwtToken { get; set; }
        public ProductIdentifierModel ProductIdentifierModel { get; set; }
        public DataHelper Datahelper { get; set; }

        private readonly string SinceDateTime = DateTime.Now.AddDays(-5).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);

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
        [Category("SmokeTest-AIOEnabled")]
        public async Task WhenICallTheProductIdentifiersApiWithValidAndGB800001ProductAndAioIsEnabled_ThenACorrectResponseIsReturned()
        {
            ProductIdentifierModel.ProductIdentifier = new List<string>() { "DE5NOBRK", "DE4NO18Q", "DE416080", "GB800001" };

            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(ProductIdentifierModel.ProductIdentifier, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.AreEqual(4, apiResponseData.RequestedProductCount, $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 4.");

            //Verify requested product AlreadyUpToDate count
            Assert.AreEqual(0, apiResponseData.RequestedProductsAlreadyUpToDateCount, $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");
            
            // Verify ExchangeSetCellCount
            Assert.AreEqual(3, apiResponseData.ExchangeSetCellCount, $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 3.");

            //Verify requested AIO product count
            Assert.AreEqual(1, apiResponseData.RequestedAioProductCount, $"Response body returned RequestedProductCount {apiResponseData.RequestedAioProductCount}, Instead of expected count is 1.");

            //Verify requested AIO product AlreadyUpToDate count
            Assert.AreEqual(0, apiResponseData.RequestedAioProductsAlreadyUpToDateCount, $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedAioProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");
            
            // Verify AIO ExchangeSetCellCount
            Assert.AreEqual(1, apiResponseData.AioExchangeSetCellCount, $"Response body returned ExchangeSetCellCount {apiResponseData.AioExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 1.");

            //Check RequestedProductsNotInExchangeSet is empty
            Assert.IsEmpty(apiResponseData.RequestedProductsNotInExchangeSet, "Response body returns Not Empty for RequestedProductsNotInExchangeSet, instead of Empty");

            ProductIdentifierModel.ProductIdentifier.Clear();
            
        }

        [Test]
        [Category("SmokeTest-AIOEnabled")]
        public async Task WhenICallTheProductVersionApiWithValidAndGB800001ProductAndAioIsEnabled_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData("DE416080", 9, 1));
            ProductVersiondata.Add(Datahelper.GetProductVersionModelData("CA479226", 4, 6));
            ProductVersiondata.Add(Datahelper.GetProductVersionModelData("GB800001", 1, 0));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check RequestedProductsNotInExchangeSet is empty
            Assert.IsEmpty(apiResponseData.RequestedProductsNotInExchangeSet, "Response body returns Not Empty for RequestedProductsNotInExchangeSet, instead of Empty");
        }

        [Test]
        [Category("SmokeTest-AIOEnabled")]
        public async Task WhenICallTheApiWithAValidRFC1123DateTimeAndAioIsEnabled_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(SinceDateTime, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check RequestedProductsNotInExchangeSet is empty
            Assert.IsEmpty(apiResponseData.RequestedProductsNotInExchangeSet, "Response body returns Not Empty for RequestedProductsNotInExchangeSet, instead of Empty");
        }

        [Test]
        [Category("SmokeTest-AIOEnabled")]
        public async Task WhenICallTheProductIdentifiersApiWithValidAndGB800001AndAnInvalidProductAndAioIsEnabled_ThenACorrectResponseIsReturned()
        {
            ProductIdentifierModel.ProductIdentifier = new List<string>() { "DE5NOBRK", "DE4NO18Q", "DE416080", "GB800001", "ABCDEFGH" };

            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(ProductIdentifierModel.ProductIdentifier, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForAioSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            //Verify requested product count
            Assert.AreEqual(5, apiResponseData.RequestedProductCount, $"Response body returned RequestedProductCount {apiResponseData.RequestedProductCount}, Instead of expected count is 4.");

            //Verify requested product AlreadyUpToDate count
            Assert.AreEqual(0, apiResponseData.RequestedProductsAlreadyUpToDateCount, $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify ExchangeSetCellCount
            Assert.AreEqual(3, apiResponseData.ExchangeSetCellCount, $"Response body returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 3.");

            //Verify requested AIO product count
            Assert.AreEqual(1, apiResponseData.RequestedAioProductCount, $"Response body returned RequestedProductCount {apiResponseData.RequestedAioProductCount}, Instead of expected count is 1.");

            //Verify requested AIO product AlreadyUpToDate count
            Assert.AreEqual(0, apiResponseData.RequestedAioProductsAlreadyUpToDateCount, $"Response body returned RequestedProductsAlreadyUpToDateCount : {apiResponseData.RequestedAioProductsAlreadyUpToDateCount}, Instead of expected RequestedProductsAlreadyUpToDateCount is 0.");

            // Verify AIO ExchangeSetCellCount
            Assert.AreEqual(1, apiResponseData.AioExchangeSetCellCount, $"Response body returned ExchangeSetCellCount {apiResponseData.AioExchangeSetCellCount}, instead of expected ExchangeSetCellCount is 1.");

            //Check RequestedProductsNotInExchangeSet is empty
            Assert.IsNotEmpty(apiResponseData.RequestedProductsNotInExchangeSet, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");

            Assert.AreEqual("ABCDEFGH", apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'ABCDEFGH'");
            Assert.AreEqual("invalidProduct", apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

            ProductIdentifierModel.ProductIdentifier.Clear();
        }
    }
}