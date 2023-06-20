using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
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
        public async Task WhenGetNonCachedProductDataForFssIsCalled_ThenReturnProductNotFound()
        {
            const string exchangeSetRootPath = @"C:\\HOME";

            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetResponseCache());
            A.CallTo(() => fakeAzureBlobStorageClient.GetBlobClient(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(new BlobClient(new System.Uri("http://tempuri.org/blob")));
            A.CallTo(() => fakeFileSystemHelper.DownloadToFileAsync(A<BlobClient>.Ignored, A<string>.Ignored));
            CommonHelper.IsPeriodicOutputService = false;

            var response = await fileShareServiceCache.GetNonCachedProductDataForFss(GetProductdetails(), GetSearchBatchResponse(), exchangeSetRootPath, GetScsResponseQueueMessage(), null, CancellationToken.None);

            Assert.AreEqual(0, response.Count);
        }

        [Test]
        public async Task WhenGetNonCachedProductDataForFssIsCalled_ThenReturnProductFound()
        {
            var cachingResponse = new FssSearchResponseCache() { };
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(cachingResponse);

            var response = await fileShareServiceCache.GetNonCachedProductDataForFss(GetProductdetails(), null, string.Empty, GetScsResponseQueueMessage(), null, CancellationToken.None);

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
            var blobClient = A.Fake<BlobClient>(o => o.WithArgumentsForConstructor(() => new BlobClient(new Uri("http://tempuri.org/blob"), null)));
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(storageConnectionString);
            A.CallTo(() => fakeAzureBlobStorageClient.GetBlobClient(fileName, storageConnectionString, batchId)).Returns(blobClient);
            A.CallTo(() => blobClient.ExistsAsync(default)).Returns(Response.FromValue(false, default));
            await fileShareServiceCache.CopyFileToBlob(stream, fileName, batchId);
            A.CallTo(() => blobClient.UploadAsync(stream)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenFileExistsInBlob_ThenCopyFileToBlobDoesNothing()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("test stream"));
            const string fileName = "file name";
            const string batchId = "batch id";
            var storageConnectionString = GetStorageAccountConnectionStringAndContainerName().Item1;
            var blobClient = A.Fake<BlobClient>(o => o.WithArgumentsForConstructor(() => new BlobClient(new Uri("http://tempuri.org/blob"), null)));
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(storageConnectionString);
            A.CallTo(() => fakeAzureBlobStorageClient.GetBlobClient(fileName, storageConnectionString, batchId)).Returns(blobClient);
            A.CallTo(() => blobClient.ExistsAsync(default)).Returns(Response.FromValue(true, default));
            await fileShareServiceCache.CopyFileToBlob(stream, fileName, batchId);
            A.CallTo(() => blobClient.UploadAsync(stream)).MustNotHaveHappened();
        }

        [Test]
        public void WhenCancellationRequestedInGetNonCachedProductDataForFss_ThenThrowOperationCancelledException()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            Assert.ThrowsAsync<OperationCanceledException>(async () => await fileShareServiceCache.GetNonCachedProductDataForFss(GetProductdetails(), GetSearchBatchResponse(), string.Empty, GetScsResponseQueueMessage(), cancellationTokenSource, cancellationToken));
        }

        [Test]
        public async Task WhenInsertOrMergeFssCacheDetail_ThenReturnTrue()
        {
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureTableStorageClient.InsertOrMergeIntoTableStorageAsync(A<ITableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored));
            await fileShareServiceCache.InsertOrMergeFssCacheDetail(GetResponseCache());

            Assert.IsNotNull(true);
        }

        [Test]
        public async Task WhenGetNonCachedProductDataForFssIsCalled_ThenReturnNonCachedProduct()
        {
            const string exchangeSetRootPath = @"C:\\HOME";

            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetResponseCache());
            A.CallTo(() => fakeAzureBlobStorageClient.GetBlobClient(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(new BlobClient(new Uri("http://tempuri.org/blob")));
            A.CallTo(() => fakeFileSystemHelper.DownloadToFileAsync(A<BlobClient>.Ignored, A<string>.Ignored)).Throws(new RequestFailedException(0, "Test error", BlobErrorCode.BlobNotFound.ToString(), null));
            CommonHelper.IsPeriodicOutputService = false;

            var nonCachedProduct = await fileShareServiceCache.GetNonCachedProductDataForFss(GetProductdetails(), GetSearchBatchResponse(), exchangeSetRootPath, GetScsResponseQueueMessage(), null, CancellationToken.None);

            Assert.AreEqual(1, nonCachedProduct.Count);
        }

        [Test]
        public async Task WhenGetNonCachedProductDataForFssIsCalled_ThenReturnNonCachedProductFromBlob()
        {
            const string exchangeSetRootPath = @"C:\\HOME";

            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetEmptyResponseCache());
            A.CallTo(() => fakeAzureBlobStorageClient.GetBlobClient(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(new BlobClient(new Uri("http://tempuri.org/blob")));
            A.CallTo(() => fakeFileSystemHelper.DownloadToFileAsync(A<BlobClient>.Ignored, A<string>.Ignored));

            string blobResponse = JsonConvert.SerializeObject(GetBatchDetail(), Formatting.None);
            A.CallTo(() => fakeAzureBlobStorageClient.DownloadTextAsync(A<BlobClient>.Ignored)).Returns(blobResponse);
            CommonHelper.IsPeriodicOutputService = false;

            var nonCachedProduct = await fileShareServiceCache.GetNonCachedProductDataForFss(GetProductdetails(), GetSearchBatchResponse(), exchangeSetRootPath, GetScsResponseQueueMessage(), null, CancellationToken.None);

            Assert.AreEqual(0, nonCachedProduct.Count);
        }

        [Test]
        public void WhenGetNonCachedProductDataForFssIsCalled_ThenReturnRequestFailedException()
        {
            const string exchangeSetRootPath = @"C:\\HOME";

            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetResponseCache());
            A.CallTo(() => fakeAzureBlobStorageClient.GetBlobClient(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(new BlobClient(new Uri("http://tempuri.org/blob")));
            A.CallTo(() => fakeFileSystemHelper.DownloadToFileAsync(A<BlobClient>.Ignored, A<string>.Ignored)).Throws(new RequestFailedException(0, "Test error", BlobErrorCode.BlobBeingRehydrated.ToString(), null));
            CommonHelper.IsPeriodicOutputService = false;

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                 async delegate { await fileShareServiceCache.GetNonCachedProductDataForFss(GetProductdetails(), GetSearchBatchResponse(), exchangeSetRootPath, GetScsResponseQueueMessage(), null, CancellationToken.None); });
        }

        [Test]
        public async Task WhenInsertOrMergeFssCacheDetailForLargeResponseSize_ThenUploadSuccessful()
        {
            FssSearchResponseCache fssSearchResponseCache = GetResponseCacheForAioProduct();
            fssSearchResponseCache.Response = new string('a', 62464);

            var storageConnectionString = GetStorageAccountConnectionStringAndContainerName().Item1;
            var blobClient = A.Fake<BlobClient>(o => o.WithArgumentsForConstructor(() => new BlobClient(new Uri("http://tempuri.org/blob"), null)));
            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(storageConnectionString);
            A.CallTo(() => fakeAzureBlobStorageClient.GetBlobClient(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(blobClient);
            A.CallTo(() => blobClient.ExistsAsync(default)).Returns(Response.FromValue(false, default));
            A.CallTo(() => fakeAzureTableStorageClient.InsertOrMergeIntoTableStorageAsync(A<ITableEntity>.Ignored, A<string>.Ignored, A<string>.Ignored));

            await fileShareServiceCache.InsertOrMergeFssCacheDetail(fssSearchResponseCache);

            A.CallTo(() => blobClient.UploadAsync(A<Stream>.Ignored)).MustHaveHappenedOnceExactly();
        }

        #region LargeMediaExchangeSet

        [Test]
        public async Task WhenGetNonCachedProductDataForFssIsCalled_ThenReturnProductNotFoundForLargeMediaExchangeSet()
        {
            string exchangeSetRootPath = @"C:\\HOME";

            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetResponseCache());
            A.CallTo(() => fakeAzureBlobStorageClient.GetBlobClient(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(new BlobClient(new Uri("http://tempuri.org/blob")));
            A.CallTo(() => fakeFileSystemHelper.DownloadToFileAsync(A<BlobClient>.Ignored, A<string>.Ignored));
            CommonHelper.IsPeriodicOutputService = true;

            var response = await fileShareServiceCache.GetNonCachedProductDataForFss(GetProductdetails(), GetSearchBatchResponse(), exchangeSetRootPath, GetScsResponseQueueMessage(), null, CancellationToken.None);

            Assert.AreEqual(0, response.Count);
        }

        [Test]
        public async Task WhenGetNonCachedProductDataForFssIsCalled_ThenReturnProductNotFoundForLargeMediaExchangeSetWhenAioToggleIsOn()
        {
            string exchangeSetRootPath = @"C:\\HOME";

            fakeAioConfiguration.Value.AioEnabled = true;
            fakeAioConfiguration.Value.AioCells = "GB800001";

            A.CallTo(() => fakeAzureStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetResponseCache()).Once().Then.Returns(GetResponseCacheForAioProduct());
            A.CallTo(() => fakeAzureBlobStorageClient.GetBlobClient(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(new BlobClient(new Uri("http://tempuri.org/blob")));
            A.CallTo(() => fakeFileSystemHelper.DownloadToFileAsync(A<BlobClient>.Ignored, A<string>.Ignored));
            CommonHelper.IsPeriodicOutputService = true;

            var response = await fileShareServiceCache.GetNonCachedProductDataForFss(GetProductdetailsForEncAndAioProduct(), GetSearchBatchResponse(), exchangeSetRootPath, GetScsResponseQueueMessage(), null, CancellationToken.None);

            Assert.AreEqual(0, response.Count);
        }

        #endregion
    }
}
