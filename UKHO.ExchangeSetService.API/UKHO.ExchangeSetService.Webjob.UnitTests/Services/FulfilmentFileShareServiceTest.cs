using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentFileShareServiceTest
    {
        private IFileShareService fakefileShareService;
        private FulfilmentFileShareService fulfilmentFileShareService;
        private ILogger<FulfilmentFileShareService> fakeLogger;

        [SetUp]
        public void Setup()
        {
            fakefileShareService = A.Fake<IFileShareService>();
            fakeLogger = A.Fake<ILogger<FulfilmentFileShareService>>();

            fulfilmentFileShareService = new FulfilmentFileShareService(FakeBatchValue.FileShareServiceConfiguration, fakefileShareService, fakefileShareService, fakefileShareService, fakefileShareService, fakefileShareService, fakeLogger);
        }

        private static List<Products> GetProductdetails()
        {
            return [
                new Products
                {
                    ProductName = "DE5NOBRK",
                    EditionNumber = 0,
                    UpdateNumbers = [0, 1],
                    FileSize = 400
                }
                ];
        }

        private static SearchBatchResponse GetSearchBatchResponse(string businessUnit)
        {
            return new SearchBatchResponse()
            {
                Entries = [
                    new BatchDetail
                    {
                        BatchId ="63d38bde-5191-4a59-82d5-aa22ca1cc6dc",
                        BusinessUnit = businessUnit
                    }
                    ],
                Links = new PagingLinks(),
                Count = 0,
                Total = 0
            };
        }

        private static SalesCatalogueServiceResponseQueueMessage GetScsResponseQueueMessage()
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
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public async Task WhenRequestQueryFileShareServiceData_ThenReturnsFulfilmentDataResponse(string businessUnit)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            A.CallTo(() => fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, FakeBatchValue.ExchangeSetEncRootPath, A<string>.Ignored)).Returns(GetSearchBatchResponse(businessUnit));

            var result = await fulfilmentFileShareService.QueryFileShareServiceData(GetProductdetails(), GetScsResponseQueueMessage(), cancellationTokenSource, cancellationTokenSource.Token, FakeBatchValue.ExchangeSetEncRootPath, businessUnit);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf(typeof(List<FulfilmentDataResponse>)));
            A.CallTo(() => fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, FakeBatchValue.ExchangeSetEncRootPath, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public async Task WhenRequestQueryFileShareServiceData_WithNullProducts_ThenReturnsFulfilmentDataNullResponse(string businessUnit)
        {
            var result = await fulfilmentFileShareService.QueryFileShareServiceData(null, GetScsResponseQueueMessage(), null, CancellationToken.None, string.Empty, businessUnit);

            Assert.That(result, Is.Null);
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public async Task WhenRequestQueryFileShareServiceData_WithNoProducts_ThenReturnsFulfilmentDataNullResponse(string businessUnit)
        {
            var result = await fulfilmentFileShareService.QueryFileShareServiceData([], GetScsResponseQueueMessage(), null, CancellationToken.None, string.Empty, businessUnit);

            Assert.That(result, Is.Null);
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public void WhenIsCancellationRequestedinQueryFileShareServiceData_ThenThrowCancelledException(string businessUnit)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            var cancellationToken = cancellationTokenSource.Token;

            Assert.ThrowsAsync<OperationCanceledException>(async () => await fulfilmentFileShareService.QueryFileShareServiceData(GetProductdetails(), GetScsResponseQueueMessage(), cancellationTokenSource, cancellationToken, FakeBatchValue.ExchangeSetEncRootPath, businessUnit));

            A.CallTo(() => fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
        }

        [Test]
        public async Task WhenValidSearchReadMeFileRequest_ThenReturnFilePath()
        {
            const string readMeFilePath = $@"batch/{FakeBatchValue.BatchId}/files/README.TXT";
            A.CallTo(() => fakefileShareService.SearchReadMeFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(readMeFilePath);

            var result = await fulfilmentFileShareService.SearchReadMeFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId);
            Assert.That(result, Is.EqualTo(readMeFilePath));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenRequestDownloadReadMeFile_ThenReturnsBoolIfFileIsDownloaded(bool isFileDownloaded)
        {
            const string readMeFilePath = $@"batch/{FakeBatchValue.BatchId}/files/README.TXT";
            A.CallTo(() => fakefileShareService.DownloadReadMeFileFromFssAsync(readMeFilePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).Returns(isFileDownloaded);

            var result = await fulfilmentFileShareService.DownloadReadMeFileFromFssAsync(readMeFilePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetEncRootPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.EqualTo(isFileDownloaded));
            A.CallTo(() => fakefileShareService.DownloadReadMeFileFromFssAsync(readMeFilePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidUploadZipFileRequest_ThenReturnBool(bool isFileUploaded)
        {
            A.CallTo(() => fakefileShareService.UploadFileToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).Returns(isFileUploaded);

            var result = await fulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName);

            Assert.That(result, Is.EqualTo(isFileUploaded));
            A.CallTo(() => fakefileShareService.UploadFileToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidCreateZipFileRequest_ThenReturnBool(bool isZipFileCreated)
        {
            A.CallTo(() => fakefileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(isZipFileCreated);

            var result = await fulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.EqualTo(isZipFileCreated));
            A.CallTo(() => fakefileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
        }

        #region LargeMediaExchangeSet

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidUploadZipFileForLargeMediaExchangeSetToFileShareService_ThenReturnBool(bool isFileUploaded)
        {
            A.CallTo(() => fakefileShareService.UploadLargeMediaFileToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetFileFolder)).Returns(isFileUploaded);

            var result = await fulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetFileFolder);

            Assert.That(result, Is.EqualTo(isFileUploaded));
            A.CallTo(() => fakefileShareService.UploadLargeMediaFileToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetFileFolder)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidCommitLargeMediaExchangeSet_ThenReturnBool(bool isBatchCommitted)
        {
            A.CallTo(() => fakefileShareService.CommitAndGetBatchStatusForLargeMediaExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId)).Returns(isBatchCommitted);

            var result = await fulfilmentFileShareService.CommitLargeMediaExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.EqualTo(isBatchCommitted));
            A.CallTo(() => fakefileShareService.CommitAndGetBatchStatusForLargeMediaExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
        }

        #endregion LargeMediaExchangeSet

        #region SearchFolderFile

        [Test]
        public async Task WhenInfoSearchFolderFileRequest_ThenReturnFilePath()
        {
            var batchFiles = new List<BatchFile>();
            A.CallTo(() => fakefileShareService.SearchFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, A<string>.That.Contains(FakeBatchValue.ContentInfo))).Returns(batchFiles);

            var result = await fulfilmentFileShareService.SearchFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.Info);

            Assert.That(result, Is.EqualTo(batchFiles));
        }

        [Test]
        public async Task WhenAdcSearchFolderFileRequest_ThenReturnFilePath()
        {
            var batchFiles = new List<BatchFile>();
            A.CallTo(() => fakefileShareService.SearchFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, A<string>.That.Contains(FakeBatchValue.Content))).Returns(batchFiles);

            var result = await fulfilmentFileShareService.SearchFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.Adc);

            Assert.That(result, Is.EqualTo(batchFiles));
        }

        #endregion SearchFolderFile

        #region DownloadFolderDetails

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenRequestDownloadFolderDetails_ThenReturnsBoolForFileIsDownloaded(bool isFileDownloaded)
        {
            var batchFiles = new List<BatchFile>();
            A.CallTo(() => fakefileShareService.DownloadFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, batchFiles, FakeBatchValue.ExchangeSetMediaInfoPath)).Returns(isFileDownloaded);

            var result = await fulfilmentFileShareService.DownloadFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, batchFiles, FakeBatchValue.ExchangeSetMediaInfoPath);

            Assert.That(result, Is.EqualTo(isFileDownloaded));
            A.CallTo(() => fakefileShareService.DownloadFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, batchFiles, FakeBatchValue.ExchangeSetMediaInfoPath)).MustHaveHappenedOnceExactly();
        }

        #endregion DownloadFolderDetails

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenRequestCommitExchangeSet_ThenReturnsBoolForBatchCommitted(bool isBatchCommitted)
        {
            A.CallTo(() => fakefileShareService.CommitBatchToFss(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath, A<string>.Ignored)).Returns(isBatchCommitted);

            var result = await fulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath);

            Assert.That(result, Is.EqualTo(isBatchCommitted));
            A.CallTo(() => fakefileShareService.CommitBatchToFss(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenSearchIhoPubFilePath_ThenReturnsPath()
        {
            const string pathResponse = "path";
            A.CallTo(() => fakefileShareService.SearchIhoPubFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(pathResponse);

            var result = await fulfilmentFileShareService.SearchIhoPubFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.EqualTo(pathResponse));
            A.CallTo(() => fakefileShareService.SearchIhoPubFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenSearchIhoCrtFilePath_ThenReturnsPath()
        {
            const string pathResponse = "path";
            A.CallTo(() => fakefileShareService.SearchIhoCrtFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(pathResponse);

            var result = await fulfilmentFileShareService.SearchIhoCrtFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.EqualTo(pathResponse));
            A.CallTo(() => fakefileShareService.SearchIhoCrtFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenDownloadIhoPubFile_ThenReturnsBool(bool isFileDownloaded)
        {
            const string filePath = "path";
            A.CallTo(() => fakefileShareService.DownloadIhoPubFile(filePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(isFileDownloaded);

            var result = await fulfilmentFileShareService.DownloadIhoPubFile(filePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.EqualTo(isFileDownloaded));
            A.CallTo(() => fakefileShareService.DownloadIhoPubFile(filePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenDownloadIhoCrtFile_ThenReturnsBool(bool isFileDownloaded)
        {
            const string filePath = "path";
            A.CallTo(() => fakefileShareService.DownloadIhoCrtFile(filePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(isFileDownloaded);

            var result = await fulfilmentFileShareService.DownloadIhoCrtFile(filePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.EqualTo(isFileDownloaded));
            A.CallTo(() => fakefileShareService.DownloadIhoCrtFile(filePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenDownloadReadMeFileFromCacheAsync_ThenReturnsBool(bool isFileDownloaded)
        {
            A.CallTo(() => fakefileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).Returns(isFileDownloaded);

            var result = await fulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.EqualTo(isFileDownloaded));
            A.CallTo(() => fakefileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
        }
    }
}
