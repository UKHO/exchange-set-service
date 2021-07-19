using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class ExchangeSetValidateProductIdentifier
    {
        private string EssJwtToken { get; set; }
        private string FssJwtToken { get; set; }
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private FssApiClient FssApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper DataHelper { get; set; }
        public ProductIdentifierModel ProductIdentifierModel { get; set; }
        private SalesCatalogueApiClient ScsApiClient { get; set; }
        private string ScsJwtToken { get; set; }
        private string DownloadedFolderPath { get; set; }

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductIdentifierModel = new ProductIdentifierModel();
            FssApiClient = new FssApiClient();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssJwtToken = await authTokenProvider.GetFssToken();
            DataHelper = new DataHelper();
            ScsApiClient = new SalesCatalogueApiClient(Config.ScsAuthConfig.ScsApiUrl);
            ScsJwtToken = await authTokenProvider.GetScsToken();
            DownloadedFolderPath = await ValidateGeneratedFile();
        }

        public async Task<string> ValidateGeneratedFile()
        {
            var apiEssResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(new List<string>() { "DE416080", "GB416080" }, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiEssResponse.StatusCode, $"Incorrect status code is returned {apiEssResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseData = await apiEssResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseData.Links.ExchangeSetBatchStatusUri.Href;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);
            Assert.AreEqual("Commited", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseData.Links.ExchangeSetFileUri.Href;

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);

            var downloadFolder = FssBatchHelper.RenameFolder(extractDownloadedFolder);
            var downloadFolderPath = Path.Combine(Path.GetTempPath(), downloadFolder);

            return downloadFolderPath;
        }

        [Test]
        public async Task WhenICallExchangeSetApiWithAValidProductIdentifiers_ThenAProductTxtFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath), Config.ExchangeSetProductFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath)}");

            //Verify Product.txt file content
            var apiScsResponse = await ScsApiClient.GetScsCatalogueAsync(Config.ExchangeSetProductType, Config.ExchangeSetCatalogueType, ScsJwtToken);
            var apiResponseDetails = await apiScsResponse.ReadAsStringAsync();
            dynamic apiScsResponseData = JsonConvert.DeserializeObject(apiResponseDetails);

            FileContentHelper.CheckProductFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath, Config.ExchangeSetProductFile), apiScsResponseData, ScsJwtToken);

        }

        [Test]
        public void WhenICallExchangeSetApiWithValidProductIdentifiers_ThenAReadMeTxtFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(DownloadedFolderPath, Config.ExchangeReadMeFile);
            Assert.IsTrue(checkFile, $"{Config.ExchangeReadMeFile} File not Exist in the specified folder path : {DownloadedFolderPath}");

            //Verify README.TXT file content
            FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeReadMeFile));
        }

        [Test]
        public async Task WhenICallExchangeSetApiWithAValidProductIdentifiers_ThenACatalogueFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeSetCatalogueFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetCatalogueFile)}");
           
            //Verify Catalog file content
            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, new List<string>() { "DE416080", "GB416080" }, ScsJwtToken);
            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            FileContentHelper.CheckCatalogueFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeSetCatalogueFile), apiScsResponseData, ScsJwtToken);

        }

        [Test]
        public void WhenICallExchangeSetApiWithAValidProductIdentifiers_ThenASerialEncFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(DownloadedFolderPath, Config.ExchangeSetSerialEncFile);
            Assert.IsTrue(checkFile, $"{Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {DownloadedFolderPath}");

            //Verify Serial.Enc file content
            FileContentHelper.CheckSerialEncFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetSerialEncFile));

        }

        [Test]
        public async Task WhenICallExchangeSetApiWithAvalidValidMultipleProductIdentifiers_ThenEncFilesAreDownloadedd()
        {
            //Get the product details form sales catalogue service

            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, new List<string> { "DE416080", "GB416080" }, ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            foreach (var product in apiScsResponseData.Products)
            {
                string productName = product.ProductName;
                int editionNumber = product.EditionNumber;

                //Enc file downloaded verification
                foreach (var updateNumber in product.UpdateNumbers)
                {
                    await FileContentHelper.GetDownloadedEncFilesAsync(Config.FssConfig.FssApiUrl, Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), productName, editionNumber, updateNumber, FssJwtToken);

                }

            }
        }

        [Test]
        public void WhenICallExchangeSetApiWithAnInvalidProductIdentifier_ThenNoEncFileDownloaded()
        {
            //Verify No folder available for the ENC             
            FileContentHelper.CheckNoEncFilesDownloadedAsync(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), "GB416080");
        }


    }
}
