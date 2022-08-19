using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentFileShareServiceTest
    {
        private IOptions<FileShareServiceConfiguration> fakefileShareServiceConfig;
        private IFileShareService fakefileShareService;
        public FulfilmentFileShareService fulfilmentFileShareService;
        public ILogger<FulfilmentFileShareService> fakeLogger;
        public bool fakeIsFileUploaded = false;
        public bool fakeIsZipFileCreated = false;
        private bool fakeIsBatchCommitted = false;
        public string fakeExchangeSetRootPath = @"D:\\Downloads\";
        public string fakeBatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
        readonly string fakeMediaFolderName = "M01X01.zip";
        public CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        [SetUp]
        public void Setup()
        {
            fakefileShareService = A.Fake<IFileShareService>();
            fakefileShareServiceConfig = Options.Create(new FileShareServiceConfiguration()
            { Limit = 100, Start = 0, ProductLimit = 4, UpdateNumberLimit = 10, EncRoot = "ENC_ROOT", ExchangeSetFileFolder = "V01X01" });
            fakeLogger = A.Fake<ILogger<FulfilmentFileShareService>>();

            fulfilmentFileShareService = new FulfilmentFileShareService(fakefileShareServiceConfig, fakefileShareService, fakeLogger);
        }
        private List<Products> GetProductdetails()
        {
            return new List<Products> {
                            new Products {
                                ProductName = "DE5NOBRK",
                                EditionNumber = 0,
                                UpdateNumbers = new List<int?> {0,1},
                                FileSize = 400
                            }
                        };
        }

        private SearchBatchResponse GetSearchBatchResponse()
        {
            return new SearchBatchResponse()
            {
                Entries = new List<BatchDetail>() {
                    new BatchDetail {
                        BatchId ="63d38bde-5191-4a59-82d5-aa22ca1cc6dc"
                    } },
                Links = new PagingLinks(),
                Count = 0,
                Total = 0
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

        [Test]
        public async Task WhenRequestQueryFileShareServiceData_ThenReturnsFulfilmentDataResponse()
        {
            A.CallTo(() => fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(GetSearchBatchResponse());

            var result = await fulfilmentFileShareService.QueryFileShareServiceData(GetProductdetails(), GetScsResponseQueueMessage(), null, CancellationToken.None, string.Empty);

            Assert.IsNotNull(result);
            Assert.IsInstanceOf(typeof(List<FulfilmentDataResponse>), result);

            Assert.AreEqual("Received Fulfilment Data Successfully!!!!", "Received Fulfilment Data Successfully!!!!");
        }

        [Test]
        public async Task WhenRequestQueryFileShareServiceData_ThenReturnsFulfillmentDataNullResponse()
        {
            A.CallTo(() => fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(GetSearchBatchResponse());

            var result = await fulfilmentFileShareService.QueryFileShareServiceData(null, GetScsResponseQueueMessage(), null, CancellationToken.None, string.Empty);

            Assert.IsNull(result);
        }

        [Test]
        public void WhenIsCancellationRequestedinQueryFileShareServiceData_ThenThrowCancelledException()
        {
            A.CallTo(() => fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(GetSearchBatchResponse());

            cancellationTokenSource.Cancel();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            Assert.ThrowsAsync<OperationCanceledException>(async()=> await fulfilmentFileShareService.QueryFileShareServiceData(GetProductdetails(), GetScsResponseQueueMessage(), cancellationTokenSource, cancellationToken, string.Empty));    
        }

        [Test]
        public async Task WhenValidSearchReadMeFileRequest_ThenReturnFilePath()
        {
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string exchangeSetRootPath = @"batch/" + batchId + "/files/README.TXT";
            A.CallTo(() => fakefileShareService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(exchangeSetRootPath);
            var result = await fulfilmentFileShareService.SearchReadMeFilePath(batchId, null);
            Assert.IsNotEmpty(result);
        }
        [Test]
        public async Task WhenInvalidSearchReadMeFileRequest_ThenReturnEmptyFilePath()
        {
            string exchangeSetRootPath = string.Empty;
            string batchId = Guid.NewGuid().ToString();

            A.CallTo(() => fakefileShareService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(exchangeSetRootPath);
            var result = await fulfilmentFileShareService.SearchReadMeFilePath(batchId, null);

            Assert.IsEmpty(result);
        }

        [Test]
        public async Task WhenRequestDownloadReadMeFile_ThenReturnsTrueIfFileIsDownloaded()
        {
            bool isFileDownloaded = true;
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string exchangeSetRootPath = @"D:\\Downloads";
            string filePath = "TestFilePath";

            A.CallTo(() => fakefileShareService.DownloadReadMeFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isFileDownloaded);
            isFileDownloaded = await fulfilmentFileShareService.DownloadReadMeFile(filePath, batchId, exchangeSetRootPath, null);

            Assert.AreEqual(true, isFileDownloaded);
        }

        [Test]
        public async Task WhenRequestDownloadReadMeFile_ThenReturnsFalseIfFileIsNotDownloaded()
        {
            bool isFileDownloaded = false;
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string filePath = "TestFilePath";
            A.CallTo(() => fakefileShareService.DownloadReadMeFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isFileDownloaded);
            isFileDownloaded = await fulfilmentFileShareService.DownloadReadMeFile(filePath, batchId, fakeExchangeSetRootPath, null);
            Assert.AreEqual(false, isFileDownloaded);
        }

        [Test]
        public async Task WhenValidUploadZipFileRequest_ThenReturnTrue()
        {
            fakeIsFileUploaded = true;
            A.CallTo(() => fakefileShareService.UploadFileToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(fakeIsFileUploaded);
            fakeIsFileUploaded = await fulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(fakeBatchId, fakeExchangeSetRootPath, null);
            Assert.AreEqual(true, fakeIsFileUploaded);
        }

        [Test]
        public async Task WhenInvalidUploadZipFileRequest_ThenReturnFalse()
        {
            fakeIsFileUploaded = false;
            A.CallTo(() => fakefileShareService.UploadFileToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(fakeIsFileUploaded);
            fakeIsFileUploaded = await fulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(fakeBatchId, string.Empty, null);
            Assert.AreEqual(false, fakeIsFileUploaded);
        }

        [Test]
        public async Task WhenValidCreateZipFileRequest_ThenReturnTrue()
        {
            fakeIsZipFileCreated = true;
            A.CallTo(() => fakefileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(fakeIsZipFileCreated);
            fakeIsZipFileCreated = await fulfilmentFileShareService.CreateZipFileForExchangeSet(fakeBatchId, fakeExchangeSetRootPath, null);
            Assert.AreEqual(true, fakeIsZipFileCreated);
        }

        [Test]
        public async Task WhenInvalidCreateZipFileRequest_ThenReturnFalse()
        {
            fakeIsZipFileCreated = false;
            A.CallTo(() => fakefileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(fakeIsZipFileCreated);
            fakeIsZipFileCreated = await fulfilmentFileShareService.CreateZipFileForExchangeSet(fakeBatchId, string.Empty, null);
            Assert.AreEqual(false, fakeIsZipFileCreated);
        }

        #region LargeMediaExchangeSet

        [Test]
        public async Task WhenValidUploadZipFileForLargeMediaExchangeSetToFileShareService_ThenReturnTrue()
        {
            fakeIsFileUploaded = true;
            A.CallTo(() => fakefileShareService.UploadLargeMediaFileToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(fakeIsFileUploaded);
            fakeIsFileUploaded = await fulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(fakeBatchId, fakeExchangeSetRootPath, null, fakeMediaFolderName);
            Assert.AreEqual(true, fakeIsFileUploaded);
        }

        [Test]
        public async Task WhenInvalidUploadZipFileForLargeMediaExchangeSetToFileShareService_ThenReturnFalse()
        {
            fakeIsFileUploaded = false;
            A.CallTo(() => fakefileShareService.UploadLargeMediaFileToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(fakeIsFileUploaded);
            fakeIsFileUploaded = await fulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(fakeBatchId, string.Empty, null, fakeMediaFolderName);
            Assert.AreEqual(false, fakeIsFileUploaded);
        }

        [Test]
        public async Task WhenValidCommitLargeMediaExchangeSet_ThenReturnTrue()
        {
            fakeIsBatchCommitted = true;
            A.CallTo(() => fakefileShareService.CommitAndGetBatchStatusForLargeMediaExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(fakeIsBatchCommitted);
            fakeIsBatchCommitted = await fulfilmentFileShareService.CommitLargeMediaExchangeSet(fakeBatchId, fakeExchangeSetRootPath, null);
            Assert.AreEqual(true, fakeIsBatchCommitted);
        }

        [Test]
        public async Task WhenInvalidCommitLargeMediaExchangeSet_ThenReturnFalse()
        {
            fakeIsBatchCommitted = false;
            A.CallTo(() => fakefileShareService.CommitAndGetBatchStatusForLargeMediaExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(fakeIsBatchCommitted);
            fakeIsBatchCommitted = await fulfilmentFileShareService.CommitLargeMediaExchangeSet(fakeBatchId, fakeExchangeSetRootPath, null);
            Assert.AreEqual(false, fakeIsBatchCommitted);
        }

        #region SearchAdcFolderFile
        [Test]
        public async Task WhenValidSearchAdcFolderFileRequest_ThenReturnFilePath()
        {
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            var batchFileList = new List<BatchFile>() {
                new BatchFile{  Filename = "test.txt", FileSize = 400, Links = new Links { Get = new Link { Href = "" } } }

            };

            A.CallTo(() => fakefileShareService.SearchFolderDetails(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(batchFileList);
            var result = await fulfilmentFileShareService.SearchAdcFilePath(batchId, null);
            Assert.IsNotEmpty(result);
        }

        [Test]
        public async Task WhenInvalidSearchAdcFolderFileRequest_ThenReturnEmptyFileList()
        { 
            string batchId = Guid.NewGuid().ToString();

            List<BatchFile> batchFileList = new List<BatchFile>();

            A.CallTo(() => fakefileShareService.SearchFolderDetails(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(batchFileList);
            var result = await fulfilmentFileShareService.SearchAdcFilePath(batchId, null);

            Assert.IsEmpty(result);
        }

        #endregion SearchAdcFolderFile

        #region DownloadFolderDetails
        [Test]
        public async Task WhenRequestDownloadFolderDetails_ThenReturnsTrueIfFileIsDownloaded()
        {
            bool isFileDownloaded = true;
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            var batchFileList = new List<BatchFile>() {
                new BatchFile{  Filename = "test.txt", FileSize = 400, Links = new Links { Get = new Link { Href = "" } } }
            };

            A.CallTo(() => fakefileShareService.DownloadFolderDetails(A<string>.Ignored, A<string>.Ignored, A<List<BatchFile>>.Ignored, A<string>.Ignored)).Returns(isFileDownloaded);
            isFileDownloaded = await fulfilmentFileShareService.DownloadFolderDetails(fakeExchangeSetRootPath, batchId, batchFileList, null);

            Assert.AreEqual(true, isFileDownloaded);
        }

        [Test]
        public async Task WhenRequestDownloadFolderDetails_ThenReturnsFalseIfFileIsNotDownloaded()
        {
            bool isFileDownloaded = false;
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            var batchFileList = new List<BatchFile>() {
                new BatchFile{  Filename = "test.txt", FileSize = 400, Links = new Links { Get = new Link { Href = "" } } }
            };

            A.CallTo(() => fakefileShareService.DownloadFolderDetails(A<string>.Ignored, A<string>.Ignored, A<List<BatchFile>>.Ignored, A<string>.Ignored)).Returns(isFileDownloaded);
            isFileDownloaded = await fulfilmentFileShareService.DownloadFolderDetails(fakeExchangeSetRootPath, batchId, batchFileList, null);

            Assert.AreEqual(false, isFileDownloaded);
        }

        #endregion DownloadFolderDetails

        #endregion
    }
}
