using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using System.Net.Http;
using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    [Ignore("This FT's issue has been fixed in PBI 74619, so we will remove this ignore tag in that branch.")]
    [TestFixture]
    class ExchangeSetGenerateFilesForInValidProductVersion
    {
        private string EssJwtToken { get; set; }
        private string FssJwtToken { get; set; }
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private FssApiClient FssApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper DataHelper { get; set; }
        private SalesCatalogueApiClient ScsApiClient { get; set; }
        private string ScsJwtToken { get; set; }
        private string DownloadedFolderPath { get; set; }
        private List<ProductVersionModel> ProductVersionData { get; set; }
        private HttpResponseMessage ApiEssResponse { get; set; }
        private readonly List<string> CleanUpBatchIdList = new List<string>();

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            FssApiClient = new FssApiClient();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssJwtToken = await authTokenProvider.GetFssToken();
            DataHelper = new DataHelper();
            ScsApiClient = new SalesCatalogueApiClient(Config.ScsAuthConfig.BaseUrl);
            ScsJwtToken = await authTokenProvider.GetScsToken();
            ProductVersionData = new List<ProductVersionModel>();
            ////Invalid Edition Number
            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416040", 11, 1));
            ApiEssResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            //Get the BatchId
            var batchId = await ApiEssResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);
            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFile(ApiEssResponse, FssJwtToken);
        }

        [Ignore("This FT's issue has been fixed in PBI 74619, so we will remove this ignore tag in that branch.")]
        [Test]
        [Category("QCOnlyTest")]
        public async Task WhenICallExchangeSetApiWithAnInValidProductVersion_ThenAProductTxtFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath), Config.ExchangeSetProductFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath)}");

            //Verify Product.txt file content
            var apiScsResponse = await ScsApiClient.GetScsCatalogueAsync(Config.ExchangeSetProductType, Config.ExchangeSetCatalogueType, ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");
            var apiResponseDetails = await apiScsResponse.ReadAsStringAsync();
            dynamic apiScsResponseData = JsonConvert.DeserializeObject(apiResponseDetails);

            FileContentHelper.CheckProductFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath, Config.ExchangeSetProductFile), apiScsResponseData);
        }

        [Ignore("This FT's issue has been fixed in PBI 74619, so we will remove this ignore tag in that branch.")]
        [Test]
        [Category("QCOnlyTest")]
        public void WhenICallExchangeSetApiWithAnInvalidProductVersion_ThenAReadMeTxtFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeReadMeFile);
            Assert.IsTrue(checkFile, $"{Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder)}");

            //Verify the README.TXT file content
            FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeReadMeFile));
        }

        [Ignore("This FT's issue has been fixed in PBI 74619, so we will remove this ignore tag in that branch.")]
        [Test]
        [Category("QCOnlyTest")]
        public async Task WhenICallExchangeSetApiWithAnInValidProductVersion_ThenACatalogueFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeSetCatalogueFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder)}");

            //Verify Catalog file content
            var apiScsResponse = await ScsApiClient.GetProductVersionsAsync(Config.ExchangeSetProductType, ProductVersionData, ScsJwtToken);
            Assert.AreEqual(304, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 304.");

            FileContentHelper.CheckCatalogueFileNoContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeSetCatalogueFile), ProductVersionData);
        }

        [Ignore("This FT's issue has been fixed in PBI 74619, so we will remove this ignore tag in that branch.")]
        [Test]
        [Category("QCOnlyTest")]
        public void WhenICallExchangeSetApiWithAnInValidProductVersion_ThenASerialEncFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(DownloadedFolderPath, Config.ExchangeSetSerialEncFile);
            Assert.IsTrue(checkFile, $"{Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {DownloadedFolderPath}");

            //Verify Serial.Enc file content
            FileContentHelper.CheckSerialEncFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetSerialEncFile));
        }

        [Ignore("This FT's issue has been fixed in PBI 74619, so we will remove this ignore tag in that branch.")]
        [Test]
        [Category("QCOnlyTest")]
        public void WhenICallExchangeSetApiWithAnInValidProductVersion_ThenNoEncFilesAreDownloaded()
        {
            //Verify No folder available for the product             
            FileContentHelper.CheckNoEncFilesDownloadedAsync(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), ProductVersionData[0].ProductName);
        }

        [OneTimeTearDown]
        public async Task GlobalTeardown()
        {
            //Clean up downloaded files/folders   
            FileContentHelper.DeleteDirectory(Config.ExchangeSetFileName);

            if (CleanUpBatchIdList != null && CleanUpBatchIdList.Count > 0)
            {
                //Clean up batches from local foldar 
                var apiResponse = await FssApiClient.CleanUpBatchesAsync(Config.FssConfig.BaseUrl, CleanUpBatchIdList, FssJwtToken);
                Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned for clean up batches, instead of the expected 200.");
            }
        }
    }
}