using NUnit.Framework;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using System.Net.Http;
using System.IO;
using System.Threading;

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
        private string DownloadedFolderPath { get; set; }

        ////A hard-coded batch has been used to run the below tests becasue the dependent functionalities are part of the future sprint development
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
        [Category("SmokeTest")]
        public async Task WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAMediaTxtFileIsGenerated()
        {
            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var FolderName = $"M0{mediaNumber}X02";
                DownloadedFolderPath = await FileContentHelper.ExchangeSetLargeFile(ApiEssResponse, FssJwtToken, FolderName);
                bool checkFile = FssBatchHelper.CheckforFileExist(DownloadedFolderPath, Config.POSConfig.LargeExchangeSetMediaFileName);
                Assert.IsTrue(checkFile, $"File not Exist in the specified folder path :");

                FileContentHelper.CheckMediaTxtFileContent(Path.Combine(DownloadedFolderPath, Config.POSConfig.LargeExchangeSetMediaFileName), mediaNumber);
            }
        }

        [Test]
        [Category("SmokeTest")]
        public async Task WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAnINFOFolderIsGenerated()
        {
            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var FolderName = $"M0{mediaNumber}X02";
                DownloadedFolderPath = await FileContentHelper.ExchangeSetLargeFile(ApiEssResponse, FssJwtToken, FolderName);
                bool checkFolder = FssBatchHelper.CheckforFolderExist(DownloadedFolderPath, Config.POSConfig.LargeExchangeSetInfoFolderName);
                
                Assert.IsTrue(checkFolder, $"Folder not Exist in the specified folder path :");
            }
        }

        [Test]
        [Category("SmokeTest")]
        public async Task WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAnADCFolderIsGenerated()
        {
            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var FolderName = $"M0{mediaNumber}X02";
                DownloadedFolderPath = await FileContentHelper.ExchangeSetLargeFile(ApiEssResponse, FssJwtToken, FolderName);
                bool checkFolder = FssBatchHelper.CheckforFolderExist(Path.Combine(DownloadedFolderPath, Config.POSConfig.LargeExchangeSetInfoFolderName), Config.POSConfig.LargeExchangeSetAdcFolderName);
                
                Assert.IsTrue(checkFolder, $"Folder not Exist in the specified folder path :");
            }
        }

        [Test]
        [Category("SmokeTest")]
        public async Task WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAReadMeTxtFileIsGenerated()
        {
            int baseNumber = 1;
            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var FolderName = $"M0{mediaNumber}X02";
                bool checkFolder;
                DownloadedFolderPath = await FileContentHelper.ExchangeSetLargeFile(ApiEssResponse, FssJwtToken, FolderName);
                do
                {
                    bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, $"B{baseNumber}", Config.ExchangeSetEncRootFolder), Config.ExchangeReadMeFile);
                    Assert.IsTrue(checkFile, $"{Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder)}");

                    //Verify README.TXT file content
                    FileContentHelper.CheckReadMeTxtFileContentForLargeMediaExchangeSet(Path.Combine(DownloadedFolderPath, $"B{baseNumber}", Config.ExchangeSetEncRootFolder, Config.ExchangeReadMeFile));

                    baseNumber++;
                    var folderName = $"B{baseNumber}";
                    checkFolder = FssBatchHelper.CheckforFolderExist(DownloadedFolderPath, folderName);
                } while (checkFolder);
            }
        }

        [Test]
        [Category("SmokeTest")]
        public async Task WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenASerialEncFileIsGenerated()
        {
            int baseNumber = 1;
            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var FolderName = $"M0{mediaNumber}X02";
                bool checkFolder;
                DownloadedFolderPath = await FileContentHelper.ExchangeSetLargeFile(ApiEssResponse, FssJwtToken, FolderName);
                do
                {
                    bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, $"B{baseNumber}"), Config.ExchangeSetSerialEncFile);
                    Assert.IsTrue(checkFile, $"{Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, $"B{baseNumber}")}");

                    //Verify Serial.ENC file content
                    FileContentHelper.CheckSerialEncFileContentForLargeMediaExchangeSet(Path.Combine(DownloadedFolderPath, $"B{baseNumber}", Config.ExchangeSetSerialEncFile), baseNumber);

                    baseNumber++;
                    var folderName = $"B{baseNumber}";
                    checkFolder = FssBatchHelper.CheckforFolderExist(DownloadedFolderPath, folderName);
                } while (checkFolder);
            }
        }

        [Test]
        [Category("SmokeTest")]
        public async Task WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenEncFilesAreGenerated()
        {
            int baseNumber = 1;
            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var FolderName = $"M0{mediaNumber}X02";
                bool checkFolder;
                DownloadedFolderPath = await FileContentHelper.ExchangeSetLargeFile(ApiEssResponse, FssJwtToken, FolderName);
                do
                {
                    string [] checkDirectories = FssBatchHelper.CheckforDirectories(Path.Combine(DownloadedFolderPath, $"B{baseNumber}", Config.ExchangeSetEncRootFolder));
                    
                    foreach (var folder in checkDirectories)
                    {
                        string[] checksubDirectories = FssBatchHelper.CheckforDirectories(folder);
                        
                        foreach (var updateFolder in checksubDirectories)
                        {
                            string[] checkupdateDirectories = FssBatchHelper.CheckforDirectories(updateFolder);

                            foreach (var encFile in checkupdateDirectories)
                            {
                                int totalFileCount = FileContentHelper.FileCountInDirectories(encFile);
                                Assert.That(totalFileCount > 0, $"No files available in the folder: {encFile}");
                            }
                        }
                    }
                    baseNumber++;
                    var folderName = $"B{baseNumber}";
                    checkFolder = FssBatchHelper.CheckforFolderExist(DownloadedFolderPath, folderName);
                } while (checkFolder);
            }
        }

        [Test]
        [Category("SmokeTest")]
        public async Task WhenICallExchangeSetApiWithAnInValidProductVersion_ThenAProductTxtFileIsGenerated()
        {
            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var FolderName = $"M0{mediaNumber}X02";
                DownloadedFolderPath = await FileContentHelper.ExchangeSetLargeFile(ApiEssResponse, FssJwtToken, FolderName);
                bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.POSConfig.LargeExchangeSetInfoFolderName), Config.ExchangeSetProductFile);
                Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath)}");

                FileContentHelper.CheckProductFileContentLargeFile(Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath, Config.ExchangeSetProductFile));
            }
        }

        [TearDown]
        public void GlobalTeardown()
        {
            //Clean up downloaded files/folders
            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var FolderName = $"M0{mediaNumber}X02.zip";
                FileContentHelper.DeleteDirectory(FolderName);
            }
        }
    }
}