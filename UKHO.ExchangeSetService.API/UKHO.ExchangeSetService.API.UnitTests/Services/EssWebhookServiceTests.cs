using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Storage;
using UKHO.ExchangeSetService.Common.Models.AzureTableEntities;
using Attribute = UKHO.ExchangeSetService.Common.Models.Request.Attribute;
using UKHO.ExchangeSetService.API.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;

namespace UKHO.ExchangeSetService.API.UnitTests.Services
{
    [TestFixture]
    public class EssWebhookServiceTests
    {
        private EssWebhookService service;       
        private IAzureTableStorageClient fakeAzureTableStorageClient;
        private ISalesCatalogueStorageService fakeAzureStorageService;
        private IAzureBlobStorageClient fakeAzureBlobStorageClient;      
        private IOptions<CacheConfiguration> fakeCacheConfiguration;
        private IEnterpriseEventCacheDataRequestValidator fakeEnterpriseEventCacheDataRequestValidator;       
        private ILogger<EssWebhookService> fakeLogger;
        private IOptions<EssFulfilmentStorageConfiguration> fakeEssFulfilmentStorageConfig;

        [SetUp]
        public void Setup()
        {           
            fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();
            fakeAzureStorageService = A.Fake<ISalesCatalogueStorageService>();
            fakeAzureTableStorageClient = A.Fake<IAzureTableStorageClient>();            
            fakeCacheConfiguration = A.Fake<IOptions<CacheConfiguration>>();
            fakeEnterpriseEventCacheDataRequestValidator = A.Fake<IEnterpriseEventCacheDataRequestValidator>();
            fakeLogger = A.Fake<ILogger<EssWebhookService>>();
            fakeEssFulfilmentStorageConfig = A.Fake <IOptions<EssFulfilmentStorageConfiguration>>();
            fakeCacheConfiguration.Value.CacheBusinessUnit = "ADDS";
            fakeCacheConfiguration.Value.CacheProductCode = "AVCS";

            service= new EssWebhookService(fakeAzureTableStorageClient, fakeAzureStorageService, fakeAzureBlobStorageClient, fakeEnterpriseEventCacheDataRequestValidator, fakeCacheConfiguration, fakeLogger, fakeEssFulfilmentStorageConfig);
        }

        public string fakeCorrelationId = "d6cd4d37-4d89-470d-9a33-82b3d7f54b6e";
       
        private string GetStorageAccountConnectionString()
        {
            string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessstorage; AccountKey = testaccountkey;";
            return storageAccountConnectionString;
        }

        private BatchDetail GetBatchDetail()
        {
            return new BatchDetail
            {
                BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272",
                Files = new List<BatchFile>() { new BatchFile { Filename = "test.txt", FileSize = 400, Links = new Common.Models.FileShareService.Response.Links { Get = new Link { Href = "" } } } }

            };
        }

        private FssSearchResponseCache GetResponseCache()
        {
            return new FssSearchResponseCache()
            {
                BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272",
                PartitionKey = "DE416050",
                RowKey = "2|0",
                Response = JsonConvert.SerializeObject(GetBatchDetail())
            };
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
            Get linkGet = new Get()
            {
                Href = @"http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272/files/exchangeset123.zip",
            };
            LinksNew links = new LinksNew()
            {
                BatchDetails = linkBatchDetails,
                BatchStatus = linkBatchStatus,
                Get = linkGet
            };
            return new EnterpriseEventCacheDataRequest
            {
                Links = links,
                BusinessUnit = "ADDS",
                Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "DE" } ,
                                                           new Attribute { Key= "CellName", Value= "DE416050" },
                                                           new Attribute { Key= "EditionNumber", Value= "2" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "AVCS" }},
                BatchId = "d6cd4d37-4d89-470d-9a33-82b3d7f54b6e",
                BatchPublishedDate = DateTime.UtcNow
            };
        }

        private EnterpriseEventCacheDataRequest GetInvalidCacheRequestData()
        {
            BatchDetails linkBatchDetails = new BatchDetails()
            {
                Href = @"http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272"
            };
            BatchStatus linkBatchStatus = new BatchStatus()
            {
                Href = @"http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272/status"
            };
            Get linkGet = new Get()
            {
                Href = @"http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272/files/exchangeset123.zip",
            };
            LinksNew links = new LinksNew()
            {
                BatchDetails = linkBatchDetails,
                BatchStatus = linkBatchStatus,
                Get = linkGet
            };
            return new EnterpriseEventCacheDataRequest
            {
                Links = links,
                BusinessUnit = "ABC",
                Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "DE" } ,
                                                           new Attribute { Key= "CellName", Value= "DE416050" },
                                                           new Attribute { Key= "EditionNumber", Value= "2" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "DEF" }},
                BatchId = "d6cd4d37-4d89-470d-9a33-82b3d7f54b6e",
                BatchPublishedDate = DateTime.UtcNow
            };
        }

        #region ClearSearchAndDownloadCache
        [Test]
        public async Task WhenCacheDataExistsInDeleteSearchAndDownloadCache_ThenReturnResponseTrue()
        {
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetResponseCache());
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());
            A.CallTo(() => fakeAzureBlobStorageClient.DeleteCacheContainer(A<string>.Ignored, A<string>.Ignored));

            await service.DeleteSearchAndDownloadCacheData(GetCacheRequestData(), fakeCorrelationId);
            A.CallTo(() => fakeAzureTableStorageClient.DeleteAsync(A<TableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenInvalidRequestDataInDeleteSearchAndDownloadCache_ThenReturnResponseFalse()
        {
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetResponseCache());
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());
            A.CallTo(() => fakeAzureBlobStorageClient.DeleteCacheContainer(A<string>.Ignored, A<string>.Ignored));

           await service.DeleteSearchAndDownloadCacheData(GetInvalidCacheRequestData(), fakeCorrelationId);
           A.CallTo(() => fakeAzureTableStorageClient.DeleteAsync(A<TableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();           
        }

        [Test]
        public async Task WhenNoCacheDataExistsInDeleteSearchAndDownloadCache_ThenReturnResponseFalse()
        {
            var cachingResponse = new FssSearchResponseCache() { };
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(cachingResponse);
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());

            await service.DeleteSearchAndDownloadCacheData(GetCacheRequestData(), fakeCorrelationId);
            A.CallTo(() => fakeAzureTableStorageClient.DeleteAsync(A<TableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceOrLess();
        }
        #endregion
    }
}
