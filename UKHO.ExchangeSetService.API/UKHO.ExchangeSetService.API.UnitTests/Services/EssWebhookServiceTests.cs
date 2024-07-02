using FakeItEasy;
using FluentValidation.Results;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureTableEntities;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Storage;
using Attribute = UKHO.ExchangeSetService.Common.Models.Request.Attribute;

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
        private const string FakeCorrelationId = "d6cd4d37-4d89-470d-9a33-82b3d7f54b6e";

        [SetUp]
        public void Setup()
        {
            fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();
            fakeAzureStorageService = A.Fake<ISalesCatalogueStorageService>();
            fakeAzureTableStorageClient = A.Fake<IAzureTableStorageClient>();
            fakeCacheConfiguration = A.Fake<IOptions<CacheConfiguration>>();
            fakeEnterpriseEventCacheDataRequestValidator = A.Fake<IEnterpriseEventCacheDataRequestValidator>();
            fakeLogger = A.Fake<ILogger<EssWebhookService>>();
            fakeEssFulfilmentStorageConfig = A.Fake<IOptions<EssFulfilmentStorageConfiguration>>();
            fakeCacheConfiguration.Value.S63CacheBusinessUnit = "ADDS";
            fakeCacheConfiguration.Value.S57CacheBusinessUnit = "ADDS-S57";
            fakeCacheConfiguration.Value.CacheProductCode = "AVCS";

            service = new EssWebhookService(fakeAzureTableStorageClient, fakeAzureStorageService, fakeAzureBlobStorageClient, fakeEnterpriseEventCacheDataRequestValidator, fakeCacheConfiguration, fakeLogger, fakeEssFulfilmentStorageConfig);
        }

        [Test]
        public async Task WhenInvalidCacheDataRequest_ThenValidateEventGridCacheDataRequestReturnsOKWithInvalidFlag()
        {
            A.CallTo(() => fakeEnterpriseEventCacheDataRequestValidator.Validate(A<EnterpriseEventCacheDataRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("PostESSWebhook", "OK")}));

            var result = await service.ValidateEventGridCacheDataRequest(new EnterpriseEventCacheDataRequest()
            { Attributes = new List<Attribute>() { new Attribute() { Key = null } } });

            A.CallTo(() => fakeAzureTableStorageClient.DeleteAsync(A<TableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeAzureBlobStorageClient.DeleteCacheContainer(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public async Task WhenInvalidRequestDataInDeleteSearchAndDownloadCache_ThenReturnResponseFalse()
        {
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetCacheResponse(fakeCacheConfiguration.Value.S63CacheBusinessUnit));
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());

            await service.DeleteSearchAndDownloadCacheData(GetInvalidCacheRequestData(), FakeCorrelationId);

            A.CallTo(() => fakeAzureTableStorageClient.DeleteAsync(A<TableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeAzureBlobStorageClient.DeleteCacheContainer(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadInvalidCacheDataFoundEvent.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Invalid data found in Search and Download Cache Request for ProductName:{cellName}, BusinessUnit:{businessUnit} and ProductCode:{productCode} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenNoS63CacheDataExistsInDeleteSearchAndDownloadCache_ThenReturnResponseFalse()
        {
            var cachingResponse = new FssSearchResponseCache() { };
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(cachingResponse);
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());

            await service.DeleteSearchAndDownloadCacheData(GetCacheRequestData(fakeCacheConfiguration.Value.S63CacheBusinessUnit), FakeCorrelationId);

            A.CallTo(() => fakeAzureTableStorageClient.DeleteAsync(A<TableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeAzureBlobStorageClient.DeleteCacheContainer(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataEventStart.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Search and Download cache data deletion from table and Blob started for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheNoDataFoundEvent.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No Matching Product found in Search and Download Cache table:{cacheConfiguration.Value.FssSearchCacheTableName} with ProductName:{cellName} and BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataEventCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Search and Download cache data deletion from table and Blob completed for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenNoS57CacheDataExistsInDeleteSearchAndDownloadCache_ThenReturnResponseFalse()
        {
            var cachingResponse = new FssSearchResponseCache() { };
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(cachingResponse);
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());

            await service.DeleteSearchAndDownloadCacheData(GetCacheRequestData(fakeCacheConfiguration.Value.S57CacheBusinessUnit), FakeCorrelationId);

            A.CallTo(() => fakeAzureTableStorageClient.DeleteAsync(A<TableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeAzureBlobStorageClient.DeleteCacheContainer(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataEventStart.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Search and Download cache data deletion from table and Blob started for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheNoDataFoundEvent.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No Matching Product found in Search and Download Cache table:{cacheConfiguration.Value.FssSearchCacheTableName} with ProductName:{cellName} and BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataEventCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Search and Download cache data deletion from table and Blob completed for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenS63CacheDataExistsInDeleteSearchAndDownloadCache_ThenReturnResponseTrue()
        {
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetCacheResponse(fakeCacheConfiguration.Value.S63CacheBusinessUnit));
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());

            await service.DeleteSearchAndDownloadCacheData(GetCacheRequestData(fakeCacheConfiguration.Value.S63CacheBusinessUnit), FakeCorrelationId);

            A.CallTo(() => fakeAzureTableStorageClient.DeleteAsync(A<TableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeAzureBlobStorageClient.DeleteCacheContainer(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataEventStart.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Search and Download cache data deletion from table and Blob started for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataFromTableStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Deletion started for Search and Download cache data from table:{cacheConfiguration.Value.FssSearchCacheTableName} for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheInfo.BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataFromTableCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Deletion completed for Search and Download cache data from table:{cacheConfiguration.Value.FssSearchCacheTableName} for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataFromContainerStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Deletion started for Search and Download cache data from Blob Container for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataFromContainerCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Deletion completed for Search and Download cache data from Blob Container for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenS57CacheDataExistsInDeleteSearchAndDownloadCache_ThenReturnResponseTrue()
        {
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetCacheResponse(fakeCacheConfiguration.Value.S57CacheBusinessUnit));
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());

            await service.DeleteSearchAndDownloadCacheData(GetCacheRequestData(fakeCacheConfiguration.Value.S57CacheBusinessUnit), FakeCorrelationId);

            A.CallTo(() => fakeAzureTableStorageClient.DeleteAsync(A<TableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeAzureBlobStorageClient.DeleteCacheContainer(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataEventStart.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Search and Download cache data deletion from table and Blob started for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataFromTableStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Deletion started for Search and Download cache data from table:{cacheConfiguration.Value.FssSearchCacheTableName} for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheInfo.BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataFromTableCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Deletion completed for Search and Download cache data from table:{cacheConfiguration.Value.FssSearchCacheTableName} for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataFromContainerStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Deletion started for Search and Download cache data from Blob Container for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataFromContainerCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Deletion completed for Search and Download cache data from Blob Container for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

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

        private FssSearchResponseCache GetCacheResponse(string businessUnit)
        {
            return new FssSearchResponseCache()
            {
                BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272",
                PartitionKey = "DE416050",
                RowKey = $"2|0|{businessUnit}",
                Response = JsonConvert.SerializeObject(GetBatchDetail())
            };
        }

        private EnterpriseEventCacheDataRequest GetCacheRequestData(string businessUnit)
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
            CacheLinks links = new CacheLinks()
            {
                BatchDetails = linkBatchDetails,
                BatchStatus = linkBatchStatus,
                Get = linkGet
            };
            return new EnterpriseEventCacheDataRequest
            {
                Links = links,
                BusinessUnit = businessUnit,
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
            CacheLinks links = new CacheLinks()
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
    }
}
