using NUnit.Framework;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using System.Net.Http;
using System.IO;
using System.Threading;
using System;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    public class ExchangeSetForLargeFile
    {
        private string EssJwtToken { get; set; }
        private string FssJwtToken { get; set; }
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private FssApiClient FssApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper DataHelper { get; set; }
        private HttpResponseMessage ApiEssResponse { get; set; }
        
        public string currentDate = DateTime.UtcNow.ToString("ddMMMyyyy");
        private string DownloadedFolderPath { get; set; }

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
            ApiEssResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifiersForBigFile(), accessToken: EssJwtToken);
            Thread.Sleep(5000); //File creation takes time
        }

        [Test]
        [Category("QCOnlyTest")]
        public async Task WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAMediaTxtFileIsGenerated()
        {
            for (int i = 1; i <= 2; i++)
            {
                var FolderName = $"M0{i}X02";
                DownloadedFolderPath = await FileContentHelper.ExchangeSetLargeFile(ApiEssResponse, FssJwtToken, FolderName);
                bool checkFile = FssBatchHelper.CheckforLargeFileExist(DownloadedFolderPath, Config.POSConfig.LargeExchangeSetMediaFileName);
                Assert.IsTrue(checkFile, $"File not Exist in the specified folder path :");

                FileContentHelper.CheckMediaTxtFileContent(Path.Combine(DownloadedFolderPath, $"{Config.POSConfig.LargeExchangeSetMediaFileName}"), i);
            }
        }

        [Test]
        [Category("QCOnlyTest")]
        public async Task WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAINFOFolderIsGenerated()
        {
            for (int i = 1; i <= 2; i++)
            {
                var FolderName = $"M0{i}X02";
                DownloadedFolderPath = await FileContentHelper.ExchangeSetLargeFile(ApiEssResponse, FssJwtToken, FolderName);
                bool checkFolder = FssBatchHelper.CheckforFolderExist(DownloadedFolderPath, Config.POSConfig.LargeExchangeSetInfoFolderName);
                
                Assert.IsTrue(checkFolder, $"Folder not Exist in the specified folder path :");
            }
        }

        [Test]
        [Category("QCOnlyTest")]
        public async Task WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAADCFolderIsGenerated()
        {
            for (int i = 1; i <= 2; i++)
            {
                var FolderName = $"M0{i}X02";
                DownloadedFolderPath = await FileContentHelper.ExchangeSetLargeFile(ApiEssResponse, FssJwtToken, FolderName);
                bool checkFolder = FssBatchHelper.CheckforFolderExist(Path.Combine(DownloadedFolderPath, Config.POSConfig.LargeExchangeSetInfoFolderName), Config.POSConfig.LargeExchangeSetAdcFolderName);
                
                Assert.IsTrue(checkFolder, $"Folder not Exist in the specified folder path :");
            }
        }

        [TearDown]
        public void GlobalTeardown()
        {
            //Clean up downloaded files/ folders
            for (int i = 1; i <= 2; i++)
            {
                var FolderName = $"M0{i}X02.zip";
                FileContentHelper.DeleteDirectory(FolderName);
            }
        }
    }
}