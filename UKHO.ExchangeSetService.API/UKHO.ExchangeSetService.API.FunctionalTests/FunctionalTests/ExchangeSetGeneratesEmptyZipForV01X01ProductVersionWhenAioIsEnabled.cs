using Newtonsoft.Json;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;

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
        [Category("QCOnlyTest-AIOEnabled")][Category("Temp")]
        public async Task WhenICallExchangeSetApiWithAValidProductVersion_ThenAProductTxtFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetProductFilePath), objStorage.Config.ExchangeSetProductFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetProductFilePath)}");

            //Verify Product.txt file content
            var apiScsResponse = await ScsApiClient.GetScsCatalogueAsync(Config.ExchangeSetProductType, objStorage.Config.ExchangeSetCatalogueType, objStorage.ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseDetails = await apiScsResponse.ReadAsStringAsync();
            dynamic apiScsResponseData = JsonConvert.DeserializeObject(apiResponseDetails);

            FileContentHelper.CheckProductFileContent(Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetProductFilePath, objStorage.Config.ExchangeSetProductFile), apiScsResponseData);
        }

        [Test]
        [Category("QCOnlyTest-AIOEnabled")][Category("Temp")]
        public void WhenICallExchangeSetApiWithAValidProductVersion_ThenAReadMeTxtFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder), objStorage.Config.ExchangeReadMeFile);
            Assert.IsTrue(checkFile, $"{objStorage.Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder)}");

            //Verify README.TXT file content
            FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder, objStorage.Config.ExchangeReadMeFile));
        }

        [Test]
        [Category("QCOnlyTest-AIOEnabled")][Category("Temp")]
        public void WhenICallExchangeSetApiWithAValidProductVersion_ThenACatalogueFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder), objStorage.Config.ExchangeSetCatalogueFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder)}");
        }

        [Test]
        [Category("QCOnlyTest-AIOEnabled")][Category("Temp")]
        public void WhenICallExchangeSetApiWithAValidProductVersion_ThenASerialEncFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(DownloadedFolderPath, objStorage.Config.ExchangeSetSerialEncFile);
            Assert.IsTrue(checkFile, $"{objStorage.Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {DownloadedFolderPath}");

            //Verify Serial.Enc file content
            FileContentHelper.CheckSerialEncFileContent(Path.Combine(DownloadedFolderPath, objStorage.Config.ExchangeSetSerialEncFile));
        }

        [Test]
        [Category("QCOnlyTest-AIOEnabled")][Category("Temp")]
        public async Task WhenICallExchangeSetApiWithAValidProductVersion_ThenEncFilesShouldNotBeDownloaded()
        {
            //Get the product details form sales catalogue service
            var apiScsResponse = await ScsApiClient.GetProductVersionsAsync(objStorage.Config.ExchangeSetProductType, ProductVersionData, objStorage.ScsJwtToken);
            Assert.AreEqual(304, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 304.");

            Assert.IsFalse(Directory.Exists(Path.Combine(DownloadedFolderPath, "ENC_ROOT\\DE")));
        }

        [Test]
        [Category("QCOnlyTest-AIOEnabled")][Category("Temp")]
        public async Task WhenICallEssWithAioProductAndAioIsEnabled_ThenAioZipShouldNotBeAvailable()
        {
            string downloadFileUrl = $"{objStorage.Config.FssConfig.BaseUrl}/batch/{batchId}/files/{objStorage.Config.AIOConfig.AioExchangeSetFileName}";

            var response = await FssApiClient.GetFileDownloadAsync(downloadFileUrl, accessToken: objStorage.FssJwtToken);
            Assert.AreEqual(404, (int)response.StatusCode, $"Incorrect status code File Download api returned {response.StatusCode} for the url {downloadFileUrl}, instead of the expected 404.");
        }

        [OneTimeTearDown]
        public void GlobalTeardown()
        {
            //Clean up downloaded files/folders   
            FileContentHelper.DeleteDirectory(objStorage.Config.ExchangeSetFileName);
        }
    }
}