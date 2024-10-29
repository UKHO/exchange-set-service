using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    [TestFixture]
    class ExchangeSetGeneratesEmptyZipForInvalidCellForProductIdentifierWhenAioIsEnabled : ObjectStorage
    {
        public ObjectStorage objStorage = new();

        private static IEnumerable<TestCaseData> TestDataForProductIdentifier()
        {
            yield return new TestCaseData(DataHelper.GetProductIdentifiersForInvalidProduct());
            yield return new TestCaseData(DataHelper.GetProductIdentifiersForInvalidEncAndInValidAio());
            yield return new TestCaseData(DataHelper.GetProductIdentifiersForInvalidAioCells());
            yield return new TestCaseData(DataHelper.GetProductIdentifiersForInvalidProductAndValidAio());
            yield return new TestCaseData(DataHelper.GetProductIdentifiersForInvalidEncAndValidAndInvalidAioCell());
            yield return new TestCaseData(DataHelper.GetProductIdentifiersForValidAndInvalidAioCell());
        }

        [OneTimeSetUp]
        public void SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ScsApiClient = new SalesCatalogueApiClient(Config.ScsAuthConfig.BaseUrl);
            FssApiClient = new FssApiClient();
            DataHelper = new DataHelper();
        }

        [Test]
        [TestCaseSource(nameof(TestDataForProductIdentifier))]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task VerifyEmptyExchangeSetForProductIdentifier(List<string> product)
        {
            // rhz debug start
            Console.WriteLine("Rhz ObjectStorage check");
            var objectStorageData = JsonConvert.SerializeObject(objStorage, Formatting.Indented);
            Console.WriteLine("State of Object Storage: " + objectStorageData);
            // rhz debug end


            ApiEssResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(product, accessToken: objStorage.EssJwtToken);
            Assert.That((int)ApiEssResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {ApiEssResponse.StatusCode}, instead of the expected status 200.");

            //// Check Download file URL
            batchId = await ApiEssResponse.GetBatchId();
            string downloadFileUrl = $"{objStorage.Config.FssConfig.BaseUrl}/batch/{batchId}/files/{objStorage.Config.ExchangeSetFileName}";

            var finalBatchStatusUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/status";

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(finalBatchStatusUrl, FssJwtToken);
            Assert.That(batchStatus, Is.EqualTo("Committed"), $"Incorrect batch status is returned {batchStatus}, instead of the expected status Committed.");

            var response = await FssApiClient.GetFileDownloadAsync(downloadFileUrl, accessToken: objStorage.FssJwtToken);
            Assert.That((int)response.StatusCode, Is.EqualTo(200), $"Incorrect status code File Download api returned {response.StatusCode} for the url {downloadFileUrl}, instead of the expected 404.");

            //// Download File
            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFile(ApiEssResponse, objStorage.FssJwtToken);

            //// ENC_ROOT >>> ReadmeTxtFile
            bool checkFileReadme = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder), objStorage.Config.ExchangeReadMeFile);
            Assert.That(checkFileReadme,Is.True, $"{objStorage.Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder)}");
            FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder, objStorage.Config.ExchangeReadMeFile));

            //// ENC_ROOT >> Catalog.031
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeSetCatalogueFile);
            Assert.That(checkFile,Is.True, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetCatalogueFile)}");

            //Verify Catalog file content
            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, DataHelper.GetProductIdentifiers(), ScsJwtToken);
            Assert.That((int)apiScsResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            FileContentHelper.CheckCatalogueFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeSetCatalogueFile), apiScsResponseData);

            ////SERIAL.ENC 
            bool checkFileSerial = FssBatchHelper.CheckforFileExist(DownloadedFolderPath, objStorage.Config.ExchangeSetSerialEncFile);
            Assert.That(checkFileSerial, Is.True, $"{objStorage.Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {DownloadedFolderPath}");
            FileContentHelper.CheckSerialEncFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetSerialEncFile));

            //Verify Serial.Enc file content
            FileContentHelper.CheckSerialEncFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetSerialEncFile));

            //// No ENC Available
            Assert.That(Directory.Exists(Path.Combine(DownloadedFolderPath, "ENC_ROOT\\AB")),Is.False);

            //// INFO >> PRODUCT.TXT
            bool checkFileProductTxt = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetProductFilePath), objStorage.Config.ExchangeSetProductFile);
            Assert.That(checkFileProductTxt, Is.True, $"{objStorage.Config.ExchangeSetProductFile} File not Exist in the specified folder path : {DownloadedFolderPath}");

            var apiScsResponseProductTxt = await ScsApiClient.GetScsCatalogueAsync(objStorage.Config.ExchangeSetProductType, objStorage.Config.ExchangeSetCatalogueType, objStorage.ScsJwtToken);
            Assert.That((int)apiScsResponseProductTxt.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiScsResponseProductTxt.StatusCode}, instead of the expected status 200.");
            var apiResponseDetails = await apiScsResponseProductTxt.ReadAsStringAsync();
            dynamic apiScsResponseDataProductTXT = JsonConvert.DeserializeObject(apiResponseDetails);

            FileContentHelper.CheckProductFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath, Config.ExchangeSetProductFile), apiScsResponseDataProductTXT);
        }

        [TearDown]
        public void GlobalTeardown()
        {
            ////Clean up downloaded files/ folders
            FileContentHelper.DeleteDirectory(objStorage.Config.ExchangeSetFileName);
        }
    }
}
