﻿using FakeItEasy;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureTableEntities;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;
using Attribute = UKHO.ExchangeSetService.Common.Models.FileShareService.Response.Attribute;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers
{
    public class FileShareServiceCacheTest
    {
        private IAzureBlobStorageClient fakeAzureBlobStorageClient;
        private IAzureTableStorageClient fakeAzureTableStorageClient;
        private ILogger<FileShareServiceCache> fakeLogger;
        private ISalesCatalogueStorageService fakeAzureStorageService;
        private IOptions<CacheConfiguration> fakeCacheConfiguration;
        private IFileSystemHelper fakeFileSystemHelper;
        private IFileShareServiceCache fileShareServiceCache;
        private IOptions<AioConfiguration> fakeAioConfiguration;
        public string fulfilmentExceptionMessage = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. Please contact UKHO Customer Services quoting error code : {0} and correlation ID : {1}";

        [SetUp]
        public void Setup()
        {
            fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();
            fakeAzureTableStorageClient = A.Fake<IAzureTableStorageClient>();
            fakeLogger = A.Fake<ILogger<FileShareServiceCache>>();
            fakeAzureStorageService = A.Fake<ISalesCatalogueStorageService>();
            fakeCacheConfiguration = A.Fake<IOptions<CacheConfiguration>>();
            fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            fakeCacheConfiguration.Value.CacheStorageAccountKey = "testaccountkey";
            fakeCacheConfiguration.Value.CacheStorageAccountName = "testessstorage";
            fakeCacheConfiguration.Value.FssSearchCacheTableName = "testfsscache";
            fakeCacheConfiguration.Value.IsFssCacheEnabled = true;
            fakeAioConfiguration = A.Fake<IOptions<AioConfiguration>>();

            fileShareServiceCache = new FileShareServiceCache(fakeAzureBlobStorageClient, fakeAzureTableStorageClient, fakeLogger, fakeAzureStorageService, fakeCacheConfiguration, fakeFileSystemHelper, fakeAioConfiguration);
        }

        private (string, string) GetStorageAccountConnectionStringAndContainerName()
        {
            string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessstorage; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            string containerName = "testContainer";
            return (storageAccountConnectionString, containerName);
        }

        private List<Products> GetProductdetails()
        {
            return new List<Products> {
                            new Products {
                                ProductName = "DE416050",
                                EditionNumber = 2,
                                UpdateNumbers = new List<int?> {0},
                                FileSize = 400,
                                Bundle = new List<Bundle> { new Bundle { BundleType = "DVD", Location = "M1;B1" } }
                            }
                        };
        }

        private List<Products> GetProductdetailsForEncAndAioProduct()
        {
            return new List<Products> {
                            new Products {
                                ProductName = "DE416050",
                                EditionNumber = 2,
                                UpdateNumbers = new List<int?> {0},
                                FileSize = 400,
                                Bundle = new List<Bundle> { new Bundle { BundleType = "DVD", Location = "M1;B1" } }
                            },
                            new Products {
                                ProductName = "GB800001",
                                EditionNumber = 1,
                                UpdateNumbers = new List<int?> {0},
                                FileSize = 400,
                                Bundle = new List<Bundle> { new Bundle { BundleType = "DVD", Location = "M1;B3" } }
                            }
                        };
        }

        private SalesCatalogueServiceResponseQueueMessage GetScsResponseQueueMessage()
        {
            return new SalesCatalogueServiceResponseQueueMessage
            {
                BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272",
                FileSize = 4000,
                ScsResponseUri = "https://test/ess-test/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272.json",
                CallbackUri = "https://test-callbackuri.com",
                CorrelationId = "727c5230-2c25-4244-9580-13d90004584a"
            };
        }

        private SearchBatchResponse GetSearchBatchResponse()
        {
            return new SearchBatchResponse
            {
                Entries = new List<BatchDetail>() {
                    new BatchDetail {
                        BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272",
                        Files= new List<BatchFile>(){ new BatchFile { Filename = "test.txt", FileSize = 400, Links = new Links { Get = new Link { Href = "" }}}},
                        Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "DE" } ,
                                                           new Attribute { Key= "CellName", Value= "DE416050" },
                                                           new Attribute { Key= "EditionNumber", Value= "2" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "AVCS" }}
                    } },
                Links = new PagingLinks(),
                Count = 0,
                Total = 0
            };
        }

        private BatchDetail GetBatchDetail()
        {
            return new BatchDetail
            {
                BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272",
                Files = new List<BatchFile>() { new BatchFile { Filename = "test.txt", FileSize = 400, Links = new Links { Get = new Link { Href = "/batch/26067645-643e-4a56-xy5f-19977b4ae876/files/Test.TXT" } } } },
                Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "DE" } ,
                                                           new Attribute { Key= "CellName", Value= "DE416050" },
                                                           new Attribute { Key= "EditionNumber", Value= "2" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "AVCS" }}
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

        private FssSearchResponseCache GetEmptyResponseCache()
        {
            return new FssSearchResponseCache()
            {
                BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272",
                PartitionKey = "DE416050",
                RowKey = "2|0",
                Response = string.Empty
            };
        }

        private FssSearchResponseCache GetResponseCacheForAioProduct()
        {
            return new FssSearchResponseCache()
            {
                BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272",
                PartitionKey = "GB800001",
                RowKey = "1|0",
                Response = JsonConvert.SerializeObject(GetBatchDetailAio())
            };
        }

        private BatchDetail GetBatchDetailAio()
        {
            return new BatchDetail
            {
                BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272",
                Files = new List<BatchFile>() { new BatchFile { Filename = "test.txt", FileSize = 400, Links = new Links { Get = new Link { Href = "/batch/26067645-643e-4a56-xy5f-19977b4ae876/files/Test.TXT" } } } },
                Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "DE" } ,
                                                           new Attribute { Key= "CellName", Value= "DE416050" },
                                                           new Attribute { Key= "EditionNumber", Value= "2" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "AVCS" }}
            };
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public async Task WhenGetNonCachedProductDataForFssIsCalled_ThenReturnProductNotFound(string businessUnit)
        {
            const string exchangeSetRootPath = @"C:\\HOME";

            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetResponseCache());
            A.CallTo(() => fakeAzureBlobStorageClient.GetCloudBlockBlob(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(new CloudBlockBlob(new System.Uri("http://tempuri.org/blob")));
            A.CallTo(() => fakeFileSystemHelper.DownloadToFileAsync(A<CloudBlockBlob>.Ignored, A<string>.Ignored));
            CommonHelper.IsPeriodicOutputService = false;

            var response = await fileShareServiceCache.GetNonCachedProductDataForFss(GetProductdetails(), GetSearchBatchResponse(), exchangeSetRootPath, GetScsResponseQueueMessage(), null, CancellationToken.None, businessUnit);

            Assert.AreEqual(0, response.Count);
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public async Task WhenGetNonCachedProductDataForFssIsCalled_ThenReturnProductFound(string businessUnit)
        {
            var cachingResponse = new FssSearchResponseCache() { };
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(cachingResponse);

            var response = await fileShareServiceCache.GetNonCachedProductDataForFss(GetProductdetails(), null, string.Empty, GetScsResponseQueueMessage(), null, CancellationToken.None, businessUnit);

            Assert.IsNotNull(response);
            Assert.AreEqual(1, response.Count);
        }

        [Test]
        public async Task WhenFileDoesNotExistInBlob_ThenCopyFileToBlobUploadsFile()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("test stream"));
            const string fileName = "file name";
            const string batchId = "batch id";
            var storageConnectionString = GetStorageAccountConnectionStringAndContainerName().Item1;
            var cloudBlob = A.Fake<CloudBlockBlob>(o => o.WithArgumentsForConstructor(() => new CloudBlockBlob(new Uri("http://tempuri.org/blob"))));
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(storageConnectionString);
            A.CallTo(() => fakeAzureBlobStorageClient.GetCloudBlockBlob(fileName, storageConnectionString, batchId, false)).Returns(cloudBlob);
            A.CallTo(() => cloudBlob.ExistsAsync()).Returns(false);
            await fileShareServiceCache.CopyFileToBlob(stream, fileName, batchId);
            A.CallTo(() => cloudBlob.UploadFromStreamAsync(stream)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenFileExistsInBlob_ThenCopyFileToBlobDoesNothing()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("test stream"));
            const string fileName = "file name";
            const string batchId = "batch id";
            var storageConnectionString = GetStorageAccountConnectionStringAndContainerName().Item1;
            var cloudBlob = A.Fake<CloudBlockBlob>(o => o.WithArgumentsForConstructor(() => new CloudBlockBlob(new Uri("http://tempuri.org/blob"))));
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(storageConnectionString);
            A.CallTo(() => fakeAzureBlobStorageClient.GetCloudBlockBlob(fileName, storageConnectionString, batchId, false)).Returns(cloudBlob);
            A.CallTo(() => cloudBlob.ExistsAsync()).Returns(true);
            await fileShareServiceCache.CopyFileToBlob(stream, fileName, batchId);
            A.CallTo(() => cloudBlob.UploadFromStreamAsync(stream)).MustNotHaveHappened();
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public void WhenCancellationRequestedInGetNonCachedProductDataForFss_ThenThrowOperationCanceledException(string businessUnit)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            Assert.ThrowsAsync<OperationCanceledException>(async () => await fileShareServiceCache.GetNonCachedProductDataForFss(GetProductdetails(), GetSearchBatchResponse(), string.Empty, GetScsResponseQueueMessage(), cancellationTokenSource, cancellationToken, businessUnit));

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.CancellationTokenEvent.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Operation cancelled as IsCancellationRequested flag is true while searching ENC files from cache for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumbers:[{UpdateNumbers}]. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenInsertOrMergeFssCacheDetail_ThenReturnTrue()
        {
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureTableStorageClient.InsertOrMergeIntoTableStorageAsync(A<TableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetResponseCache());
            await fileShareServiceCache.InsertOrMergeFssCacheDetail(GetResponseCache());

            Assert.IsNotNull(true);
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public async Task WhenGetNonCachedProductDataForFssIsCalled_ThenReturnNonCachedProduct(string businessUnit)
        {
            const string exchangeSetRootPath = @"C:\\HOME";

            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetResponseCache());
            A.CallTo(() => fakeAzureBlobStorageClient.GetCloudBlockBlob(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(new CloudBlockBlob(new Uri("http://tempuri.org/blob")));
            A.CallTo(() => fakeFileSystemHelper.DownloadToFileAsync(A<CloudBlockBlob>.Ignored, A<string>.Ignored)).Throws(new Microsoft.WindowsAzure.Storage.StorageException("The specified blob does not exist"));
            CommonHelper.IsPeriodicOutputService = false;

            var nonCachedProduct = await fileShareServiceCache.GetNonCachedProductDataForFss(GetProductdetails(), GetSearchBatchResponse(), exchangeSetRootPath, GetScsResponseQueueMessage(), null, CancellationToken.None, businessUnit);

            Assert.AreEqual(1, nonCachedProduct.Count);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FileShareServiceSearchENCFilesFromCacheStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search request from cache started for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FileShareServiceSearchENCFilesFromCacheCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search request from cache completed for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FileShareServiceDownloadENCFilesFromCacheStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service download request from cache container for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit} with \n Href: [{FileUri}]. ESS BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.GetBlobDetailsWithCacheContainerException.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error while download the file from blob for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} for blobName: {Name}, fileItem: {fileItem} with error: {Message}").MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public async Task WhenGetNonCachedProductDataForFssIsCalled_ThenReturnNonCachedProductFromBlob(string businessUnit)
        {
            const string exchangeSetRootPath = @"C:\\HOME";

            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetEmptyResponseCache());
            A.CallTo(() => fakeAzureBlobStorageClient.GetCloudBlockBlob(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(new CloudBlockBlob(new Uri("http://tempuri.org/blob")));
            A.CallTo(() => fakeFileSystemHelper.DownloadToFileAsync(A<CloudBlockBlob>.Ignored, A<string>.Ignored));

            string blobResponse = JsonConvert.SerializeObject(GetBatchDetail(), Formatting.None);
            A.CallTo(() => fakeAzureBlobStorageClient.DownloadTextAsync(A<CloudBlockBlob>.Ignored)).Returns(blobResponse);
            CommonHelper.IsPeriodicOutputService = false;

            var nonCachedProduct = await fileShareServiceCache.GetNonCachedProductDataForFss(GetProductdetails(), GetSearchBatchResponse(), exchangeSetRootPath, GetScsResponseQueueMessage(), null, CancellationToken.None, businessUnit);

            Assert.AreEqual(0, nonCachedProduct.Count);
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public void WhenGetNonCachedProductDataForFssIsCalled_ThenReturnStorageException(string businessUnit)
        {
            const string exchangeSetRootPath = @"C:\\HOME";
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetResponseCache());
            A.CallTo(() => fakeAzureBlobStorageClient.GetCloudBlockBlob(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(new CloudBlockBlob(new Uri("http://tempuri.org/blob")));
            A.CallTo(() => fakeFileSystemHelper.DownloadToFileAsync(A<CloudBlockBlob>.Ignored, A<string>.Ignored)).Throws(new Microsoft.WindowsAzure.Storage.StorageException());
            CommonHelper.IsPeriodicOutputService = false;

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                 async delegate { await fileShareServiceCache.GetNonCachedProductDataForFss(GetProductdetails(), GetSearchBatchResponse(), exchangeSetRootPath, GetScsResponseQueueMessage(), null, CancellationToken.None, businessUnit); });

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FileShareServiceSearchENCFilesFromCacheStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search request from cache started for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FileShareServiceSearchENCFilesFromCacheCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search request from cache completed for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FileShareServiceDownloadENCFilesFromCacheStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service download request from cache container for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit} with \n Href: [{FileUri}]. ESS BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.DownloadENCFilesFromCacheContainerException.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error while download the file from blob for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} for blobName: {Name}, fileItem: {fileItem} with error: {Message}").MustHaveHappenedOnceExactly();

        }

        [Test]
        public async Task WhenInsertOrMergeFssCacheDetailForLargeResponseSize_ThenUploadSucessful()
        {
            FssSearchResponseCache fssSearchResponseCache = GetResponseCacheForAioProduct();
            fssSearchResponseCache.Response = new string('a', 62464);

            var storageConnectionString = GetStorageAccountConnectionStringAndContainerName().Item1;
            var cloudBlob = A.Fake<CloudBlockBlob>(o => o.WithArgumentsForConstructor(() => new CloudBlockBlob(new Uri("http://tempuri.org/blob"))));
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(storageConnectionString);
            A.CallTo(() => fakeAzureBlobStorageClient.GetCloudBlockBlob(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(cloudBlob);
            A.CallTo(() => cloudBlob.ExistsAsync()).Returns(false);
            A.CallTo(() => fakeAzureTableStorageClient.InsertOrMergeIntoTableStorageAsync(A<TableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetResponseCache());

            await fileShareServiceCache.InsertOrMergeFssCacheDetail(fssSearchResponseCache);

            A.CallTo(() => cloudBlob.UploadFromStreamAsync(A<Stream>.Ignored)).MustHaveHappenedOnceExactly();
        }

        #region LargeMediaExchangeSet

        [Test]
        public async Task WhenGetNonCachedProductDataForFssIsCalled_ThenReturnProductNotFoundForLargeMediaExchangeSet()
        {
            string exchangeSetRootPath = @"C:\\HOME";
            string businessUnit = "ADDS";

            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetResponseCache());
            A.CallTo(() => fakeAzureBlobStorageClient.GetCloudBlockBlob(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(new CloudBlockBlob(new Uri("http://tempuri.org/blob")));
            A.CallTo(() => fakeFileSystemHelper.DownloadToFileAsync(A<CloudBlockBlob>.Ignored, A<string>.Ignored));
            CommonHelper.IsPeriodicOutputService = true;

            var response = await fileShareServiceCache.GetNonCachedProductDataForFss(GetProductdetails(), GetSearchBatchResponse(), exchangeSetRootPath, GetScsResponseQueueMessage(), null, CancellationToken.None, businessUnit);

            Assert.AreEqual(0, response.Count);
        }

        [Test]
        public async Task WhenGetNonCachedProductDataForFssIsCalled_ThenReturnProductNotFoundForLargeMediaExchangeSetWhenAioToggleIsOn()
        {
            string exchangeSetRootPath = @"C:\\HOME";
            fakeAioConfiguration.Value.AioEnabled = true;
            fakeAioConfiguration.Value.AioCells = "GB800001";
            string businessUnit = "ADDS";

            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetResponseCache()).Once().Then.Returns(GetResponseCacheForAioProduct());
            A.CallTo(() => fakeAzureBlobStorageClient.GetCloudBlockBlob(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(new CloudBlockBlob(new Uri("http://tempuri.org/blob")));
            A.CallTo(() => fakeFileSystemHelper.DownloadToFileAsync(A<CloudBlockBlob>.Ignored, A<string>.Ignored));
            CommonHelper.IsPeriodicOutputService = true;

            var response = await fileShareServiceCache.GetNonCachedProductDataForFss(GetProductdetailsForEncAndAioProduct(), GetSearchBatchResponse(), exchangeSetRootPath, GetScsResponseQueueMessage(), null, CancellationToken.None, businessUnit);

            Assert.AreEqual(0, response.Count);
        }
        #endregion

        #region DownloadFileFromCache
        [Test]
        public async Task WhenFileExistInStorage_ThenCalledDownloadToStreamAsync()
        {
            string filename = "README.TXT";
            string containerName = "readme";
            var storageConnectionString = GetStorageAccountConnectionStringAndContainerName().Item1;
            var cloudBlob = A.Fake<CloudBlockBlob>(o => o.WithArgumentsForConstructor(() => new CloudBlockBlob(new Uri("http://tempuri.org/blob"))));
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(storageConnectionString);
            A.CallTo(() => fakeAzureBlobStorageClient.GetCloudBlockBlob(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(cloudBlob);
            A.CallTo(() => cloudBlob.ExistsAsync()).Returns(true);

            await fileShareServiceCache.DownloadFileFromCacheAsync(filename, containerName);

            A.CallTo(() => cloudBlob.DownloadToStreamAsync(A<Stream>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenFileNotExistInStorage_ThenDoesNotCalledDownloadToStreamAsync()
        {
            string filename = "README.TXT";
            string containerName = "readme";
            var storageConnectionString = GetStorageAccountConnectionStringAndContainerName().Item1;
            var cloudBlob = A.Fake<CloudBlockBlob>(o => o.WithArgumentsForConstructor(() => new CloudBlockBlob(new Uri("http://tempuri.org/blob"))));
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(storageConnectionString);
            A.CallTo(() => fakeAzureBlobStorageClient.GetCloudBlockBlob(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(cloudBlob);
            A.CallTo(() => cloudBlob.ExistsAsync()).Returns(false);

            await fileShareServiceCache.DownloadFileFromCacheAsync(filename, containerName);

            A.CallTo(() => cloudBlob.DownloadToStreamAsync(A<Stream>.Ignored)).MustNotHaveHappened();
        }
        #endregion
    }
}
