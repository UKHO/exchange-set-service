using NUnit.Framework;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using System.Net.Http;
using System.IO;
using System.Collections.Generic;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

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
            ////ScsJwtToken = await authTokenProvider.GetScsToken();
            ScsJwtToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiJmZGFhNDc0Zi00OWE4LTQyNTYtODQ1Zi1kYzJjMTY0NTM1ZWMiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjYxMTc1MzMwLCJuYmYiOjE2NjExNzUzMzAsImV4cCI6MTY2MTE4MDk4MiwiYWNyIjoiMSIsImFpbyI6IkFXUUFtLzhUQUFBQTUvQnFwYkFLWk9GY3Frb0FDcms0T1JEVU1YNEJMNG5qY3M3S28xcDdrSUkzeXJDdFd3ajkySXpVL1JtM012VGN3RU1LV0NVZVNFQm90eVlmY1RPb1VqbWJtandCa2VjR0tDdHpUSGV0OHQ1SlpNSmVoRUdNLzM5emsvNlF5Ti9rIiwiYW1yIjpbInB3ZCJdLCJhcHBpZCI6ImZkYWE0NzRmLTQ5YTgtNDI1Ni04NDVmLWRjMmMxNjQ1MzVlYyIsImFwcGlkYWNyIjoiMCIsImVtYWlsIjoiTWF5dXJlc2gxMDY2MUBtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjE2My4xMTYuMjA1LjExOCIsIm5hbWUiOiJNYXl1cmVzaCBTYXR5YXdhbiBHYXdkZSIsIm9pZCI6ImIzNWY1OThjLTQ5NTItNGZkZC05OGQ4LTc4MDNhODE4MjBjMSIsInJoIjoiMC5BUUlBU01vMGtUMW1CVXFXaWpHa0x3cnRQazlIcXYyb1NWWkNoRl9jTEJaRk5ld0NBRlUuIiwic2NwIjoidXNlcl9pbXBlcnNvbmF0aW9uIiwic3ViIjoiaWdRVkE4TC1mbVZmVUY0bFA3c2F0VndlUkhxdDNsVEU4c1hoYmFsQjdLdyIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoiTWF5dXJlc2gxMDY2MUBtYXN0ZWsuY29tIiwidXRpIjoiMkFqejFrMG8za2VOUjZVb3F6RTFBQSIsInZlciI6IjEuMCJ9.ZWaVrf1o6lDjb1Dfp6CNUGMwZroXnLMw6NlXSFCj_K4jGNXeh-n4y3Dy22w_iE68VSPEsNMJIkYiTX6ydaj4y22udi46wFzH9Km4F-5yeNFf_IFV5_7p2MdJyZoMJBZLTTYM4bJZvAdgq324da6RHNGpOir3pME2bOs91tv8RdT7BEdYKNs53oIem0BEmOOieulD4RoMNifxrqF8XYAwj5__6pu8HYvHz99OvozLhfjHKPBo3YW1Vo2hZ97PsIl4GVKfSltVr8MGMgKCAmIgvOcrbBc-2yh0NEC6DPrASCh3ReH6cAlVjZW4qOzsLUQGVSvDlfbpNogIVFBTfm5q9g";
            DataHelper = new DataHelper();
            ApiEssResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifiersForLargeMedia(), accessToken: EssJwtToken);
            await Task.Delay(30000);
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
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAnINFOFolderWithFilesIsGenerated()
        {
            string[] InfoFolderFiles = { "AVCS-User-Guide.pdf", "ENC TandP NM status.pdf", "End-User-Licence-Agreement-for-ADMIRALTY-digital-data-services.pdf", "Important Information for AVCS users.pdf" };
            foreach (string folderPath in DownloadedFolderPath)
            {
                bool checkFolder = FssBatchHelper.CheckforFolderExist(folderPath, Config.POSConfig.LargeExchangeSetInfoFolderName);
                Assert.IsTrue(checkFolder, $"Folder not Exist in the specified folder path :");
                foreach (string infoFile in InfoFolderFiles)
                {
                    bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(folderPath, Config.POSConfig.LargeExchangeSetInfoFolderName),infoFile);
                    Assert.IsTrue(checkFile, $"{infoFile} does not Exist in the specified folder path.");
                }
            }
        }

        [Test]
        [Category("QCOnlyTest")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAnADCFolderWithFilesIsGenerated()
        {
            foreach (string folderPath in DownloadedFolderPath)
            {
                bool checkFolder = FssBatchHelper.CheckforFolderExist(Path.Combine(folderPath, Config.POSConfig.LargeExchangeSetInfoFolderName), Config.POSConfig.LargeExchangeSetAdcFolderName);
                Assert.IsTrue(checkFolder, $"Folder not Exist in the specified folder path.");

                int fileCount = Directory.GetFiles(Path.Combine(folderPath, Config.POSConfig.LargeExchangeSetInfoFolderName, Config.POSConfig.LargeExchangeSetAdcFolderName),"*.*",SearchOption.TopDirectoryOnly).Length;
                Assert.IsTrue(fileCount > 0, $"File count is {fileCount} in the specified folder path.");
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
        public async Task WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenACATALOGFileIsGenerated()
        {
            int baseNumber = 1;
            bool checkFolder;
            foreach (string folderPath in DownloadedFolderPath)
            {
                do
                {
                    bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(folderPath, $"B{baseNumber}", Config.ExchangeSetEncRootFolder), Config.ExchangeSetCatalogueFile);
                    Assert.IsTrue(checkFile, $"{Config.ExchangeSetCatalogueFile} File not Exist in the specified folder path : {Path.Combine(folderPath, Config.ExchangeSetEncRootFolder)}");

                    //Verify Catalog file content
                    var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, DataHelper.GetProductIdentifiersForLargeMedia(), ScsJwtToken);
                    Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

                    var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

                    FileContentHelper.CheckCatalogueFileContentForLargeMedia(Path.Combine(folderPath, $"B{baseNumber}", Config.ExchangeSetEncRootFolder, Config.ExchangeSetCatalogueFile), apiScsResponseData);

                    baseNumber++;
                    var folderName = $"B{baseNumber}";
                    checkFolder = FssBatchHelper.CheckforFolderExist(folderPath, folderName);
                } while (checkFolder);
            }
        }

        [Test]
        [Category("QCOnlyTest")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAProductTxtFileIsGenerated()
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