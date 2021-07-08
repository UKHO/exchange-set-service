using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class ExchangeSetValidateProductTxtFile
    {
        private string EssJwtToken { get; set; }
        private string FssJwtToken { get; set; }
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private FssApiClient FssApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper DataHelper { get; set; }


        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            FssApiClient = new FssApiClient();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssJwtToken = await authTokenProvider.GetFssToken();
            DataHelper = new DataHelper();

        }

        [Test]
        public async Task WhenICallExchangeSetApiWithAValidProductIdentifiers_ThenAProductTxtFileIsGenerated()
        {
            List<ProductVersionModel> ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 5));

            var apiEssResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiEssResponse.StatusCode, $"Incorrect status code is returned {apiEssResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseData = await apiEssResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            var batchStatusUrl = apiResponseData.Links.ExchangeSetBatchStatusUri;
            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);
            Assert.AreEqual("Failed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseData.Links.ExchangeSetFileUri;
            var responseFileDownload = await FssApiClient.GetFileDownloadAsync(downloadFileUrl.ToString(), accessToken: FssJwtToken);
            Assert.AreEqual(200, (int)responseFileDownload.StatusCode, $"Incorrect status code File Download api returned {responseFileDownload.StatusCode}, instead of the expected 200.");
           
            var ExtractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(responseFileDownload.ToString(), FssJwtToken);

            Console.WriteLine(ExtractDownloadedFolder);





        }

    }
}
