
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class CreateFssBatchAndFile
    {
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper DataHelper { get; set; }
        public ProductIdentifierModel ProductIdentifierModel { get; set; }        
        private string EssJwtToken { get; set; }       

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductIdentifierModel = new ProductIdentifierModel();            
            DataHelper = new DataHelper();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();          

        }

        [Test]
        public async Task WhenICallTheApiWithAValidRFC1123DateTime_ThenACorrectResponseIsReturned()
        {
            string sinceDatetime = DateTime.Now.AddDays(-20).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDatetime, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 200.");

            //verify FssBatchResponse
            await apiResponse.CheckFssBatchResponse();
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductIdentifiers_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetOnlyProductIdentifierData(), accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify FssBatchResponse
            await apiResponse.CheckFssBatchResponse();
        }

        [Test]
        public async Task WhenICallTheApiWithInvalidProductIdentifiers_ThenAInternalServerErrorResponseIsReturned()
        {
            ProductIdentifierModel.ProductIdentifier = new List<string>() { "GB123789" };

            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(ProductIdentifierModel.ProductIdentifier, accessToken: EssJwtToken);
            Assert.AreEqual(500, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 500.");

        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductVersion_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 6));
            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE4NO18Q", 1, 0));                     
           
            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

            //verify FssBatchResponse
            await apiResponse.CheckFssBatchResponse();
        }

        [Test]
        public async Task WhenICallTheApiWithValidAndInvalidProductVersion_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 5));
            ProductVersionData.Add(DataHelper.GetProductVersionModelData("GB123789", 1, 0));            

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify FssBatchResponse
            await apiResponse.CheckFssBatchResponse();

        }

    }
}
