using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    [TestFixture]
    public class ExchangeSetGenerateFilesForEncAndAioProductVersionWhenExchangeStandardS57 : ObjectStorage
    {
        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssJwtToken = await authTokenProvider.GetFssToken();
            ScsApiClient = new SalesCatalogueApiClient(Config.ScsAuthConfig.BaseUrl);
            ScsJwtToken = await authTokenProvider.GetScsToken();
            FssApiClient = new FssApiClient();
            DataHelper = new DataHelper();
            ProductVersionData = new List<ProductVersionModel>();
            ProductVersionData.Add(DataHelper.GetProductVersionModelData(Config.AIOConfig.AioCellName, Config.AIOConfig.AioEditionNumber, 0));
            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE4NO18Q", 2, 0));
            ApiEssResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken, callbackUri: null, exchangeSetStandard: "s57");
            //Get the BatchId
            batchId = await ApiEssResponse.GetBatchId();
            DownloadedFolderPath = await FileContentHelper.DownloadAndExtractAioZip(ApiEssResponse, FssJwtToken);
        }

        
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public void WhenIDownloadAioZipExchangeSet_ThenASerialAioFileIsAvailable()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(DownloadedFolderPath, Config.AIOConfig.ExchangeSetSerialAioFile);
            Assert.That(checkFile, Is.True, $"{Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {DownloadedFolderPath}");
        }

        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public void WhenIDownloadZipExchangeSet_ThenASerialAioFileIsNotAvailable()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(DownloadedFolderPath, Config.AIOConfig.ExchangeSetSerialAioFile);
            Assert.That(checkFile, Is.False, $"{Config.ExchangeSetSerialEncFile} File should not exist in the specified folder path : {DownloadedFolderPath}");
        }

        [OneTimeTearDown]
        public void GlobalTeardown()
        {
            //Clean up downloaded files/folders   
            FileContentHelper.DeleteDirectory(Config.AIOConfig.AioExchangeSetFileName);
        }
    }
}
