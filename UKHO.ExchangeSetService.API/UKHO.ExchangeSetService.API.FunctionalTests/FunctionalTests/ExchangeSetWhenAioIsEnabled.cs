using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    [TestFixture]
    class ExchangeSetWhenAioIsEnabled
    {
        private string FssJwtToken { get; set; }
        private TestConfiguration Config { get; set; }
        private string DownloadedFolderPath { get; set; }


        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            FssJwtToken = await authTokenProvider.GetFssToken();
        }

        //Product Backlog Item 71610: Create empty SERIAL.AIO file and add to AIO exchange set
        [Test]
        [Category("SmokeTest")]
        public async Task WhenIDownloadAioZipExchangeSet_ThenASerialAioFileIsAvailable()
        {
            foreach(string batchId in Config.AIOConfig.AioExchangeSetBatchIds)
            {
                DownloadedFolderPath = await FileContentHelper.DownloadAndExtractAioZip(FssJwtToken, batchId);

                bool checkFile = FssBatchHelper.CheckforFileExist(DownloadedFolderPath, Config.AIOConfig.ExchangeSetSerialAioFile);
                Assert.IsTrue(checkFile, $"{Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {DownloadedFolderPath}");

                FileContentHelper.DeleteDirectory(Config.AIOConfig.AioExchangeSetFileName);
            }
        }

        [Test]
        [Category("SmokeTest")]
        public async Task WhenIDownloadAioZipExchangeSet_ThenAReadmeTxtFileIsAvailableAsync()
        {
            foreach (string batchId in Config.AIOConfig.AioExchangeSetBatchIds)
            {
                DownloadedFolderPath = await FileContentHelper.DownloadAndExtractAioZip(FssJwtToken, batchId);

                bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeReadMeFile);
                Assert.IsTrue(checkFile, $"{Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder)}");

                //Verify README.TXT file content
                FileContentHelper.CheckAioReadMeTxtFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeReadMeFile));

                FileContentHelper.DeleteDirectory(Config.AIOConfig.AioExchangeSetFileName);
            }
        }

        //Product Backlog Item 74322: AIO exchange set ENC Data Set files & Signature Files
        [Test]
        [Category("SmokeTest")]
        public async Task WhenIDownloadAioZipExchangeSet_ThenEncFilesAreAvailable()
        {
            foreach (string batchId in Config.AIOConfig.AioExchangeSetBatchIds)
            {
                DownloadedFolderPath = await FileContentHelper.DownloadAndExtractAioZip(FssJwtToken, batchId);

                int count = Directory.GetFiles(Path.Combine(DownloadedFolderPath, Config.AIOConfig.AioEncTempPath)).Length;
                Assert.IsTrue(count > 0, $"Downloaded Enc files count is 0, Instead of expected count should be greater than 0");

                FileContentHelper.DeleteDirectory(Config.AIOConfig.AioExchangeSetFileName);
            }
        }
    }
}