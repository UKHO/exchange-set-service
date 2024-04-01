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
    class ExchangeSetGenerateFilesForAioProductIdentifierWhenAioIsEnabled : ObjectStorage
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
            ApiEssResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifiersForAioOnly(), accessToken: EssJwtToken);
            //Get the BatchId
            batchId = await ApiEssResponse.GetBatchId();
            DownloadedFolderPath = await FileContentHelper.DownloadAndExtractAioZip(ApiEssResponse, FssJwtToken);
        }

        //Product Backlog Item 71610: Create empty SERIAL.AIO file and add to AIO exchange set
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public void WhenIDownloadAioZipExchangeSet_ThenASerialAioFileIsAvailable()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(DownloadedFolderPath, Config.AIOConfig.ExchangeSetSerialAioFile);
            Assert.IsTrue(checkFile, $"{Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {DownloadedFolderPath}");

            //Product Backlog Item 71612: Add content to SERIAL.AIO file
            //Verify Serial.AIO file content
            FileContentHelper.CheckSerialAioFileContentForAioBase(Path.Combine(DownloadedFolderPath, Config.AIOConfig.ExchangeSetSerialAioFile));
        }

        //Product Backlog Item 71993: Get README.TXT from FSS & add to AIO exchange set
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public void WhenIDownloadAioZipExchangeSet_ThenAReadmeTxtFileIsAvailableAsync()
        {

            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeReadMeFile);
            Assert.IsTrue(checkFile, $"{Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder)}");

            //Verify README.TXT file content
            FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeReadMeFile));
        }

        //Product Backlog Item 74322: AIO exchange set ENC Data Set files & Signature Files
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenIDownloadAioZipExchangeSet_ThenEncFilesAreAvailable()
        {
            //Get the product details form sales catalogue service
            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, DataHelper.GetProductIdentifiersForAioOnly(), ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            foreach (var product in apiScsResponseData.Products)
            {
                string productName = product.ProductName;
                int editionNumber = product.EditionNumber;

                if (productName.Equals(Config.AIOConfig.AioCellName))
                {
                    //Enc file download verification
                    foreach (var updateNumber in product.UpdateNumbers)
                    {
                        await FileContentHelper.GetDownloadedEncFilesAsync(Config.FssConfig.BaseUrl, Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), productName, editionNumber, updateNumber, FssJwtToken);

                    }
                }
            }
        }

        //Product Backlog Item 72017: Create empty PRODUCTS.TXT file & add to AIO exchange set
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
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
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenIDownloadAioZipExchangeSet_ThenCatalog031IsAvailable()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeSetCatalogueFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetCatalogueFile)}");

            //Product Backlog Item 71658: Add content to CATALOG.031 file
            //Verify Catalog file content
            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, DataHelper.GetProductIdentifiersForAioOnly(), ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            FileContentHelper.CheckCatalogueFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeSetCatalogueFile), apiScsResponseData);

        }

        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public void WhenICallEssWithAioProductAndAioIsEnabled_ThenLargeMediaZipsShouldNotBeAvailable()
        {
            LargeExchangeSetFolderName.Add(Config.POSConfig.LargeExchangeSetFolderName1 + ".zip");
            LargeExchangeSetFolderName.Add(Config.POSConfig.LargeExchangeSetFolderName2 + ".zip");
            var downloadedFilename = DownloadedFolderPath.Split("\\").LastOrDefault();
            foreach (string folderName in LargeExchangeSetFolderName)
            {
                Assert.AreNotEqual(Config.ExchangeSetFileName, downloadedFilename, $"Incorrect file {folderName} downloaded");
            }
        }

        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public void WhenICallEssWithAioProductAndAioIsEnabled_ThenV01X01ZipShouldNotBeAvailable()
        {
            var downloadedFilename = DownloadedFolderPath.Split("\\").LastOrDefault();
            Assert.AreNotEqual(Config.ExchangeSetFileName, downloadedFilename, $"Incorrect file {Config.ExchangeSetFileName} downloaded");
        }


        [OneTimeTearDown]
        public void GlobalTeardown()
        {
            //Clean up downloaded files/folders   
            FileContentHelper.DeleteDirectory(Config.AIOConfig.AioExchangeSetFileName);
        }
    }
}