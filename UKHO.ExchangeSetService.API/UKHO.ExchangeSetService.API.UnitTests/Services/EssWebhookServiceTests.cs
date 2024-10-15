using Azure.Data.Tables;
using FakeItEasy;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
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
        private IFileShareServiceCache fakeFileShareServiceCache;
        private IFileShareServiceClient fakeFileShareServiceClient;
        private IFileSystemHelper fakeFileSystemHelper;
        private IOptions<FileShareServiceConfiguration> fakeFileShareServiceConfig;
        private IAuthFssTokenProvider fakeAuthFssTokenProvider;

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
            fakeFileShareServiceCache = A.Fake<IFileShareServiceCache>();
            fakeFileShareServiceClient = A.Fake<IFileShareServiceClient>();
            fakeFileSystemHelper = A.Fake<FileSystemHelper>();
            fakeFileShareServiceConfig = A.Fake<IOptions<FileShareServiceConfiguration>>();
            fakeFileShareServiceConfig.Value.ReadMeFileName = "README.TXT";
            fakeAuthFssTokenProvider = A.Fake<IAuthFssTokenProvider>();

            service = new EssWebhookService(fakeAzureTableStorageClient, fakeAzureStorageService, fakeAzureBlobStorageClient, fakeEnterpriseEventCacheDataRequestValidator, fakeCacheConfiguration, fakeLogger, fakeEssFulfilmentStorageConfig, fakeFileShareServiceCache, fakeFileShareServiceClient, fakeFileSystemHelper, fakeFileShareServiceConfig, fakeAuthFssTokenProvider);
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
        public async Task WhenInvalidRequestData_ThenReturnResponseFalse()
        {
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetCacheResponse(fakeCacheConfiguration.Value.S63CacheBusinessUnit));
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());

            await service.InvalidateAndInsertCacheDataAsync(GetInvalidCacheRequestData(), FakeCorrelationId);

            A.CallTo(() => fakeAzureTableStorageClient.DeleteAsync(A<TableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeAzureBlobStorageClient.DeleteCacheContainer(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.InsertCacheInvalidDataFoundEvent.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Invalid data found in search and download cache Request for ProductName:{cellName}, BusinessUnit:{businessUnit} and ProductCode:{productCode} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenNoS63CacheDataExists_ThenReturnResponseFalse()
        {
            var cachingResponse = new FssSearchResponseCache() { };
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(cachingResponse);
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());

            await service.InvalidateAndInsertCacheDataAsync(GetCacheRequestData(fakeCacheConfiguration.Value.S63CacheBusinessUnit), FakeCorrelationId);

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
        public async Task WhenNoS57CacheDataExists_ThenReturnResponseFalse()
        {
            var cachingResponse = new FssSearchResponseCache() { };
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(cachingResponse);
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());

            await service.InvalidateAndInsertCacheDataAsync(GetCacheRequestData(fakeCacheConfiguration.Value.S57CacheBusinessUnit), FakeCorrelationId);

            A.CallTo(() => fakeAzureTableStorageClient.DeleteAsync(A<ITableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
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
        public async Task WhenS63CacheDataExists_ThenReturnResponseTrue()
        {
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetCacheResponse(fakeCacheConfiguration.Value.S63CacheBusinessUnit));
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());

            await service.InvalidateAndInsertCacheDataAsync(GetCacheRequestData(fakeCacheConfiguration.Value.S63CacheBusinessUnit), FakeCorrelationId);

            A.CallTo(() => fakeAzureTableStorageClient.DeleteAsync(A<ITableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
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
        public async Task WhenS57CacheDataExists_ThenReturnResponseTrue()
        {
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetCacheResponse(fakeCacheConfiguration.Value.S57CacheBusinessUnit));
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());

            await service.InvalidateAndInsertCacheDataAsync(GetCacheRequestData(fakeCacheConfiguration.Value.S57CacheBusinessUnit), FakeCorrelationId);

            A.CallTo(() => fakeAzureTableStorageClient.DeleteAsync(A<ITableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
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
        public async Task WhenFileDataNotExistInRequestInvalidateAndInsertCacheData_ThenDataNotAddedToBlob()
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetCacheResponse(fakeCacheConfiguration.Value.S57CacheBusinessUnit));

            await service.InvalidateAndInsertCacheDataAsync(GetCacheRequestData(fakeCacheConfiguration.Value.S57CacheBusinessUnit), FakeCorrelationId);

            A.CallTo(() => fakeFileShareServiceCache.CopyFileToBlob(A<MemoryStream>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.UploadCacheDataEventStart.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == " Upload Cache data to table and blob started for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}").MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.InsertCacheMissingData.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Cache search and download files data missing in Request for ProductName:{cellName}, BusinessUnit:{businessUnit} and ProductCode:{productCode} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenFileDataExistInRequestInvalidateAndInsertCacheData_ThenDataAddedToBlob()
        {
            HttpResponseMessage responseMessage = GetResponse();
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetCacheResponse(fakeCacheConfiguration.Value.S57CacheBusinessUnit));

            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(Task.FromResult(responseMessage));

            await service.InvalidateAndInsertCacheDataAsync(GetCacheRequestDataWithFiles(fakeCacheConfiguration.Value.S57CacheBusinessUnit), FakeCorrelationId);

            A.CallTo(() => fakeFileShareServiceCache.CopyFileToBlob(A<MemoryStream>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.UploadCacheDataEventStart.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Upload Cache data to table and blob started for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.UploadCacheDataToBlobEvent.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Upload Cache data, save file to blob for ProductName:{cellName} of BusinessUnit:{businessUnit} and FileName:{filename} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.UploadCacheDataEventCompleted.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Upload Cache data to blob container and table completed for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{enterpriseEventCacheDataRequest.BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.InsertCacheMissingData.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Cache search and download files data missing in Request for ProductName:{cellName}, BusinessUnit:{businessUnit} and ProductCode:{productCode} and _X-Correlation-ID:{CorrelationId}").MustNotHaveHappened();
        }

        [Test]
        public async Task WhenFssReturnBadRequestInvalidateAndInsertCacheData_ThenLogException()
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            HttpResponseMessage responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            };
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetCacheResponse(fakeCacheConfiguration.Value.S57CacheBusinessUnit));
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(Task.FromResult(responseMessage));

            await service.InvalidateAndInsertCacheDataAsync(GetCacheRequestDataWithFiles(fakeCacheConfiguration.Value.S57CacheBusinessUnit), FakeCorrelationId);

            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileShareServiceCache.CopyFileToBlob(A<MemoryStream>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.UploadCacheDataEventStart.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Upload Cache data to table and blob started for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.UploadCacheDataToBlobEvent.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Upload Cache data, save file to blob for ProductName:{cellName} of BusinessUnit:{businessUnit} and FileName:{filename} and _X-Correlation-ID:{CorrelationId}").MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Error
             && call.GetArgument<EventId>(1) == EventIds.DownloadENCFilesNonOkResponse.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in search and download cache data while downloading ENC file:{fileName} with uri:{uri} responded with {StatusCode} and BatchId:{BatchId} and _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.UploadCacheDataEventCompleted.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Upload Cache data to blob container and table completed for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{enterpriseEventCacheDataRequest.BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappened();
        }

        [Test]
        public async Task WhenFssServerHeaderAsWindowsAzureBlobInvalidateAndInsertCacheData_Then307RedirectResponse()
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            HttpResponseMessage responseMessage = GetResponse();
            var headers = responseMessage.Headers;
            headers.Add("Server", "Windows-Azure-Blob/10.0");
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetCacheResponse(fakeCacheConfiguration.Value.S57CacheBusinessUnit));
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(Task.FromResult(responseMessage));

            await service.InvalidateAndInsertCacheDataAsync(GetCacheRequestDataWithFiles(fakeCacheConfiguration.Value.S57CacheBusinessUnit), FakeCorrelationId);

            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileShareServiceCache.CopyFileToBlob(A<MemoryStream>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.UploadCacheDataEventStart.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Upload Cache data to table and blob started for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.UploadCacheDataToBlobEvent.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Upload Cache data, save file to blob for ProductName:{cellName} of BusinessUnit:{businessUnit} and FileName:{filename} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.DownloadENCFiles307RedirectResponse.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Cache search and download data, download ENC file:{fileName} redirected with uri:{requestUri} responded with 307 code for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Error
             && call.GetArgument<EventId>(1) == EventIds.DownloadENCFilesNonOkResponse.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in search and download cache data while downloading ENC file:{fileName} with uri:{uri} responded with {StatusCode} and BatchId:{BatchId} and _X-Correlation-ID:{correlationId}").MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.UploadCacheDataEventCompleted.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Upload Cache data to blob container and table completed for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{enterpriseEventCacheDataRequest.BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenInvalidateAndInsertCacheDataCalled_ThenExecuteAzureStorageInsertOperations()
        {
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetCacheResponse(fakeCacheConfiguration.Value.S63CacheBusinessUnit));
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            HttpResponseMessage responseMessage = GetResponse();
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(Task.FromResult(responseMessage));

            await service.InvalidateAndInsertCacheDataAsync(GetCacheRequestDataWithFiles(fakeCacheConfiguration.Value.S57CacheBusinessUnit), FakeCorrelationId);

            A.CallTo(() => fakeFileShareServiceCache.InsertOrMergeFssCacheDetail(A<FssSearchResponseCache>.Ignored)).MustHaveHappenedOnceExactly();

        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            var nullAzureClientEx = Assert.Throws<ArgumentNullException>(() => new EssWebhookService(null, fakeAzureStorageService, fakeAzureBlobStorageClient, fakeEnterpriseEventCacheDataRequestValidator, fakeCacheConfiguration, fakeLogger, fakeEssFulfilmentStorageConfig, fakeFileShareServiceCache, fakeFileShareServiceClient, fakeFileSystemHelper, fakeFileShareServiceConfig, fakeAuthFssTokenProvider));
            Assert.That(nullAzureClientEx!.Message, Is.EqualTo("Value cannot be null. (Parameter 'azureTableStorageClient')"));
            var nullAzureServiceEx = Assert.Throws<ArgumentNullException>(() => new EssWebhookService(fakeAzureTableStorageClient, null, fakeAzureBlobStorageClient, fakeEnterpriseEventCacheDataRequestValidator, fakeCacheConfiguration, fakeLogger, fakeEssFulfilmentStorageConfig, fakeFileShareServiceCache, fakeFileShareServiceClient, fakeFileSystemHelper, fakeFileShareServiceConfig, fakeAuthFssTokenProvider));
            Assert.That(nullAzureServiceEx!.Message, Is.EqualTo("Value cannot be null. (Parameter 'azureStorageService')"));
            var nullAzureBlobEx = Assert.Throws<ArgumentNullException>(() => new EssWebhookService(fakeAzureTableStorageClient, fakeAzureStorageService, null, fakeEnterpriseEventCacheDataRequestValidator, fakeCacheConfiguration, fakeLogger, fakeEssFulfilmentStorageConfig, fakeFileShareServiceCache, fakeFileShareServiceClient, fakeFileSystemHelper, fakeFileShareServiceConfig, fakeAuthFssTokenProvider));
            Assert.That(nullAzureBlobEx!.Message, Is.EqualTo("Value cannot be null. (Parameter 'azureBlobStorageClient')"));
            var nullCacheEventDataRequestEx = Assert.Throws<ArgumentNullException>(() => new EssWebhookService(fakeAzureTableStorageClient, fakeAzureStorageService, fakeAzureBlobStorageClient, null, fakeCacheConfiguration, fakeLogger, fakeEssFulfilmentStorageConfig, fakeFileShareServiceCache, fakeFileShareServiceClient, fakeFileSystemHelper, fakeFileShareServiceConfig, fakeAuthFssTokenProvider));
            Assert.That(nullCacheEventDataRequestEx!.Message, Is.EqualTo("Value cannot be null. (Parameter 'enterpriseEventCacheDataRequestValidator')"));
            var nullCacheConfigEx = Assert.Throws<ArgumentNullException>(() => new EssWebhookService(fakeAzureTableStorageClient, fakeAzureStorageService, fakeAzureBlobStorageClient, fakeEnterpriseEventCacheDataRequestValidator, null, fakeLogger, fakeEssFulfilmentStorageConfig, fakeFileShareServiceCache, fakeFileShareServiceClient, fakeFileSystemHelper, fakeFileShareServiceConfig, fakeAuthFssTokenProvider));
            Assert.That(nullCacheConfigEx!.Message, Is.EqualTo("Value cannot be null. (Parameter 'cacheConfiguration')"));
            var nullLoggerEx = Assert.Throws<ArgumentNullException>(() => new EssWebhookService(fakeAzureTableStorageClient, fakeAzureStorageService, fakeAzureBlobStorageClient, fakeEnterpriseEventCacheDataRequestValidator, fakeCacheConfiguration, null, fakeEssFulfilmentStorageConfig, fakeFileShareServiceCache, fakeFileShareServiceClient, fakeFileSystemHelper, fakeFileShareServiceConfig, fakeAuthFssTokenProvider));
            Assert.That(nullLoggerEx!.Message, Is.EqualTo("Value cannot be null. (Parameter 'logger')"));
            var nullFulFilmentStorageEx = Assert.Throws<ArgumentNullException>(() => new EssWebhookService(fakeAzureTableStorageClient, fakeAzureStorageService, fakeAzureBlobStorageClient, fakeEnterpriseEventCacheDataRequestValidator, fakeCacheConfiguration, fakeLogger, null, fakeFileShareServiceCache, fakeFileShareServiceClient, fakeFileSystemHelper, fakeFileShareServiceConfig, fakeAuthFssTokenProvider));
            Assert.That(nullFulFilmentStorageEx!.Message, Is.EqualTo("Value cannot be null. (Parameter 'essFulfilmentStorageconfig')"));
            var nullFssCacheStorageEx = Assert.Throws<ArgumentNullException>(() => new EssWebhookService(fakeAzureTableStorageClient, fakeAzureStorageService, fakeAzureBlobStorageClient, fakeEnterpriseEventCacheDataRequestValidator, fakeCacheConfiguration, fakeLogger, fakeEssFulfilmentStorageConfig, null, fakeFileShareServiceClient, fakeFileSystemHelper, fakeFileShareServiceConfig, fakeAuthFssTokenProvider));
            Assert.That(nullFssCacheStorageEx!.Message, Is.EqualTo("Value cannot be null. (Parameter 'fileShareServiceCache')"));
            var nullFssClientEx = Assert.Throws<ArgumentNullException>(() => new EssWebhookService(fakeAzureTableStorageClient, fakeAzureStorageService, fakeAzureBlobStorageClient, fakeEnterpriseEventCacheDataRequestValidator, fakeCacheConfiguration, fakeLogger, fakeEssFulfilmentStorageConfig, fakeFileShareServiceCache, null, fakeFileSystemHelper, fakeFileShareServiceConfig, fakeAuthFssTokenProvider));
            Assert.That(nullFssClientEx!.Message, Is.EqualTo("Value cannot be null. (Parameter 'fileShareServiceClient')"));
            var nullFileSystemHelperEx = Assert.Throws<ArgumentNullException>(() => new EssWebhookService(fakeAzureTableStorageClient, fakeAzureStorageService, fakeAzureBlobStorageClient, fakeEnterpriseEventCacheDataRequestValidator, fakeCacheConfiguration, fakeLogger, fakeEssFulfilmentStorageConfig, fakeFileShareServiceCache, fakeFileShareServiceClient, null, fakeFileShareServiceConfig, fakeAuthFssTokenProvider));
            Assert.That(nullFileSystemHelperEx!.Message, Is.EqualTo("Value cannot be null. (Parameter 'fileSystemHelper')"));
            var nullFssConfigEx = Assert.Throws<ArgumentNullException>(() => new EssWebhookService(fakeAzureTableStorageClient, fakeAzureStorageService, fakeAzureBlobStorageClient, fakeEnterpriseEventCacheDataRequestValidator, fakeCacheConfiguration, fakeLogger, fakeEssFulfilmentStorageConfig, fakeFileShareServiceCache, fakeFileShareServiceClient, fakeFileSystemHelper, null, fakeAuthFssTokenProvider));
            Assert.That(nullFssConfigEx!.Message, Is.EqualTo("Value cannot be null. (Parameter 'fileShareServiceConfig')"));
            var nullAuthTokenProviderEx = Assert.Throws<ArgumentNullException>(() => new EssWebhookService(fakeAzureTableStorageClient, fakeAzureStorageService, fakeAzureBlobStorageClient, fakeEnterpriseEventCacheDataRequestValidator, fakeCacheConfiguration, fakeLogger, fakeEssFulfilmentStorageConfig, fakeFileShareServiceCache, fakeFileShareServiceClient, fakeFileSystemHelper, fakeFileShareServiceConfig, null));
            Assert.That(nullAuthTokenProviderEx!.Message, Is.EqualTo("Value cannot be null. (Parameter 'authFssTokenProvider')"));

        }

        [Test]
        public async Task WhenReadmeFileWithValidDataExistInRequestInsertCacheData_ThenDataInvalidatedFromCache()
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());

            await service.InvalidateAndInsertCacheDataAsync(GetReadmeFileCacheRequestData(fakeCacheConfiguration.Value.S57CacheBusinessUnit), FakeCorrelationId);

            A.CallTo(() => fakeAzureBlobStorageClient.DeleteCacheContainer(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataFromContainerStarted.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Deletion started for readme.txt file cache data from Blob Container for BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataFromContainerCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Deletion completed for readme.txt file cache data from Blob Container for BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.InsertCacheInvalidDataFoundEvent.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Invalid data found in search and download cache Request for readme.txt BusinessUnit:{businessUnit} and ProductCode:{productType} and _X-Correlation-ID:{CorrelationId}").MustNotHaveHappened();
        }

        [Test]
        public async Task WhenReadmeFileWithInvalidDataExistInRequestInsertCacheData_ThenDataNotInvalidatedFromCache()
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionString());

            await service.InvalidateAndInsertCacheDataAsync(GetInvalidReadmeFileCacheRequestData(fakeCacheConfiguration.Value.S57CacheBusinessUnit), FakeCorrelationId);

            A.CallTo(() => fakeAzureBlobStorageClient.DeleteCacheContainer(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataFromContainerStarted.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Deletion started for readme.txt file cache data from Blob Container for BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}").MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.DeleteSearchDownloadCacheDataFromContainerCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Deletion completed for readme.txt file cache data from Blob Container for BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}").MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.InsertCacheInvalidDataFoundEvent.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Invalid data found in search and download cache Request for readme.txt BusinessUnit:{businessUnit} and ProductCode:{productType} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
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
                Files = new List<CacheFile>() { },
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
                Files = { },
                BatchId = "d6cd4d37-4d89-470d-9a33-82b3d7f54b6e",
                BatchPublishedDate = DateTime.UtcNow
            };
        }

        private EnterpriseEventCacheDataRequest GetCacheRequestDataWithFiles(string businessUnit)
        {
            List<CacheFile> cacheFiles = new List<CacheFile>();
            Get fileLink = new Get()
            {
                Href = @"/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272/files/DE416050.000",
            };
            cacheFiles.Add(new CacheFile
            {
                Filename = "DE416050.000",
                MimeType = "text/plain",
                Attributes = new List<Attribute>(),
                Links = new CacheLinks() { Get = fileLink }
            });

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
                BatchPublishedDate = DateTime.UtcNow,
                Files = cacheFiles
            };
        }
        private EnterpriseEventCacheDataRequest GetReadmeFileCacheRequestData(string businessUnit)
        {
            List<CacheFile> cacheFiles = new List<CacheFile>();
            Get fileLink = new Get()
            {
                Href = @"/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272/files/README.TXT",
            };
            cacheFiles.Add(new CacheFile
            {
                Filename = "README.TXT",
                MimeType = "text/plain",
                Attributes = new List<Attribute>(),
                Links = new CacheLinks() { Get = fileLink }
            });
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
                Attributes = new List<Attribute>
                {
                    new Attribute { Key= "Product Type", Value= "AVCS" }
                },
                BatchId = "7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272",
                BatchPublishedDate = DateTime.UtcNow,
                Files = cacheFiles
            };
        }

        private EnterpriseEventCacheDataRequest GetInvalidReadmeFileCacheRequestData(string businessUnit)
        {
            List<CacheFile> cacheFiles = new List<CacheFile>();
            Get fileLink = new Get()
            {
                Href = @"/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272/files/README.TXT",
            };
            cacheFiles.Add(new CacheFile
            {
                Filename = "README.TXT",
                MimeType = "text/plain",
                Attributes = new List<Attribute>(),
                Links = new CacheLinks() { Get = fileLink }
            });

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
                Attributes = new List<Attribute>(),
                BatchId = "7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272",
                BatchPublishedDate = DateTime.UtcNow,
                Files = cacheFiles
            };
        }
        private static string GetFakeToken()
        {
            return "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJ0ZXN0IHNlcnZlciIsImlhdCI6MTU1ODMyOTg2MCwiZXhwIjoxNTg5OTUyMjYwLCJhdWQiOiJ3d3cudGVzdC5jb20iLCJzdWIiOiJ0ZXN0dXNlckB0ZXN0LmNvbSIsIm9pZCI6IjE0Y2I3N2RjLTFiYTUtNDcxZC1hY2Y1LWEwNDBkMTM4YmFhOSJ9.uOPTbf2Tg6M2OIC6bPHsBAOUuFIuCIzQL_MV3qV6agc";
        }

        private HttpResponseMessage GetResponse()
        {
            string FakeRequestUri = "http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272/files/ENC10001.000";
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, FakeRequestUri)

            };
        }
    }
}
