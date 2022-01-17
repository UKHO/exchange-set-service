using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System;
using Attribute = UKHO.ExchangeSetService.API.FunctionalTests.Models.Attribute;

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
        private readonly List<string> CleanUpBatchIdList = new List<string>();        
        private ClearCacheHelper ClearCacheHelper { get; set; }

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
            ScsApiClient = new SalesCatalogueApiClient(Config.ScsAuthConfig.BaseUrl);
            ScsJwtToken = await authTokenProvider.GetScsToken();
            ClearCacheHelper = new ClearCacheHelper();
            ApiEssResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(new List<string>() { "DE290001" }, accessToken: EssJwtToken);
            //Get the BatchId
            var batchId = await ApiEssResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);
            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFile(ApiEssResponse, FssJwtToken);
        }
        private EnterpriseEventCacheDataRequest GetCacheRequestData()
        {
            BatchDetails linkBatchDetails = new BatchDetails()
            {
                Href = @"http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272"
            };
            BatchStatus linkBatchStatus = new BatchStatus()
            {
                Href = @"http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272/status"
            };
            GetUrl linkGet = new GetUrl()
            {
                Href = @"http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272/files/exchangeset123.zip",
            };
            LinksNew links = new LinksNew()
            {
                BatchDetails = linkBatchDetails,
                BatchStatus = linkBatchStatus,
                GetUrl =linkGet
            };
            return new EnterpriseEventCacheDataRequest
            {
                Links = links,
                BusinessUnit = "ADDS",
                Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "DE" } ,
                                                           new Attribute { Key= "CellName", Value= "DE290001" },
                                                           new Attribute { Key= "EditionNumber", Value= "1" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "AVCS" }},
                BatchId = "d6cd4d37-4d89-470d-9a33-82b3d7f54b6e",
                BatchPublishedDate = DateTime.UtcNow
            };
        }

        [Test]
        [Category("QCOnlyTest")]
        public async Task WhenICallExchangeSetApiWithProductIdentifiers_AndCalledClearCache_ThenItsClearTheCache()
        {
            //Get the product details form sales catalogue service
            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, new List<string>() { "DE290001" }, ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            foreach (var product in apiScsResponseData.Products)
            {
                string productName = product.ProductName;
                int editionNumber = product.EditionNumber;

                //Enc file download verification
                foreach (var updateNumber in product.UpdateNumbers)
                {
                    await FileContentHelper.GetDownloadedEncFilesAsync(Config.FssConfig.BaseUrl, Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), productName, editionNumber, updateNumber, FssJwtToken);

                }

            }
            var partitionKey = apiScsResponseData.Products[0].ProductName;
            var rowKey = apiScsResponseData.Products[0].EditionNumber + "|" + apiScsResponseData.Products[0].UpdateNumbers[0];

            Console.WriteLine("Storange conn " + Config.EssStorageAccountConnectionString);
            Console.WriteLine("table cache " + Config.ClearCacheConfig.FssSearchCacheTableName);
            Console.WriteLine("connection string " + Config.ClearCacheConfig.CacheStorageConnectionString);
            //Check caching info
            var tableCacheCheck = (FssSearchResponseCache)await ClearCacheHelper.RetrieveFromTableStorageAsync< FssSearchResponseCache > (partitionKey, rowKey, Config.ClearCacheConfig.FssSearchCacheTableName, Config.ClearCacheConfig.CacheStorageConnectionString);
           
            // Verify the Cache is generated
            Assert.IsNotNull(tableCacheCheck);
            Assert.IsNotEmpty(tableCacheCheck.Response);

            var essCacheJson = JObject.Parse(@"{""Type"":""uk.gov.UKHO.FileShareService.NewFilesPublished.v1""}");
            essCacheJson["Source"] = "AcceptanceTest";
            essCacheJson["Id"] = "25d6c6c1-418b-40f9-bb76-f6dfc0f133bc";
            essCacheJson["Data"] = JObject.FromObject(new { Data = GetCacheRequestData() });

            var apiClearCacheResponse = await ExchangeSetApiClient.PostEssWebhookAsync(essCacheJson, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiClearCacheResponse.StatusCode, $"Incorrect status code is returned for clear cache endpoint {apiClearCacheResponse.StatusCode}, instead of the expected status 200.");

            //Check caching info
            tableCacheCheck = (FssSearchResponseCache)await ClearCacheHelper.RetrieveFromTableStorageAsync<FssSearchResponseCache>(partitionKey, rowKey, Config.ClearCacheConfig.FssSearchCacheTableName, Config.ClearCacheConfig.CacheStorageConnectionString);

            // Verify the No Cache available
            Assert.IsNull(tableCacheCheck);

        }

        [OneTimeTearDown]
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
