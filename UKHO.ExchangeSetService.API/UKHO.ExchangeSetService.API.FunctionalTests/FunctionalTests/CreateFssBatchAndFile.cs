
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
        private ExchangeSetApiClient ExchangesetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper Datahelper { get; set; }
        public ProductIdentifierModel ProductIdentifiermodel { get; set; }        
        private string EssJwtToken { get; set; }       

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangesetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductIdentifiermodel = new ProductIdentifierModel();            
            Datahelper = new DataHelper();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();          

        }

        [Test]
        public async Task WhenICallTheApiWithAValidRFC1123DateTime_ThenACorrectResponseIsReturned()
        {
            string sinceDatetime = DateTime.Now.AddDays(-20).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);
            var apiResponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDatetime, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 200.");

            //verify FssBatchResponse
            await apiResponse.CheckFssBatchResponse();
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductIdentifiers_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(Datahelper.GetProductIdentifierData(), accessToken: EssJwtToken);
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

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData("DE416080", 9, 6));
            ProductVersiondata.Add(Datahelper.GetProductVersionModelData("DE4NO18Q", 1, 0));                     
           
            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

            //verify FssBatchResponse
            await apiResponse.CheckFssBatchResponse();
        }

        [Test]
        public async Task WhenICallTheApiWithValidAndInvalidProductVersion_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData("DE416080", 9, 5));
            ProductVersiondata.Add(Datahelper.GetProductVersionModelData("GB123789", 1, 0));            

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify FssBatchResponse
            await apiResponse.CheckFssBatchResponse();

        }

    }
}
