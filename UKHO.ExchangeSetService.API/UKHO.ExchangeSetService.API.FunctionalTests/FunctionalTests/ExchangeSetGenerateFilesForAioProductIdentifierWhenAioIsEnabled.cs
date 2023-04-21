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
    class ExchangeSetGenerateFilesForAioProductIdentifierWhenAioIsEnabled
    {
        private string EssJwtToken { get; set; }
        private string FssJwtToken { get; set; }
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper DataHelper { get; set; }
        private HttpResponseMessage ApiEssResponse { get; set; }
        private string DownloadedFolderPath;
        private SalesCatalogueApiClient ScsApiClient { get; set; }
        private string ScsJwtToken { get; set; }
        private FssApiClient FssApiClient { get; set; }
        private readonly List<string> LargeExchangeSetFolderName = new List<string>();
        private string batchId;

        //Product Backlog Item 76440: ESS : Creation of AIO.zip and uploading to FSS with ENC Exchange Set 
        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            await Task.Delay(30000);
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
            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, DataHelper.GetProductIdentifiersForLargeMediaAndAio(), ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            foreach (var product in apiScsResponseData.Products)
            {
                string productName = product.ProductName;
                int editionNumber = product.EditionNumber;

                if(productName.Equals("GB800001"))
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
        public void WhenIDownloadAioZipExchangeSet_ThenCatalog031IsAvailable()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeSetCatalogueFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetCatalogueFile)}");
        }

        [Test]
        [Category("SmokeTest-AIOEnabled")]
        public async Task WhenICallEssWithAioProductAndAioIsEnabled_ThenLargeMediaZipsShouldNotBeAvailable()
        {
            LargeExchangeSetFolderName.Add(Config.POSConfig.LargeExchangeSetFolderName1+".zip");
            LargeExchangeSetFolderName.Add(Config.POSConfig.LargeExchangeSetFolderName2 + ".zip");

            foreach(string folderName in LargeExchangeSetFolderName)
            {
                string downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{folderName}";

                var response = await FssApiClient.GetFileDownloadAsync(downloadFileUrl, accessToken: FssJwtToken);
                Assert.AreEqual(404, (int)response.StatusCode, $"Incorrect status code File Download api returned {response.StatusCode} for the url {downloadFileUrl}, instead of the expected 404.");
            }
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