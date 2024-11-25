using NUnit.Framework;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using System.Net.Http;
using System.IO;
using System.Collections.Generic;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using static UKHO.ExchangeSetService.API.FunctionalTests.Helper.TestConfiguration;

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
        private SalesCatalogueApiClient ScsApiClient { get; set; }
        private string ScsJwtToken { get; set; }
        public static readonly PeriodicOutputServiceConfiguration posDetails = new TestConfiguration().POSConfig;

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            FssApiClient = new FssApiClient();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssJwtToken = await authTokenProvider.GetFssToken();
            ScsApiClient = new SalesCatalogueApiClient(Config.ScsAuthConfig.BaseUrl);
            ScsJwtToken = await authTokenProvider.GetScsToken();
            DataHelper = new DataHelper();
            ApiEssResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifiersForLargeMedia(), accessToken: EssJwtToken);
            //Get the BatchId
            var batchId = await ApiEssResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);
            DownloadedFolderPath = new List<string>(); // rhz this is to be removed.
            //// rhz disable DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFileForLargeMedia(ApiEssResponse, FssJwtToken);
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        [Ignore("rhz Test disabled")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAMediaTxtFileIsGenerated()
        {
            int mediaNumber = 1;
            foreach (string folderPath in DownloadedFolderPath)
            {
                bool checkFile = FssBatchHelper.CheckforFileExist(folderPath, Config.POSConfig.LargeExchangeSetMediaFileName);
                Assert.That(checkFile,Is.True, $"File not Exist in the specified folder path :");

                FileContentHelper.CheckMediaTxtFileContent(Path.Combine(folderPath, Config.POSConfig.LargeExchangeSetMediaFileName), mediaNumber);
                mediaNumber++;
            }
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        [Ignore("rhz Test disabled")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAnINFOFolderWithFilesIsGenerated()
        {
            string[] infoFolderFiles = { posDetails.InfoFolderEnctandPnmstatus, posDetails.InfoFolderAvcsUserGuide, posDetails.InfoFolderAddsEul, posDetails.InfoFolderImpInfo, posDetails.EncUpdateList };
            foreach (string folderPath in DownloadedFolderPath)
            {
                //To verify the INFO folder exists
                bool checkFolder = FssBatchHelper.CheckforFolderExist(folderPath, Config.POSConfig.LargeExchangeSetInfoFolderName);
                Assert.That(checkFolder,Is.True, $"{folderPath} not Exist in the specified path");

                //To verify the files under INFO folder exists
                foreach (string infoFile in infoFolderFiles)
                {
                    Assert.That(File.Exists(Path.Combine(folderPath, Config.POSConfig.LargeExchangeSetInfoFolderName, infoFile)),Is.True);
                }
            }
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        [Ignore("rhz Test disabled")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAnADCFolderWithFilesIsGenerated()
        {
            foreach (string folderPath in DownloadedFolderPath)
            {
                //To verify the ADC folder exists
                bool checkFolder = FssBatchHelper.CheckforFolderExist(Path.Combine(folderPath, Config.POSConfig.LargeExchangeSetInfoFolderName), Config.POSConfig.LargeExchangeSetAdcFolderName);
                Assert.That(checkFolder, Is.True, $"{folderPath} does not exist in the specified path.");

                //To verify that the files exists under ADC folder
                int fileCount = Directory.GetFiles(Path.Combine(folderPath, Config.POSConfig.LargeExchangeSetInfoFolderName, Config.POSConfig.LargeExchangeSetAdcFolderName),"*.*",SearchOption.TopDirectoryOnly).Length;
                Assert.That(fileCount > 0,Is.True, $"File count is {fileCount} in the specified folder path.");
            }
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        [Ignore("rhz Test disabled")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAReadMeTxtFileIsGenerated()
        {
            int baseNumber = 1;
            bool checkFolder;
            foreach (string folderPath in DownloadedFolderPath)
            {
                do
                {
                    bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(folderPath, $"B{baseNumber}", Config.ExchangeSetEncRootFolder), Config.ExchangeReadMeFile);
                    Assert.That(checkFile, Is.True, $"{Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(folderPath, Config.ExchangeSetEncRootFolder)}");

                    //Verify README.TXT file content
                    FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(folderPath, $"B{baseNumber}", Config.ExchangeSetEncRootFolder, Config.ExchangeReadMeFile));

                    baseNumber++;
                    var folderName = $"B{baseNumber}";
                    checkFolder = FssBatchHelper.CheckforFolderExist(folderPath, folderName);
                } while (checkFolder);
            }
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        [Ignore("rhz Test disabled")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenASerialEncFileIsGenerated()
        {
            int baseNumber = 1;
            bool checkFolder;
            foreach (string folderPath in DownloadedFolderPath)
            {
                do
                {
                    bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(folderPath, $"B{baseNumber}"), Config.ExchangeSetSerialEncFile);
                    Assert.That(checkFile, Is.True, $"{Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {Path.Combine(folderPath, $"B{baseNumber}")}");

                    //Verify Serial.ENC file content
                    FileContentHelper.CheckSerialEncFileContentForLargeMediaExchangeSet(Path.Combine(folderPath, $"B{baseNumber}", Config.ExchangeSetSerialEncFile), baseNumber);

                    baseNumber++;
                    var folderName = $"B{baseNumber}";
                    checkFolder = FssBatchHelper.CheckforFolderExist(folderPath, folderName);
                } while (checkFolder);
            }
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        [Ignore("rhz Test disabled")]
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
        [Category("QCOnlyTest-AIODisabled")]
        [Ignore("rhz Test disabled")]
        public async Task WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenACATALOGFileIsGenerated()
        {
            int baseNumber = 1;
            bool checkFolder;
            foreach (string folderPath in DownloadedFolderPath)
            {
                do
                {
                    bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(folderPath, $"B{baseNumber}", Config.ExchangeSetEncRootFolder), Config.ExchangeSetCatalogueFile);
                    Assert.That(checkFile, Is.True, $"{Config.ExchangeSetCatalogueFile} File not Exist in the specified folder path : {Path.Combine(folderPath, Config.ExchangeSetEncRootFolder)}");

                    //Verify Catalog file content
                    var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, DataHelper.GetProductIdentifiersForLargeMedia(), ScsJwtToken);
                    Assert.That((int)apiScsResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

                    var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

                    FileContentHelper.CheckCatalogueFileContentForLargeMedia(Path.Combine(folderPath, $"B{baseNumber}", Config.ExchangeSetEncRootFolder, Config.ExchangeSetCatalogueFile), apiScsResponseData);

                    baseNumber++;
                    var folderName = $"B{baseNumber}";
                    checkFolder = FssBatchHelper.CheckforFolderExist(folderPath, folderName);
                } while (checkFolder);
            }
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        [Ignore("rhz Test disabled")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAProductTxtFileIsGenerated()
        {
            foreach (string folderPath in DownloadedFolderPath)
            {
                bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(folderPath, Config.POSConfig.LargeExchangeSetInfoFolderName), Config.ExchangeSetProductFile);
                Assert.That(checkFile, Is.True, $"File not Exist in the specified folder path : {Path.Combine(folderPath, Config.ExchangeSetProductFilePath)}");

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
                Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code {apiResponse.StatusCode} is returned for clean up batches, instead of the expected 200.");
            }
        }
    }
}
