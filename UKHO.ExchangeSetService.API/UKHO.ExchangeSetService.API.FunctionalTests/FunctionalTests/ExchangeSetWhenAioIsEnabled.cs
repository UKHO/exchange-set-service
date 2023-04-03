using NUnit.Framework;
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
    }
}