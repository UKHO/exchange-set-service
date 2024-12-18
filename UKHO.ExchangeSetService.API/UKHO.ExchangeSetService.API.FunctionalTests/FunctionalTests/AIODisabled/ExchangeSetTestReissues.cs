using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using System.Net.Http;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests.AIODisabled
{
    [TestFixture]
    class ExchangeSetTestReissues
    {
        private string EssJwtToken { get; set; }
        private string FssJwtToken { get; set; }
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private FssApiClient FssApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper DataHelper { get; set; }
        private SalesCatalogueApiClient ScsApiClient { get; set; }
        private List<ProductVersionModel> ProductVersionData { get; set; }
        private string ScsJwtToken { get; set; }
        private string DownloadedFolderPath { get; set; }
        private HttpResponseMessage ApiEssResponse { get; set; }
        private readonly List<string> CleanUpBatchIdList = new List<string>();

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            FssApiClient = new FssApiClient();
            var authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssJwtToken = await authTokenProvider.GetFssToken();
            DataHelper = new DataHelper();
            ScsApiClient = new SalesCatalogueApiClient(Config.ScsAuthConfig.BaseUrl);
            ScsJwtToken = await authTokenProvider.GetScsToken();
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallExchangeSetApiWithMultipleReissueProductIdentifiers_ThenEncFilesAreDownloaded()
        {
            ApiEssResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetReissueProducts(), accessToken: EssJwtToken);
            //Get the BatchId
            var batchId = await ApiEssResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);

            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFile(ApiEssResponse, FssJwtToken);

            //Get the product details from sales catalogue service
            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, DataHelper.GetReissueProducts(), ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            foreach (var product in apiScsResponseData.Products)
            {
                var productName = product.ProductName;
                var editionNumber = product.EditionNumber;

                //Enc file download verification
                foreach (var updateNumber in product.UpdateNumbers)
                {
                    await FileContentHelper.GetDownloadedEncFilesAsync(Config.FssConfig.BaseUrl, Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), productName, editionNumber, updateNumber, FssJwtToken);

                }
            }
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallExchangeSetApiWithAnUpdatePriorToSpecifiedReissueProductVersion_ThenEncFilesWillBeCreatedForLatestProductVersion()
        {

            ProductVersionData = new List<ProductVersionModel>();
            ProductVersionData.Add(DataHelper.GetProductVersionModelData("JP5BHTR7", 7, 5));

            ApiEssResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            //Get the BatchId
            var batchId = await ApiEssResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);

            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFile(ApiEssResponse, FssJwtToken);

            //Get the product details form sales catalogue service
            var apiScsResponse = await ScsApiClient.GetProductVersionsAsync(Config.ExchangeSetProductType, ProductVersionData, ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            foreach (var product in apiScsResponseData.Products)
            {
                var productName = product.ProductName;
                var editionNumber = product.EditionNumber;

                //Enc file downloaded verification
                foreach (var updateNumber in product.UpdateNumbers)
                {
                    await FileContentHelper.GetDownloadedEncFilesAsync(Config.FssConfig.BaseUrl, Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), productName, editionNumber, updateNumber, FssJwtToken);

                }
            }
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallExchangeSetApiWithASpecifiedReissueProductVersion_ThenEncFilesWillBeCreatedForLatestProductVersion()
        {

            ProductVersionData = new List<ProductVersionModel>();
            ProductVersionData.Add(DataHelper.GetProductVersionModelData("JP5BHTR7", 7, 6));

            ApiEssResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: EssJwtToken);
            //Get the BatchId
            var batchId = await ApiEssResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);

            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFile(ApiEssResponse, FssJwtToken);

            //Get the product details form sales catalogue service
            var apiScsResponse = await ScsApiClient.GetProductVersionsAsync(Config.ExchangeSetProductType, ProductVersionData, ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            foreach (var product in apiScsResponseData.Products)
            {
                var productName = product.ProductName;
                var editionNumber = product.EditionNumber;

                //Enc file downloaded verification
                foreach (var updateNumber in product.UpdateNumbers)
                {
                    await FileContentHelper.GetDownloadedEncFilesAsync(Config.FssConfig.BaseUrl, Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), productName, editionNumber, updateNumber, FssJwtToken);

                }
            }
        }

        [TearDown]
        public async Task GlobalTeardown()
        {
            //Clean up downloaded files/folders   
            FileContentHelper.DeleteDirectory(Config.ExchangeSetFileName);
            if (CleanUpBatchIdList != null && CleanUpBatchIdList.Count > 0)
            {
                //Clean up batches from local foldar 
                var apiResponse = await FssApiClient.CleanUpBatchesAsync(Config.FssConfig.BaseUrl, CleanUpBatchIdList, FssJwtToken);
                Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned for clean up batches, instead of the expected 200.");
            }
        }
    }
}
