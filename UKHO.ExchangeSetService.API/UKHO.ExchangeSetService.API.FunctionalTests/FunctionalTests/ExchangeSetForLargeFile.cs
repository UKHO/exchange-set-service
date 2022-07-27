using NUnit.Framework;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using System.Net.Http;
using System.IO;
using System.Threading;
using System.Collections.Generic;

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

        private readonly List<string> CleanUpBatchIdList = new List<string>();

        private List<string> DownloadedFolderPath;

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
            ApiEssResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifiersForLargeMedia(), accessToken: EssJwtToken);
            Thread.Sleep(5000); //File creation takes time
            //Get the BatchId
            var batchId = await ApiEssResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);
            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFileForLargeMedia(ApiEssResponse, FssJwtToken);
        }

        [Test]
        [Category("QCOnlyTest")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAMediaTxtFileIsGenerated()
        {
            int mediaNumber = 1;
            foreach (string folderPath in DownloadedFolderPath)
            {
                bool checkFile = FssBatchHelper.CheckforFileExist(folderPath, Config.POSConfig.LargeExchangeSetMediaFileName);
                Assert.IsTrue(checkFile, $"File not Exist in the specified folder path :");

                FileContentHelper.CheckMediaTxtFileContent(Path.Combine(folderPath, Config.POSConfig.LargeExchangeSetMediaFileName), mediaNumber);
                mediaNumber++;
            }

        }

        [Test]
        [Category("QCOnlyTest")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAnINFOFolderIsGenerated()
        {
            foreach (string folderPath in DownloadedFolderPath)
            {
                bool checkFolder = FssBatchHelper.CheckforFolderExist(folderPath, Config.POSConfig.LargeExchangeSetInfoFolderName);
                Assert.IsTrue(checkFolder, $"Folder not Exist in the specified folder path :");
            }
        }

        [Test]
        [Category("QCOnlyTest")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAnADCFolderIsGenerated()
        {
            foreach (string folderPath in DownloadedFolderPath)
            {
                bool checkFolder = FssBatchHelper.CheckforFolderExist(Path.Combine(folderPath, Config.POSConfig.LargeExchangeSetInfoFolderName), Config.POSConfig.LargeExchangeSetAdcFolderName);
                Assert.IsTrue(checkFolder, $"Folder not Exist in the specified folder path :");
            }
        }

        [Test]
        [Category("QCOnlyTest")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAReadMeTxtFileIsGenerated()
        {
            int baseNumber = 1;
            bool checkFolder;

            foreach (string folderPath in DownloadedFolderPath)
            {
                do
                {
                    bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(folderPath, $"B{baseNumber}", Config.ExchangeSetEncRootFolder), Config.ExchangeReadMeFile);
                    Assert.IsTrue(checkFile, $"{Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(folderPath, Config.ExchangeSetEncRootFolder)}");

                    //Verify README.TXT file content
                    FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(folderPath, $"B{baseNumber}", Config.ExchangeSetEncRootFolder, Config.ExchangeReadMeFile));

                    baseNumber++;
                    var folderName = $"B{baseNumber}";
                    checkFolder = FssBatchHelper.CheckforFolderExist(folderPath, folderName);
                } while (checkFolder);
            }
        }

        [Test]
        [Category("QCOnlyTest")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenASerialEncFileIsGenerated()
        {
            int baseNumber = 1;
            bool checkFolder;

            foreach (string folderPath in DownloadedFolderPath)
            {
                do
                {
                    bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(folderPath, $"B{baseNumber}"), Config.ExchangeSetSerialEncFile);
                    Assert.IsTrue(checkFile, $"{Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {Path.Combine(folderPath, $"B{baseNumber}")}");

                    //Verify Serial.ENC file content
                    FileContentHelper.CheckSerialEncFileContentForLargeMediaExchangeSet(Path.Combine(folderPath, $"B{baseNumber}", Config.ExchangeSetSerialEncFile), baseNumber);

                    baseNumber++;
                    var folderName = $"B{baseNumber}";
                    checkFolder = FssBatchHelper.CheckforFolderExist(folderPath, folderName);
                } while (checkFolder);
            }
        }

        [Test]
        [Category("QCOnlyTest")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenEncFilesAreGenerated()
        {
            int baseNumber = 1;
            bool checkFolder;

            foreach (string folderPath in DownloadedFolderPath)
            {
                do
                {
                    string[] checkDirectories = FssBatchHelper.CheckforDirectories(Path.Combine(folderPath, $"B{baseNumber}", Config.ExchangeSetEncRootFolder));

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
                    checkFolder = FssBatchHelper.CheckforFolderExist(folderPath, folderName);
                } while (checkFolder);
            }
        }

        [Test]
        [Category("QCOnlyTest")]
        public void WhenICallExchangeSetApiWithAnInValidProductVersion_ThenAProductTxtFileIsGenerated()
        {
            foreach (string folderPath in DownloadedFolderPath)
            {
                bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(folderPath, Config.POSConfig.LargeExchangeSetInfoFolderName), Config.ExchangeSetProductFile);
                Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(folderPath, Config.ExchangeSetProductFilePath)}");

                FileContentHelper.CheckProductFileContentLargeFile(Path.Combine(folderPath, Config.ExchangeSetProductFilePath, Config.ExchangeSetProductFile));
            }
        }

        [OneTimeTearDown]
        public async Task GlobalTeardown()
        {
            //Clean up downloaded files/folders
            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var FolderName = $"M0{mediaNumber}X02.zip";
                FileContentHelper.DeleteDirectory(FolderName);
            }

            if (CleanUpBatchIdList != null && CleanUpBatchIdList.Count > 0)
            {
                //Clean up batches from FSS
                var apiResponse = await FssApiClient.CleanUpBatchesAsync(Config.FssConfig.BaseUrl, CleanUpBatchIdList, FssJwtToken);
                Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned for clean up batches, instead of the expected 200.");
            }
        }
    }
}