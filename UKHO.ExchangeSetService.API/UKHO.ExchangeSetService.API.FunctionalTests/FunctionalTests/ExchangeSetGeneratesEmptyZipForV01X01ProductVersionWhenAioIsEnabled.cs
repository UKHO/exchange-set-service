using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    [TestFixture]
    class ExchangeSetGeneratesEmptyZipForV01X01ProductVersionWhenAioIsEnabled : ObjectStorage
    {
        public ObjectStorage objStorage = new();

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            ProductVersionData.Add(DataHelper.GetProductVersionModelData(Config.AIOConfig.EncCellName, Config.AIOConfig.EncEditionNumber, Config.AIOConfig.EncUpdateNumber));
            ApiEssResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: objStorage.EssJwtToken);
            //Get the BatchId
            batchId = await ApiEssResponse.GetBatchId();
            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFile(ApiEssResponse, objStorage.FssJwtToken);
        }

        [Test]
        public async Task WhenICallExchangeSetApiWithAValidProductVersion_ThenAProductTxtFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetProductFilePath), objStorage.Config.ExchangeSetProductFile);
            Assert.That(checkFile,Is.True, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetProductFilePath)}");

            //Verify Product.txt file content
            var apiScsResponse = await ScsApiClient.GetScsCatalogueAsync(Config.ExchangeSetProductType, objStorage.Config.ExchangeSetCatalogueType, objStorage.ScsJwtToken);
            Assert.That((int)apiScsResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseDetails = await apiScsResponse.ReadAsStringAsync();
            dynamic apiScsResponseData = JsonConvert.DeserializeObject(apiResponseDetails);

            FileContentHelper.CheckProductFileContent(Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetProductFilePath, objStorage.Config.ExchangeSetProductFile), apiScsResponseData);
        }

        [Test]
        public void WhenICallExchangeSetApiWithAValidProductVersion_ThenAReadMeTxtFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder), objStorage.Config.ExchangeReadMeFile);
            Assert.That(checkFile, Is.True, $"{objStorage.Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder)}");

            //Verify README.TXT file content
            FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder, objStorage.Config.ExchangeReadMeFile));
        }

        [Test]
        public void WhenICallExchangeSetApiWithAValidProductVersion_ThenACatalogueFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder), objStorage.Config.ExchangeSetCatalogueFile);
            Assert.That(checkFile, Is.True, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder)}");
        }

        [Test]
        public void WhenICallExchangeSetApiWithAValidProductVersion_ThenASerialEncFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(DownloadedFolderPath, objStorage.Config.ExchangeSetSerialEncFile);
            Assert.That(checkFile, Is.True, $"{objStorage.Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {DownloadedFolderPath}");

            //Verify Serial.Enc file content
            FileContentHelper.CheckSerialEncFileContent(Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetSerialEncFile));
        }

        [Test]
        public async Task WhenICallExchangeSetApiWithAValidProductVersion_ThenEncFilesShouldNotBeDownloaded()
        {
            //Get the product details form sales catalogue service
            var notModifiedProductVersion = new List<ProductVersionModel>()
            {
                DataHelper.GetProductVersionModelData(Config.AIOConfig.NotModifiedCellName,
                    Config.AIOConfig.NotModifiedCellEditionNumber, Config.AIOConfig.NotModifiedCellUpdateNumber)
            };
            var apiScsResponse = await ScsApiClient.GetProductVersionsAsync(objStorage.Config.ExchangeSetProductType, notModifiedProductVersion, objStorage.ScsJwtToken);
            Assert.That((int)apiScsResponse.StatusCode, Is.EqualTo(304), $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 304.");

            Assert.That(Directory.Exists(Path.Combine(DownloadedFolderPath, "ENC_ROOT\\DE")),Is.False);
        }

        [Test]
        public void WhenICallEssWithAioProductAndAioIsEnabled_ThenAioZipShouldNotBeAvailable()
        {
            var downloadedFilename = DownloadedFolderPath.Split("\\").LastOrDefault();
            Assert.That(downloadedFilename, Is.Not.EqualTo(Config.ExchangeSetFileName), $"Incorrect file {objStorage.Config.AIOConfig.AioExchangeSetFileName} downloaded");
        }

        [OneTimeTearDown]
        public void GlobalTeardown()
        {
            //Clean up downloaded files/folders   
            FileContentHelper.DeleteDirectory(objStorage.Config.ExchangeSetFileName);
        }
    }
}
