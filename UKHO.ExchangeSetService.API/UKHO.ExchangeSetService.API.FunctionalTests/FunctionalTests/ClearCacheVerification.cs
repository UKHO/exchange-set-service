using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using System.Net.Http;
using System.Collections.Generic;
using Azure.Storage.Blobs;
using Newtonsoft.Json.Linq;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    [TestFixture]
    class ClearCacheVerification
    {
        private string EssJwtToken { get; set; }
        private string FssJwtToken { get; set; }
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private FssApiClient FssApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper DataHelper { get; set; }
        private SalesCatalogueApiClient ScsApiClient { get; set; }
        private string ScsJwtToken { get; set; }
        private string DownloadedFolderPath { get; set; }
        private HttpResponseMessage ApiEssResponse { get; set; }
        private readonly List<string> cleanUpBatchIdList = new();
        private ClearCacheHelper ClearCacheHelper { get; set; }
        private BlobServiceClient BlobServiceClient { get; set; }
     

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            FssApiClient = new FssApiClient();
            AuthTokenProvider authTokenProvider = new();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssJwtToken = await authTokenProvider.GetFssToken();
            DataHelper = new DataHelper();
            ScsApiClient = new SalesCatalogueApiClient(Config.ScsAuthConfig.BaseUrl);
            ScsJwtToken = await authTokenProvider.GetScsToken();
            ClearCacheHelper = new ClearCacheHelper();
            BlobServiceClient = new BlobServiceClient(Config.ClearCacheConfig.CacheStorageConnectionString);
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallExchangeSetApiWithProductIdentifiersForExchangeSetStandards63_AndCalledClearCache_ThenCacheIsAvailable()
        {
            ApiEssResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(new List<string>() { "DE290001" }, accessToken: EssJwtToken);
            //Get the BatchId
            var batchId = await ApiEssResponse.GetBatchId();
            cleanUpBatchIdList.Add(batchId);
            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFile(ApiEssResponse, FssJwtToken);

            //Get the product details form sales catalogue service
            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, new List<string>() { "DE290001" }, ScsJwtToken);
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
            var partitionKey = apiScsResponseData.Products[0].ProductName;
            var rowKey = apiScsResponseData.Products[0].EditionNumber + "|" + apiScsResponseData.Products[0].UpdateNumbers[0] + "|" + Config.BESSConfig.S63BusinessUnit;

            //Check caching info
            var tableCacheCheck = (FssSearchResponseCache)await ClearCacheHelper.RetrieveFromTableStorageAsync<FssSearchResponseCache>(partitionKey, rowKey, Config.ClearCacheConfig.FssSearchCacheTableName, Config.ClearCacheConfig.CacheStorageConnectionString);

            // Verify the Cache is generated
            Assert.IsNotNull(tableCacheCheck);
            Assert.IsNotEmpty(tableCacheCheck.Response);

            var essCacheJson = JObject.Parse(@"{""Type"":""uk.gov.UKHO.FileShareService.NewFilesPublished.v1""}");
            essCacheJson["Source"] = "AcceptanceTest";
            essCacheJson["Id"] = "25d6c6c1-418b-40f9-bb76-f6dfc0f133bc";
            essCacheJson["Data"] = JObject.FromObject(ClearCacheHelper.GetCacheRequestData(Config.BESSConfig.S63BusinessUnit, partitionKey.Substring(0, 2), partitionKey, apiScsResponseData.Products[0].EditionNumber));

            var apiClearCacheResponse = await ExchangeSetApiClient.PostNewFilesPublishedAsync(essCacheJson, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiClearCacheResponse.StatusCode, $"Incorrect status code is returned for clear cache endpoint {apiClearCacheResponse.StatusCode}, instead of the expected status 200.");

            //Check caching info
            tableCacheCheck = (FssSearchResponseCache)await ClearCacheHelper.RetrieveFromTableStorageAsync<FssSearchResponseCache>(partitionKey, rowKey, Config.ClearCacheConfig.FssSearchCacheTableName, Config.ClearCacheConfig.CacheStorageConnectionString);

            // Verify the Cache available
            Assert.IsNotNull(tableCacheCheck);
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallExchangeSetApiWithProductIdentifiersForExchangeSetStandards57_AndCalledClearCache_ThenCacheIsAvailable()
        {
            ApiEssResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifiersS57(), null, EssJwtToken, "s57");
            //Get the BatchId
            var batchId = await ApiEssResponse.GetBatchId();
            cleanUpBatchIdList.Add(batchId);
            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFile(ApiEssResponse, FssJwtToken);

            //Get the product details form sales catalogue service
            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, DataHelper.GetProductIdentifiersS57(), ScsJwtToken);
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
            var partitionKey = apiScsResponseData.Products[0].ProductName;
            var rowKey = apiScsResponseData.Products[0].EditionNumber + "|" + apiScsResponseData.Products[0].UpdateNumbers[0] + "|" + Config.BESSConfig.S57BusinessUnit;

            //Check caching info
            var tableCacheCheck = (FssSearchResponseCache)await ClearCacheHelper.RetrieveFromTableStorageAsync<FssSearchResponseCache>(partitionKey, rowKey, Config.ClearCacheConfig.FssSearchCacheTableName, Config.ClearCacheConfig.CacheStorageConnectionString);

            // Verify the Cache is generated
            Assert.IsNotNull(tableCacheCheck);
            Assert.IsNotEmpty(tableCacheCheck.Response);

            var essCacheJson = JObject.Parse(@"{""Type"":""uk.gov.UKHO.FileShareService.NewFilesPublished.v1""}");
            essCacheJson["Source"] = "AcceptanceTest";
            essCacheJson["Id"] = "25d6c6c1-418b-40f9-bb76-f6dfc0f133bc";
            essCacheJson["Data"] = JObject.FromObject(ClearCacheHelper.GetCacheRequestData(Config.BESSConfig.S57BusinessUnit, partitionKey.Substring(0, 2), partitionKey, apiScsResponseData.Products[0].EditionNumber));

            var apiClearCacheResponse = await ExchangeSetApiClient.PostNewFilesPublishedAsync(essCacheJson, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiClearCacheResponse.StatusCode, $"Incorrect status code is returned for clear cache endpoint {apiClearCacheResponse.StatusCode}, instead of the expected status 200.");

            //Check caching info
            tableCacheCheck = (FssSearchResponseCache)await ClearCacheHelper.RetrieveFromTableStorageAsync<FssSearchResponseCache>(partitionKey, rowKey, Config.ClearCacheConfig.FssSearchCacheTableName, Config.ClearCacheConfig.CacheStorageConnectionString);

            // Verify the Cache available
            Assert.IsNotNull(tableCacheCheck);
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallNewFilePublishedEventForReadMeTxtFileWithDetailsPresentInEventPayload_ThenExistingReadMeFileDeletedFromContainer()
        {
            var readmeContainer = "readme";

            //ProductIdentifiers hit
            ApiEssResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(new List<string>() { "DE290001" }, accessToken: EssJwtToken);
            bool containerExists = await FileContentHelper.WaitForContainerAsync(BlobServiceClient, readmeContainer,3,5000);
            Assert.IsTrue(containerExists);

            // newfile publish hit
            var essCacheJson = JObject.Parse(@"{""Type"":""uk.gov.UKHO.FileShareService.NewFilesPublished.v1""}");
            essCacheJson["Source"] = "AcceptanceTest";
            essCacheJson["Id"] = "25d6c6c1-418b-40f9-bb76-f6dfc0f133bc";
            essCacheJson["Data"] = JObject.FromObject(ClearCacheHelper.GetCacheRequestDataForReadMeFile(Config.BESSConfig.S63BusinessUnit));
            var apiClearCacheResponse = await ExchangeSetApiClient.PostNewFilesPublishedAsync(essCacheJson, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiClearCacheResponse.StatusCode, $"Incorrect status code is returned for clear cache endpoint {apiClearCacheResponse.StatusCode}, instead of the expected status 200.");

            // Verify the no Cache available for readme
            containerExists = await FileContentHelper.WaitForContainerAsync(BlobServiceClient, readmeContainer,3,5000);
            Assert.IsFalse(containerExists);

            //Azure blob Container takes 30 seconds to recreate container with same id, therefore we have added delay 'Task.Delay()' to avoid intermittent failure in the pipe.
            await Task.Delay(30000);
            ApiEssResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(new List<string>() { "DE290001" }, accessToken: EssJwtToken);

        }

        [OneTimeTearDown]
        public async Task GlobalTeardown()
        {
            //Clean up downloaded files/folders   
            FileContentHelper.DeleteDirectory(Config.ExchangeSetFileName);

            if (cleanUpBatchIdList is { Count: > 0 })
            {
                //Clean up batches from local folder 
                var apiResponse = await FssApiClient.CleanUpBatchesAsync(Config.FssConfig.BaseUrl, cleanUpBatchIdList, FssJwtToken);
                Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned for clean up batches, instead of the expected 200.");
            }
        }
    }
}