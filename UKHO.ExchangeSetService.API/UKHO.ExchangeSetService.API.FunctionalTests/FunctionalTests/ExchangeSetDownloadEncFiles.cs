using NUnit.Framework;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using System.Collections.Generic;
using System.IO;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class ExchangeSetDownloadEncFiles
    {
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper DataHelper { get; set; }
        public ProductIdentifierModel ProductIdentifierModel { get; set; }
        private string EssJwtToken { get; set; }
        private FssApiClient FssApiClient { get; set; }
        private string FssJwtToken { get; set; }

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductIdentifierModel = new ProductIdentifierModel();
            DataHelper = new DataHelper();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssApiClient = new FssApiClient();
            FssJwtToken = await authTokenProvider.GetFssToken();
        }

        [Test]
        public async Task WhenICallExchangeSetApiWithAValidProductVersions_ThenEncFilesAreDownloaded()
        {
            string productName = "DE316004";
            int editionNumber = 13;
            int updateNumber = 0;

            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData(productName, editionNumber, updateNumber));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseDetails = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseDetails.Links.ExchangeSetBatchStatusUri.Href;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);

            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseDetails.Links.ExchangeSetFileUri.Href;

            //Get temp downloaded folder
            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);

            //Verify downloaded files matches with File Share Service Api response file details             
            await FileContentHelper.CheckDownloadedEncFilesAsync(Config.FssConfig.FssApiUrl, Path.Combine(extractDownloadedFolder,Config.ExchangeSetEncRootFolder), productName, editionNumber, FssJwtToken);

        }

        [Test]
        public async Task WhenICallExchangeSetApiWithAInValidEditionNumber_ThenNoEncFilesAreDownloaded()
        {
            string productName = "DE316004";
            int editionNumber = 15;
            int updateNumber = 0;

            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData(productName, editionNumber, updateNumber));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseDetails = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseDetails.Links.ExchangeSetBatchStatusUri.Href;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);

            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseDetails.Links.ExchangeSetFileUri.Href;

            //Get temp downloaded folder
            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);

            //Verify No folder available for the product             
            FileContentHelper.CheckNoEncFilesDownloadedAsync(Path.Combine(extractDownloadedFolder, Config.ExchangeSetEncRootFolder), productName);

        }

        [Test]
        public async Task WhenICallExchangeSetApiWithAInValidUpdateNumber_ThenNoEncFilesAreDownloaded()
        {
            string productName = "DE316004";
            int editionNumber = 13;
            int updateNumber = 50;

            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData(productName, editionNumber, updateNumber));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseDetails = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseDetails.Links.ExchangeSetBatchStatusUri.Href;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);

            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseDetails.Links.ExchangeSetFileUri.Href;

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);

            //Verify No folder available for the product             
            FileContentHelper.CheckNoEncFilesDownloadedAsync(Path.Combine(extractDownloadedFolder, Config.ExchangeSetEncRootFolder), productName);

        }

        [Test]
        public async Task WhenICallExchangeSetApiWithAnInvalidValidProductIdentifier_ThenEncFilesAreDownloadedd()
        {
            string productName = "GB416080";
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(new List<string>() { productName }, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseDetails = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseDetails.Links.ExchangeSetBatchStatusUri.Href;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);

            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseDetails.Links.ExchangeSetFileUri.Href;

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);

            //Verify No folder available for the product             
            FileContentHelper.CheckNoEncFilesDownloadedAsync(Path.Combine(extractDownloadedFolder, Config.ExchangeSetEncRootFolder), productName);

        }

    }
}
