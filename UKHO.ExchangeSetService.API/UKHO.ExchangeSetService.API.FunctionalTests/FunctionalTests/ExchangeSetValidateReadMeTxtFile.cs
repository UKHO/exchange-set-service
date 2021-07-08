using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class ExchangeSetValidateReadMeTxtFile
    {
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private static TestConfiguration Config { get; set; }
        public DataHelper DataHelper { get; set; }
        public ProductIdentifierModel ProductIdentifierModel { get; set; }
        private string EssJwtToken { get; set; }
        private FssApiClient FssApiClient { get; set; }
        private string FssJwtToken { get; set; }

        private static bool fileContentCheck = false;
        private static bool fileExistCheck = false;

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductIdentifierModel = new ProductIdentifierModel();
            DataHelper = new DataHelper();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssApiClient = new FssApiClient();
            FssJwtToken = await authTokenProvider.GetFssToken();
        }

        /// <summary>
        /// Checks if Directory contains the README File
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> CheckIfFileExistAndVerify(string filePath, string readMeFileName)
        {
            var fullPath = filePath + @"\" + readMeFileName;

            //Added step to wait for file exist in specific folder
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < TimeSpan.FromMinutes(Config.FileDownloadWaitTime))
            {
                await Task.Delay(5000);
                if (File.Exists(fullPath))
                {
                    fileExistCheck = true;
                    break;
                }
            }

            if (fileExistCheck)
            {
                string[] lines = File.ReadAllLines(fullPath);
                var fileSecondLineContent = lines[1];

                string[] fileContents = fileSecondLineContent.Split("File date:");

                //Verifying file contents - second line of the readme file
                Assert.True(fileSecondLineContent.Contains(fileContents[0]));

                var utcDateTime = fileContents[1].Remove(fileContents[1].Length - 1);

                Assert.True(DateTime.Parse(utcDateTime) <= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second), $"Response body returned ExpiryDateTime {utcDateTime} , greater than the expected value.");


                fileContentCheck = true;
            }
            else
            {
                fileContentCheck = false;
            }

            return fileContentCheck;

        }


        [Test]
        public async Task WhenICallExchangeSetApiWithAValidProductIdentifiers_ThenAReadMeTxtFileIsGenerated()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetOnlyProductIdentifierData(), accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseDetails = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseDetails.Links.ExchangeSetBatchStatusUri;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);

            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseDetails.Links.ExchangeSetFileUri;

            var responseFileDownload = await FssApiClient.GetFileDownloadAsync(downloadFileUrl.ToString(), accessToken: FssJwtToken);

            Assert.AreEqual(200, (int)responseFileDownload.StatusCode, $"Incorrect status code File Download api returned {responseFileDownload.StatusCode}, instead of the expected 200.");

            bool readMeFileContentCheck = await CheckIfFileExistAndVerify(filePath, readMeFileName);

            Assert.IsTrue(readMeFileContentCheck);
        }

        [Test]
        public async Task WhenICallTheApiWithInvalidProductIdentifiers_ThenAReadMeTxtFileIsGenerated()
        {
            ProductIdentifierModel.ProductIdentifier = new List<string>() { "GB123789" };

            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(ProductIdentifierModel.ProductIdentifier, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseDetails = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseDetails.Links.ExchangeSetBatchStatusUri;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);

            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseDetails.Links.ExchangeSetFileUri;

            var responseFileDownload = await FssApiClient.GetFileDownloadAsync(downloadFileUrl.ToString(), accessToken: FssJwtToken);

            Assert.AreEqual(200, (int)responseFileDownload.StatusCode, $"Incorrect status code File Download api returned {responseFileDownload.StatusCode}, instead of the expected 200.");

            bool readMeFileContentCheck = await CheckIfFileExistAndVerify();

            Assert.IsTrue(readMeFileContentCheck);
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductVersion_ThenAReadMeTxtFileIsGenerated()
        {
            List<ProductVersionModel> ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 6));
            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE4NO18Q", 1, 0));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

            var apiResponseDetails = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseDetails.Links.ExchangeSetBatchStatusUri;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);

            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseDetails.Links.ExchangeSetFileUri;

            var responseFileDownload = await FssApiClient.GetFileDownloadAsync(downloadFileUrl.ToString(), accessToken: FssJwtToken);

            Assert.AreEqual(200, (int)responseFileDownload.StatusCode, $"Incorrect status code File Download api returned {responseFileDownload.StatusCode}, instead of the expected 200.");

            bool readMeFileContentCheck = await CheckIfFileExistAndVerify();

            Assert.IsTrue(readMeFileContentCheck);
        }

        [Test]
        public async Task WhenICallTheApiWithValidAndInvalidProductVersion_ThenAReadMeTxtFileIsGenerated()
        {
            List<ProductVersionModel> ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 5));
            ProductVersionData.Add(DataHelper.GetProductVersionModelData("GB123789", 1, 0));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            var apiResponseDetails = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseDetails.Links.ExchangeSetBatchStatusUri;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);

            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseDetails.Links.ExchangeSetFileUri;

            var responseFileDownload = await FssApiClient.GetFileDownloadAsync(downloadFileUrl.ToString(), accessToken: FssJwtToken);

            Assert.AreEqual(200, (int)responseFileDownload.StatusCode, $"Incorrect status code File Download api returned {responseFileDownload.StatusCode}, instead of the expected 200.");

            bool readMeFileContentCheck = await CheckIfFileExistAndVerify();

            Assert.IsTrue(readMeFileContentCheck);
        }

        [Test]
        public async Task WhenICallTheApiWithProductVersionNotAvailable_ThenAReadMeTxtFileIsGenerated()
        {
            List<ProductVersionModel> ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 100, 5));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            var apiResponseDetails = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseDetails.Links.ExchangeSetBatchStatusUri;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);

            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseDetails.Links.ExchangeSetFileUri;

            var responseFileDownload = await FssApiClient.GetFileDownloadAsync(downloadFileUrl.ToString(), accessToken: FssJwtToken);

            Assert.AreEqual(200, (int)responseFileDownload.StatusCode, $"Incorrect status code File Download api returned {responseFileDownload.StatusCode}, instead of the expected 200.");

            bool readMeFileContentCheck = await CheckIfFileExistAndVerify();

            Assert.IsTrue(readMeFileContentCheck);

        }
    }
}