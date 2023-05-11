using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    [TestFixture]
    class ExchangeSetGeneratesEmptyZipForAioProductVersionWhenAioIsEnabled : ObjectStorage
    {

        //Product Backlog Item 76440: ESS : Creation of AIO.zip and uploading to FSS with ENC Exchange Set
        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssJwtToken = await authTokenProvider.GetFssToken();
            ScsApiClient = new SalesCatalogueApiClient(Config.ScsAuthConfig.BaseUrl);
            ScsJwtToken = await authTokenProvider.GetScsToken();
            FssApiClient = new FssApiClient();
            DataHelper = new DataHelper();
            ProductVersionData = new List<ProductVersionModel>();
            ProductVersionData.Add(DataHelper.GetProductVersionModelData("GB800001", 31, 34));
            ApiEssResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            //Get the BatchId
            batchId = await ApiEssResponse.GetBatchId();
            DownloadedFolderPath = await FileContentHelper.DownloadAndExtractAioZip(ApiEssResponse, FssJwtToken);
        }

        //Product Backlog Item 77585: ESS : Empty AIO Exchange Set Creation
        [Test]
        [Category("SmokeTest-AIOEnabled")]
        public void WhenIDownloadAioZipExchangeSet_ThenASerialAioFileIsAvailable()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(DownloadedFolderPath, Config.AIOConfig.ExchangeSetSerialAioFile);
            Assert.IsTrue(checkFile, $"{Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {DownloadedFolderPath}");

            //Product Backlog Item 71612: Add content to SERIAL.AIO file
            //Verify Serial.AIO file content
            FileContentHelper.CheckSerialAioFileContentForAioUpdate(Path.Combine(DownloadedFolderPath, Config.AIOConfig.ExchangeSetSerialAioFile));
        }

        //Product Backlog Item 71993: Get README.TXT from FSS & add to AIO exchange set
        [Test]
        [Category("SmokeTest-AIOEnabled")]
        public void WhenIDownloadAioZipExchangeSet_ThenAReadmeTxtFileIsAvailableAsync()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeReadMeFile);
            Assert.IsTrue(checkFile, $"{Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder)}");

            //Verify README.TXT file content
            FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeReadMeFile));
        }

        //Product Backlog Item 77585: ESS : Empty AIO Exchange Set Creation
        [Test]
        [Category("SmokeTest-AIOEnabled")]
        public async Task WhenIDownloadAioZipExchangeSet_ThenEncFilesShouldNotBeAvailable()
        {
            //Get the product details form sales catalogue service
            var apiScsResponse = await ScsApiClient.GetProductVersionsAsync(Config.ExchangeSetProductType, ProductVersionData, ScsJwtToken);
            Assert.AreEqual(304, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 304.");

            Assert.IsFalse(Directory.Exists(Path.Combine(DownloadedFolderPath, "ENC_ROOT\\GB")));
        }

        //Product Backlog Item 72017: Create empty PRODUCTS.TXT file & add to AIO exchange set
        [Test]
        [Category("SmokeTest-AIOEnabled")]
        public async Task WhenIDownloadAioZipExchangeSet_ThenAProductTxtFileIsAvailable()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath), Config.ExchangeSetProductFile);
            Assert.IsTrue(checkFile, $"{Config.ExchangeSetProductFile} File not Exist in the specified folder path : {DownloadedFolderPath}");

            //Product Backlog Item 72019: Add content to PRODUCTS.TXT file
            //Verify Product.txt file content
            var apiScsResponse = await ScsApiClient.GetScsCatalogueAsync(Config.ExchangeSetProductType, Config.ExchangeSetCatalogueType, ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");
            var apiResponseDetails = await apiScsResponse.ReadAsStringAsync();
            dynamic apiScsResponseData = JsonConvert.DeserializeObject(apiResponseDetails);

            FileContentHelper.CheckAioProductFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath, Config.ExchangeSetProductFile), apiScsResponseData);
        }

        //Product Backlog Item 71646: Create CATALOG.031 file and add to AIO exchange set
        [Test]
        [Category("SmokeTest-AIOEnabled")]
        public void WhenIDownloadAioZipExchangeSet_ThenCatalog031IsAvailable()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeSetCatalogueFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetCatalogueFile)}");
        }

        [Test]
        [Category("SmokeTest-AIOEnabled")]
        public async Task WhenICallEssWithAioProductAndAioIsEnabled_ThenV01X01ZipShouldNotBeAvailable()
        {
            string downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{Config.ExchangeSetFileName}";

            var response = await FssApiClient.GetFileDownloadAsync(downloadFileUrl, accessToken: FssJwtToken);
            Assert.AreEqual(404, (int)response.StatusCode, $"Incorrect status code File Download api returned {response.StatusCode} for the url {downloadFileUrl}, instead of the expected 404.");
        }

        [OneTimeTearDown]
        public void GlobalTeardown()
        {
            //Clean up downloaded files/folders   
            FileContentHelper.DeleteDirectory(Config.AIOConfig.AioExchangeSetFileName);
        }
    }
}