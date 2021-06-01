using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests.FileNameCheck
{
    public class AzureBlobStorageFileNameCheck
    {
        private ExchangeSetApiClient ExchangesetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        private DataHelper DataHelper { get; set; }

        private readonly string sinceDateTime = DateTime.Now.AddDays(-10).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);
        private string EssJwtToken { get; set; }
        public ProductIdentifierModel ProductIdentifierModel { get; set; }

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangesetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductIdentifierModel = new ProductIdentifierModel();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            DataHelper = new DataHelper();
        }

        /// <summary>
        /// This Test check container created in Azure storage account,
        /// When Api with Valid RFC1123DateTime is called
        /// </summary>
        [Test]
        public async Task WhenICallTheApiWithAValidRFC1123DateTime_ThenValidFileNameIsPresentInAzureStorage()
        {
            var apiResponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDateTime, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 200.");

            bool checkContainer = await AzureBlobStorageCheck.CheckIfFileNameExist(Config.EssStorageAccountConnectionString, apiResponse);
            Assert.IsTrue(checkContainer);
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductIdentifiers_ThenValidFileNameIsPresentInAzureStorage()
        {
            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(DataHelper.GetProductIdentifierData(), accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            bool checkContainer = await AzureBlobStorageCheck.CheckIfFileNameExist(Config.EssStorageAccountConnectionString, apiResponse);
            Assert.IsTrue(checkContainer);
        }

        [Test]
        public async Task WhenICallTheApiWithInvalidProductIdentifiers_ThenValidFileNameIsPresentInAzureStorage()
        {
            ProductIdentifierModel.ProductIdentifier = new List<string>() { "GB123789" };

            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifierModel.ProductIdentifier, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            bool checkContainer = await AzureBlobStorageCheck.CheckIfFileNameExist(Config.EssStorageAccountConnectionString, apiResponse);
            Assert.IsTrue(checkContainer);
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductVersion_ThenValidFileNameIsPresentInAzureStorage()
        {
            List<ProductVersionModel> ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 6));
            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE4NO18Q", 1, 0));

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

            bool checkContainer = await AzureBlobStorageCheck.CheckIfFileNameExist(Config.EssStorageAccountConnectionString, apiResponse);
            Assert.IsTrue(checkContainer);
        }

        [Test]
        public async Task WhenICallTheApiWithValidAndInvalidProductVersion_ThenValidFileNameIsPresentInAzureStorage()
        {
            List<ProductVersionModel> ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 5));
            ProductVersionData.Add(DataHelper.GetProductVersionModelData("GB123789", 1, 0));

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            bool checkContainer = await AzureBlobStorageCheck.CheckIfFileNameExist(Config.EssStorageAccountConnectionString, apiResponse);
            Assert.IsTrue(checkContainer);
        }
    }
}
