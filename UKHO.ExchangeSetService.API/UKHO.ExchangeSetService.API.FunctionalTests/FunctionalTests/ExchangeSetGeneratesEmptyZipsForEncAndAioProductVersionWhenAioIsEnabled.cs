using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    [TestFixture]
    class ExchangeSetGeneratesEmptyZipsForEncAndAioProductVersionWhenAioIsEnabled : ObjectStorage
    {
        public ObjectStorage objStorage = new();

        //Product Backlog Item 77585: ESS : Empty AIO Exchange Set Creation
        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            ProductVersionData.Add(DataHelper.GetProductVersionModelData(Config.AIOConfig.EncCellName, Config.AIOConfig.EncEditionNumber, Config.AIOConfig.EncUpdateNumber));
            ProductVersionData.Add(DataHelper.GetProductVersionModelData(Config.AIOConfig.AioCellName, Config.AIOConfig.AioEditionNumber, Config.AIOConfig.AioUpdateNumber));
            ApiEssResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: objStorage.EssJwtToken);
            Console.WriteLine("ExchangeSetGeneratesEmptyZipsForEncAndAioProductVersionWhenAioIsEnabled ESSEndPoint status ==> " + ApiEssResponse.StatusCode);
            EncDownloadedFolderPath = await FileContentHelper.CreateExchangeSetFile(ApiEssResponse, objStorage.FssJwtToken);
            AioDownloadedFolderPath = await FileContentHelper.DownloadAndExtractAioZip(ApiEssResponse, objStorage.FssJwtToken);
        }

        //Product Backlog Item 71610: Create empty SERIAL.AIO file and add to AIO exchange set
        [Test]
        [Category("QCOnlyTest-AIOEnabled")][Category("SmokeTemp")]
        public void WhenIDownloadAioZipExchangeSet_ThenASerialAioFileIsAvailable()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(AioDownloadedFolderPath, objStorage.Config.AIOConfig.ExchangeSetSerialAioFile);
            Assert.IsTrue(checkFile, $"{objStorage.Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {AioDownloadedFolderPath}");

            //Product Backlog Item 71612: Add content to SERIAL.AIO file
            //Verify Serial.AIO file content
            FileContentHelper.CheckSerialAioFileContentForAioUpdate(Path.Combine(AioDownloadedFolderPath, objStorage.Config.AIOConfig.ExchangeSetSerialAioFile));
        }

        [Test]
        [Category("QCOnlyTest-AIOEnabled")][Category("SmokeTemp")]
        public void WhenIDownloadV01X01ZipExchangeSet_ThenASerialEncFileIsAvailable()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(EncDownloadedFolderPath, objStorage.Config.ExchangeSetSerialEncFile);
            Assert.IsTrue(checkFile, $"{objStorage.Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {EncDownloadedFolderPath}");

            //Verify Serial.ENC file content
            FileContentHelper.CheckSerialEncFileContent(Path.Combine(EncDownloadedFolderPath, objStorage.Config.ExchangeSetSerialEncFile));
        }

        //Product Backlog Item 71993: Get README.TXT from FSS & add to AIO exchange set
        [Test]
        [Category("QCOnlyTest-AIOEnabled")][Category("SmokeTemp")]
        public void WhenIDownloadAioZipExchangeSet_ThenAReadmeTxtFileIsAvailableAsync()
        {

            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(AioDownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder), objStorage.Config.ExchangeReadMeFile);
            Assert.IsTrue(checkFile, $"{objStorage.Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(AioDownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder)}");

            //Verify README.TXT file content
            FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(AioDownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder, objStorage.Config.ExchangeReadMeFile));
        }

        [Test]
        [Category("QCOnlyTest-AIOEnabled")][Category("SmokeTemp")]
        public void WhenIDownloadV01X01ZipExchangeSet_ThenAReadmeTxtFileIsAvailableAsync()
        {

            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(EncDownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder), objStorage.Config.ExchangeReadMeFile);
            Assert.IsTrue(checkFile, $"{objStorage.Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(EncDownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder)}");

            //Verify README.TXT file content
            FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(EncDownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder, objStorage.Config.ExchangeReadMeFile));
        }

        //Product Backlog Item 77585: ESS : Empty AIO Exchange Set Creation
        [Test]
        [Category("QCOnlyTest-AIOEnabled")][Category("SmokeTemp")]
        public async Task WhenIDownloadAioZipExchangeSet_ThenEncFilesShouldNotBeAvailable()
        {
            //Get the product details form sales catalogue service
            var apiScsResponse = await ScsApiClient.GetProductVersionsAsync(objStorage.Config.ExchangeSetProductType, ProductVersionData, objStorage.ScsJwtToken);
            Assert.AreEqual(304, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 304.");

            Assert.IsFalse(Directory.Exists(Path.Combine(AioDownloadedFolderPath, "ENC_ROOT\\GB")));
        }

        //Product Backlog Item 77585: ESS : Empty AIO Exchange Set Creation
        [Test]
        [Category("QCOnlyTest-AIOEnabled")][Category("SmokeTemp")]
        public async Task WhenIDownloadV01X01ZipExchangeSet_ThenEncFilesShouldNotBeAvailable()
        {
            //Get the product details form sales catalogue service
            var apiScsResponse = await ScsApiClient.GetProductVersionsAsync(objStorage.Config.ExchangeSetProductType, ProductVersionData, objStorage.ScsJwtToken);
            Assert.AreEqual(304, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 304.");

            Assert.IsFalse(Directory.Exists(Path.Combine(EncDownloadedFolderPath, "ENC_ROOT\\DE")));
        }

        //Product Backlog Item 72017: Create empty PRODUCTS.TXT file & add to AIO exchange set
        [Test]
        [Category("QCOnlyTest-AIOEnabled")][Category("SmokeTemp")]
        public async Task WhenIDownloadAioZipExchangeSet_ThenAProductTxtFileIsAvailable()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(AioDownloadedFolderPath, objStorage.Config.ExchangeSetProductFilePath), objStorage.Config.ExchangeSetProductFile);
            Assert.IsTrue(checkFile, $"{objStorage.Config.ExchangeSetProductFile} File not Exist in the specified folder path : {AioDownloadedFolderPath}");

            //Product Backlog Item 72019: Add content to PRODUCTS.TXT file
            //Verify Product.txt file content
            var apiScsResponse = await ScsApiClient.GetScsCatalogueAsync(objStorage.Config.ExchangeSetProductType, objStorage.Config.ExchangeSetCatalogueType, objStorage.ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");
            var apiResponseDetails = await apiScsResponse.ReadAsStringAsync();
            dynamic apiScsResponseData = JsonConvert.DeserializeObject(apiResponseDetails);

            FileContentHelper.CheckAioProductFileContent(Path.Combine(AioDownloadedFolderPath, objStorage.Config.ExchangeSetProductFilePath, objStorage.Config.ExchangeSetProductFile), apiScsResponseData);
        }

        [Test]
        [Category("QCOnlyTest-AIOEnabled")][Category("SmokeTemp")]
        public async Task WhenIDownloadV01X01ZipExchangeSet_ThenAProductTxtFileIsAvailable()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(EncDownloadedFolderPath, objStorage.Config.ExchangeSetProductFilePath), objStorage.Config.ExchangeSetProductFile);
            Assert.IsTrue(checkFile, $"{objStorage.Config.ExchangeSetProductFile} File not Exist in the specified folder path : {EncDownloadedFolderPath}");

            //Product Backlog Item 72019: Add content to PRODUCTS.TXT file
            //Verify Product.txt file content
            var apiScsResponse = await ScsApiClient.GetScsCatalogueAsync(objStorage.Config.ExchangeSetProductType, objStorage.Config.ExchangeSetCatalogueType, objStorage.ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");
            var apiResponseDetails = await apiScsResponse.ReadAsStringAsync();
            dynamic apiScsResponseData = JsonConvert.DeserializeObject(apiResponseDetails);

            FileContentHelper.CheckProductFileContent(Path.Combine(EncDownloadedFolderPath, objStorage.Config.ExchangeSetProductFilePath, objStorage.Config.ExchangeSetProductFile), apiScsResponseData);
        }

        //Product Backlog Item 71646: Create CATALOG.031 file and add to AIO exchange set
        [Test]
        [Category("QCOnlyTest-AIOEnabled")][Category("SmokeTemp")]
        public async Task WhenIDownloadAioZipExchangeSet_ThenCatalog031IsAvailable()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(AioDownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder), objStorage.Config.ExchangeSetCatalogueFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(AioDownloadedFolderPath, objStorage.Config.ExchangeSetCatalogueFile)}");

            //Product Backlog Item 71658: Add content to CATALOG.031 file
            //Verify Catalog file content
            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(objStorage.Config.ExchangeSetProductType, DataHelper.GetProductIdentifiersForAioOnly(), objStorage.ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            FileContentHelper.CheckCatalogueFileContent(Path.Combine(AioDownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder, objStorage.Config.ExchangeSetCatalogueFile), apiScsResponseData);
        }

        [Test]
        [Category("QCOnlyTest-AIOEnabled")][Category("SmokeTemp")]
        public async Task WhenIDownloadV01X01ZipExchangeSet_ThenCatalog031IsAvailable()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(EncDownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder), objStorage.Config.ExchangeSetCatalogueFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(EncDownloadedFolderPath, objStorage.Config.ExchangeSetCatalogueFile)}");

            //Product Backlog Item 71658: Add content to CATALOG.031 file
            //Verify Catalog file content
            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(objStorage.Config.ExchangeSetProductType, DataHelper.GetProductIdentifiersForAioOnly(), objStorage.ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            FileContentHelper.CheckCatalogueFileContent(Path.Combine(EncDownloadedFolderPath, objStorage.Config.ExchangeSetEncRootFolder, objStorage.Config.ExchangeSetCatalogueFile), apiScsResponseData);
        }


        [OneTimeTearDown]
        public void GlobalTeardown()
        {
            //Clean up downloaded files/folders   
            FileContentHelper.DeleteDirectory(objStorage.Config.AIOConfig.AioExchangeSetFileName);
            FileContentHelper.DeleteDirectory(objStorage.Config.ExchangeSetFileName);
        }
    }
}