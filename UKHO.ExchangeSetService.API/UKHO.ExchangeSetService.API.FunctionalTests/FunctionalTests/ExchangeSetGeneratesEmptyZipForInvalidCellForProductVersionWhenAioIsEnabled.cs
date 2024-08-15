using Newtonsoft.Json;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    [TestFixture]
    class ExchangeSetGeneratesEmptyZipForInvalidCellForProductVersionWhenAioIsEnabled : ObjectStorage
    {
        public ObjectStorage objStorage = new();

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            ProductVersionData.Add(DataHelper.GetProductVersionModelData(Config.AIOConfig.EncCellName, Config.AIOConfig.EncEditionNumber, Config.AIOConfig.EncUpdateNumber));
            ProductVersionData.Add(DataHelper.GetProductVersionModelData(Config.AIOConfig.AioCellName, Config.AIOConfig.AioEditionNumber, Config.AIOConfig.AioUpdateNumber));
            ApiEssResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: objStorage.EssJwtToken);
            EncDownloadedFolderPath = await FileContentHelper.CreateExchangeSetFile(ApiEssResponse, objStorage.FssJwtToken);
            AioDownloadedFolderPath = await FileContentHelper.DownloadAndExtractAioZip(ApiEssResponse, objStorage.FssJwtToken);
        }

        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task VerifyEmptyEncExchangeSetForProductVersion()
        {
            //// ENC_ROOT >>> ReadmeTxtFile
            bool checkFileReadme = FssBatchHelper.CheckforFileExist(Path.Combine(EncDownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder), objStorage.Config.ExchangeReadMeFile);
            Assert.IsTrue(checkFileReadme, $"{objStorage.Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(EncDownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder)}");
            FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(EncDownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder, objStorage.Config.ExchangeReadMeFile));

            //// ENC_ROOT >> Catalog.031
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(EncDownloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeSetCatalogueFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(EncDownloadedFolderPath, Config.ExchangeSetCatalogueFile)}");

            //Verify Catalog file content
            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, DataHelper.GetProductIdentifiers(), ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            FileContentHelper.CheckCatalogueFileContent(Path.Combine(EncDownloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeSetCatalogueFile), apiScsResponseData);

            ////SERIAL.ENC 
            bool checkFileSerial = FssBatchHelper.CheckforFileExist(EncDownloadedFolderPath, objStorage.Config.ExchangeSetSerialEncFile);
            Assert.IsTrue(checkFileSerial, $"{objStorage.Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {EncDownloadedFolderPath}");
            FileContentHelper.CheckSerialEncFileContent(Path.Combine(EncDownloadedFolderPath, Config.ExchangeSetSerialEncFile));

            //Verify Serial.Enc file content
            FileContentHelper.CheckSerialEncFileContent(Path.Combine(EncDownloadedFolderPath, Config.ExchangeSetSerialEncFile));

            //// No ENC Available
            Assert.IsFalse(Directory.Exists(Path.Combine(EncDownloadedFolderPath, "ENC_ROOT\\AB")));

            //// INFO >> PRODUCT.TXT
            bool checkFileProductTxt = FssBatchHelper.CheckforFileExist(Path.Combine(EncDownloadedFolderPath, objStorage.Config.ExchangeSetProductFilePath), objStorage.Config.ExchangeSetProductFile);
            Assert.IsTrue(checkFileProductTxt, $"{objStorage.Config.ExchangeSetProductFile} File not Exist in the specified folder path : {EncDownloadedFolderPath}");

            var apiScsResponseProductTxt = await ScsApiClient.GetScsCatalogueAsync(objStorage.Config.ExchangeSetProductType, objStorage.Config.ExchangeSetCatalogueType, objStorage.ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponseProductTxt.StatusCode, $"Incorrect status code is returned {apiScsResponseProductTxt.StatusCode}, instead of the expected status 200.");
            var apiResponseDetails = await apiScsResponseProductTxt.ReadAsStringAsync();
            dynamic apiScsResponseDataProductTXT = JsonConvert.DeserializeObject(apiResponseDetails);

            FileContentHelper.CheckProductFileContent(Path.Combine(EncDownloadedFolderPath, Config.ExchangeSetProductFilePath, Config.ExchangeSetProductFile), apiScsResponseDataProductTXT);
        }

        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task VerifyEmptyAioExchangeSetForProductVersion()
        {
            //Verify Catalog file content
            var apiScsResponse = await ScsApiClient.GetProductVersionsAsync(objStorage.Config.ExchangeSetProductType, ProductVersionData, objStorage.ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 304.");

            //// ENC_ROOT >>> ReadmeTxtFile
            bool checkFileReadme = FssBatchHelper.CheckforFileExist(Path.Combine(AioDownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder), objStorage.Config.ExchangeReadMeFile);
            Assert.IsTrue(checkFileReadme, $"{objStorage.Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(AioDownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder)}");
            FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(AioDownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder, objStorage.Config.ExchangeReadMeFile));

            //// ENC_ROOT >> Catalog.031
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(AioDownloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeSetCatalogueFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(AioDownloadedFolderPath, Config.ExchangeSetCatalogueFile)}");

            ////SERIAL.AIO 
            bool checkFileSerial = FssBatchHelper.CheckforFileExist(AioDownloadedFolderPath, objStorage.Config.AIOConfig.ExchangeSetSerialAioFile);
            Assert.IsTrue(checkFileSerial, $"{objStorage.Config.AIOConfig.ExchangeSetSerialAioFile} File not Exist in the specified folder path : {AioDownloadedFolderPath}");
            FileContentHelper.CheckSerialAioFileContentForAioUpdate(Path.Combine(AioDownloadedFolderPath, objStorage.Config.AIOConfig.ExchangeSetSerialAioFile));

            //// No ENC Available
            Assert.IsFalse(Directory.Exists(Path.Combine(AioDownloadedFolderPath, "ENC_ROOT\\GB")));

            //// INFO >> PRODUCT.TXT
            bool checkFileProductTxt = FssBatchHelper.CheckforFileExist(Path.Combine(AioDownloadedFolderPath, objStorage.Config.ExchangeSetProductFilePath), objStorage.Config.ExchangeSetProductFile);
            Assert.IsTrue(checkFileProductTxt, $"{objStorage.Config.ExchangeSetProductFile} File not Exist in the specified folder path : {AioDownloadedFolderPath}");

            var apiScsResponseProductTxt = await ScsApiClient.GetScsCatalogueAsync(objStorage.Config.ExchangeSetProductType, objStorage.Config.ExchangeSetCatalogueType, objStorage.ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponseProductTxt.StatusCode, $"Incorrect status code is returned {apiScsResponseProductTxt.StatusCode}, instead of the expected status 200.");
            var apiResponseDetails = await apiScsResponseProductTxt.ReadAsStringAsync();
            dynamic apiScsResponseDataProductTXT = JsonConvert.DeserializeObject(apiResponseDetails);

            FileContentHelper.CheckAioProductFileContent(Path.Combine(AioDownloadedFolderPath, objStorage.Config.ExchangeSetProductFilePath, objStorage.Config.ExchangeSetProductFile), apiScsResponseDataProductTXT);
        }

        [OneTimeTearDown]
        public void GlobalTeardown()
        {
            ////Clean up downloaded files/ folders
            FileContentHelper.DeleteDirectory(objStorage.Config.ExchangeSetFileName);
            FileContentHelper.DeleteDirectory(objStorage.Config.AIOConfig.AioExchangeSetFileName);
        }
    }
}