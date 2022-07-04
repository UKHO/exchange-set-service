using NUnit.Framework;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using System.Net.Http;
using System.IO;
using System.Threading;
using System;
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
        private string BatchId { get; set; }
        private HttpResponseMessage ApiEssResponse { get; set; }

        private readonly List<string> CleanUpBatchIdList = new List<string>();

        public string currentDate = DateTime.UtcNow.ToString("ddMMMyyyy");

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
            BatchId = await FileContentHelper.ExchangeSetLargeFile(ApiEssResponse, FssJwtToken);
            CleanUpBatchIdList.Add(BatchId);            
        }

        [Test]
        [Category("SmokeTest")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAMediaTxtFileIsGenerated()
        {
            for (int i = 1; i <= 2; i++)
            {
                bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(Config.POSConfig.DirectoryPath, $"{currentDate}\\{BatchId}\\M0{i}X02\\"), Config.POSConfig.LargeExchangeSetMediaFileName);
                Assert.IsTrue(checkFile, $"File not Exist in the specified folder path :");

                FileContentHelper.CheckMediaTxtFileContent(Path.Combine(Config.POSConfig.DirectoryPath, $"{currentDate}\\{BatchId}\\M0{i}X02\\{Config.POSConfig.LargeExchangeSetMediaFileName}"), i);
            }
        }

        [Test]
        [Category("SmokeTest")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAINFOFolderIsGenerated()
        {
            for (int i = 1; i <= 2; i++)
            {
                bool checkFolder = FssBatchHelper.CheckforFolderExist(Path.Combine(Config.POSConfig.DirectoryPath, $"{currentDate}\\{BatchId}\\M0{i}X02\\"), Config.POSConfig.LargeExchangeSetInfoFolderName);
                Assert.IsTrue(checkFolder, $"Folder not Exist in the specified folder path :");
            }
        }

        [Test]
        [Category("SmokeTest")]
        public void WhenICallExchangeSetApiWithMultipleProductIdentifiers_ThenAADCFolderIsGenerated()
        {
            ////string path;
            for (int i = 1; i <= 2; i++)
            {
                ////path = Path.Combine(Config.POSConfig.DirectoryPath, currentDate, BatchId, "M0"+i+"X02", Config.POSConfig.LargeExchangeSetInfoFolderName);
                bool checkFolder = FssBatchHelper.CheckforFolderExist(Path.Combine(Config.POSConfig.DirectoryPath, $"{currentDate}\\{BatchId}\\M0{i}X02\\{Config.POSConfig.LargeExchangeSetInfoFolderName}\\"),Config.POSConfig.LargeExchangeSetAdcFolderName);
                ////bool checkFolder = FssBatchHelper.CheckforFolderExist(path, Config.POSConfig.LargeExchangeSetAdcFolderName);
                Assert.IsTrue(checkFolder, $"Folder not Exist in the specified folder path :");
            }
        }
       
    }
}