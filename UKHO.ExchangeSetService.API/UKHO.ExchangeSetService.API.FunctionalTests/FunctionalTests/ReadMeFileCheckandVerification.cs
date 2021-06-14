using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    public class ReadMeFileCheckandVerification
    {
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        private DataHelper DataHelper { get; set; }

        private readonly string sinceDateTime = DateTime.Now.AddDays(-10).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);
        private string EssJwtToken { get; set; }
        public ProductIdentifierModel ProductIdentifierModel { get; set; }

        
        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
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
        public async Task WhenICallTheApiWithAValidRFC1123DateTime_ThenAReadMeFileIsCreated()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDateTime, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 200.");

            var BatchId = await apiResponse.GetBatchId();
            var filePath = Path.Combine(Config.FileDownloadPath, DateTime.UtcNow.ToString("ddMMMyyyy"), BatchId, Config.ExchangeSetFileFolder, Config.EncRootFolder);
            bool checkIfFileExistInDirectory = FileCheck.CheckIfFileExistAndVerify(filePath, Config.ReadMeFileName);
            Assert.IsTrue(checkIfFileExistInDirectory);
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductIdentifiers_ThenAReadMeFileIsCreated()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifierData(), accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var BatchId = await apiResponse.GetBatchId();
            var filePath = Path.Combine(Config.FileDownloadPath, DateTime.UtcNow.ToString("ddMMMyyyy"), BatchId, Config.ExchangeSetFileFolder, Config.EncRootFolder);
            bool checkIfFileExistInDirectory = FileCheck.CheckIfFileExistAndVerify(filePath, Config.ReadMeFileName);
            Assert.IsTrue(checkIfFileExistInDirectory);
        }

        [Test]
        public async Task WhenICallTheApiWithInvalidProductIdentifiers_ThenAReadMeFileIsCreated()
        {
            ProductIdentifierModel.ProductIdentifier = new List<string>() { "GB123789" };

            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(ProductIdentifierModel.ProductIdentifier, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var BatchId = await apiResponse.GetBatchId();
            var filePath = Path.Combine(Config.FileDownloadPath, DateTime.UtcNow.ToString("ddMMMyyyy"), BatchId, Config.ExchangeSetFileFolder, Config.EncRootFolder);
            bool checkIfFileExistInDirectory = FileCheck.CheckIfFileExistAndVerify(filePath, Config.ReadMeFileName);
            Assert.IsTrue(checkIfFileExistInDirectory);
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductVersion_TThenAReadMeFileIsCreated()
        {
            List<ProductVersionModel> ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 6));
            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE4NO18Q", 1, 0));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

            var BatchId = await apiResponse.GetBatchId();
            var filePath = Path.Combine(Config.FileDownloadPath, DateTime.UtcNow.ToString("ddMMMyyyy"), BatchId, Config.ExchangeSetFileFolder, Config.EncRootFolder);
            bool checkIfFileExistInDirectory = FileCheck.CheckIfFileExistAndVerify(filePath, Config.ReadMeFileName);
            Assert.IsTrue(checkIfFileExistInDirectory);
        }

        [Test]
        public async Task WhenICallTheApiWithValidAndInvalidProductVersion_ThenAReadMeFileIsCreated()
        {
            List<ProductVersionModel> ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 5));
            ProductVersionData.Add(DataHelper.GetProductVersionModelData("GB123789", 1, 0));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            var BatchId = await apiResponse.GetBatchId();
            var filePath = Path.Combine(Config.FileDownloadPath, DateTime.UtcNow.ToString("ddMMMyyyy"), BatchId, Config.ExchangeSetFileFolder, Config.EncRootFolder);
            bool checkIfFileExistInDirectory = FileCheck.CheckIfFileExistAndVerify(filePath, Config.ReadMeFileName);
            Assert.IsTrue(checkIfFileExistInDirectory);
        }

        [Test]
        public async Task WhenICallTheApiWithProductVersionNotAvailable_ThenAReadMeFileIsCreated()
        {
            List<ProductVersionModel> ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 100, 5));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            var BatchId = await apiResponse.GetBatchId();
            var filePath = Path.Combine(Config.FileDownloadPath, DateTime.UtcNow.ToString("ddMMMyyyy"), BatchId, Config.ExchangeSetFileFolder, Config.EncRootFolder);
            bool checkIfFileExistInDirectory = FileCheck.CheckIfFileExistAndVerify(filePath, Config.ReadMeFileName);
            Assert.IsTrue(checkIfFileExistInDirectory);
        }
    }
}
