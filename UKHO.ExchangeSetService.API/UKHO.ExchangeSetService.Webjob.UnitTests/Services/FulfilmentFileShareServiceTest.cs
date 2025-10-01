using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
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
        private FulfilmentFileShareService fulfilmentFileShareService;
        private ILogger<FulfilmentFileShareService> fakeLogger;
        private const string FakeBatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
        private const string FakeCorrelationId = "48f53a95-0bd2-4c0c-a6ba-afded2bdffac";
        private const string FakeBatchPath = $@"C:\HOME\25SEP2025\{FakeBatchId}";
        private const string FakeExchangeSetPath = $@"{FakeBatchPath}\V01X01";
        private const string FakeExchangeSetEncRootPath = $@"{FakeExchangeSetPath}\ENC_ROOT";
        private const string FakeExchangeSetZipFileName = "V01X01.zip";
        private const string FakeExchangeSetMediaBaseNumber = "5";
        private const string FakeExchangeSetMediaInfoPath = $@"{FakeBatchPath}\M0{FakeExchangeSetMediaBaseNumber}X02\INFO";
        private const string AioExchangeSetFileFolder = "AIO";
        private const string FakeAioExchangeSetPath = $@"{FakeBatchPath}\{AioExchangeSetFileFolder}";
        private const string FakeAioExchangeSetEncRootPath = $@"{FakeAioExchangeSetPath}\ENC_ROOT";

        [SetUp]
        public void Setup()
        {
            fakefileShareService = A.Fake<IFileShareService>();
            fakefileShareServiceConfig = Options.Create(new FileShareServiceConfiguration
            {
                Limit = 100,
                Start = 0,
                ProductLimit = 4,
                UpdateNumberLimit = 10,
                EncRoot = "ENC_ROOT",
                ExchangeSetFileFolder = "V01X01",
                ExchangeSetFileName = FakeExchangeSetZipFileName,
                Info = "INFO",
                ProductType = "ProductType",
                S63BusinessUnit = "ADDS",
                ContentInfo = "DVD INFO",
                Content = "Catalogue",
                Adc = "ADC",
                AioExchangeSetFileFolder = AioExchangeSetFileFolder
            });
            fakeLogger = A.Fake<ILogger<FulfilmentFileShareService>>();

            fulfilmentFileShareService = new FulfilmentFileShareService(fakefileShareServiceConfig, fakefileShareService, fakefileShareService, fakefileShareService, fakefileShareService, fakefileShareService, fakeLogger);
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
            A.CallTo(() => fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, FakeExchangeSetEncRootPath, A<string>.Ignored)).Returns(GetSearchBatchResponse(businessUnit));

            var result = await fulfilmentFileShareService.QueryFileShareServiceData(GetProductdetails(), GetScsResponseQueueMessage(), cancellationTokenSource, cancellationTokenSource.Token, FakeExchangeSetEncRootPath, businessUnit);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf(typeof(List<FulfilmentDataResponse>)));
            A.CallTo(() => fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, FakeExchangeSetEncRootPath, A<string>.Ignored)).MustHaveHappenedOnceExactly();
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

            Assert.ThrowsAsync<OperationCanceledException>(async () => await fulfilmentFileShareService.QueryFileShareServiceData(GetProductdetails(), GetScsResponseQueueMessage(), cancellationTokenSource, cancellationToken, FakeExchangeSetEncRootPath, businessUnit));

            A.CallTo(() => fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
        }

        [Test]
        public async Task WhenValidSearchReadMeFileRequest_ThenReturnFilePath()
        {
            const string readMeFilePath = $@"batch/{FakeBatchId}/files/README.TXT";
            A.CallTo(() => fakefileShareService.SearchReadMeFilePath(FakeBatchId, FakeCorrelationId)).Returns(readMeFilePath);

            var result = await fulfilmentFileShareService.SearchReadMeFilePath(FakeBatchId, FakeCorrelationId);
            Assert.That(result, Is.EqualTo(readMeFilePath));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenRequestDownloadReadMeFile_ThenReturnsBoolIfFileIsDownloaded(bool isFileDownloaded)
        {
            const string readMeFilePath = $@"batch/{FakeBatchId}/files/README.TXT";
            A.CallTo(() => fakefileShareService.DownloadReadMeFileFromFssAsync(readMeFilePath, FakeBatchId, FakeAioExchangeSetEncRootPath, FakeCorrelationId)).Returns(isFileDownloaded);

            var result = await fulfilmentFileShareService.DownloadReadMeFileFromFssAsync(readMeFilePath, FakeBatchId, FakeAioExchangeSetEncRootPath, FakeCorrelationId);

            Assert.That(result, Is.EqualTo(isFileDownloaded));
            A.CallTo(() => fakefileShareService.DownloadReadMeFileFromFssAsync(readMeFilePath, FakeBatchId, FakeAioExchangeSetEncRootPath, FakeCorrelationId)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidUploadZipFileRequest_ThenReturnBool(bool isFileUploaded)
        {
            A.CallTo(() => fakefileShareService.UploadFileToFileShareService(FakeBatchId, FakeBatchPath, FakeCorrelationId, FakeExchangeSetZipFileName)).Returns(isFileUploaded);

            var result = await fulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchId, FakeBatchPath, FakeCorrelationId, FakeExchangeSetZipFileName);

            Assert.That(result, Is.EqualTo(isFileUploaded));
            A.CallTo(() => fakefileShareService.UploadFileToFileShareService(FakeBatchId, FakeBatchPath, FakeCorrelationId, FakeExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidCreateZipFileRequest_ThenReturnBool(bool isZipFileCreated)
        {
            A.CallTo(() => fakefileShareService.CreateZipFileForExchangeSet(FakeBatchId, FakeExchangeSetPath, FakeCorrelationId)).Returns(isZipFileCreated);

            var result = await fulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchId, FakeExchangeSetPath, FakeCorrelationId);

            Assert.That(result, Is.EqualTo(isZipFileCreated));
            A.CallTo(() => fakefileShareService.CreateZipFileForExchangeSet(FakeBatchId, FakeExchangeSetPath, FakeCorrelationId)).MustHaveHappenedOnceExactly();
        }

        #region LargeMediaExchangeSet

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidUploadZipFileForLargeMediaExchangeSetToFileShareService_ThenReturnBool(bool isFileUploaded)
        {
            A.CallTo(() => fakefileShareService.UploadLargeMediaFileToFileShareService(FakeBatchId, FakeBatchPath, FakeCorrelationId, AioExchangeSetFileFolder)).Returns(isFileUploaded);

            var result = await fulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(FakeBatchId, FakeBatchPath, FakeCorrelationId, AioExchangeSetFileFolder);

            Assert.That(result, Is.EqualTo(isFileUploaded));
            A.CallTo(() => fakefileShareService.UploadLargeMediaFileToFileShareService(FakeBatchId, FakeBatchPath, FakeCorrelationId, AioExchangeSetFileFolder)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidCommitLargeMediaExchangeSet_ThenReturnBool(bool isBatchCommitted)
        {
            A.CallTo(() => fakefileShareService.CommitAndGetBatchStatusForLargeMediaExchangeSet(FakeBatchId, FakeBatchPath, FakeCorrelationId)).Returns(isBatchCommitted);

            var result = await fulfilmentFileShareService.CommitLargeMediaExchangeSet(FakeBatchId, FakeBatchPath, FakeCorrelationId);

            Assert.That(result, Is.EqualTo(isBatchCommitted));
            A.CallTo(() => fakefileShareService.CommitAndGetBatchStatusForLargeMediaExchangeSet(FakeBatchId, FakeBatchPath, FakeCorrelationId)).MustHaveHappenedOnceExactly();
        }

        #endregion LargeMediaExchangeSet

        #region SearchFolderFile

        [Test]
        public async Task WhenInfoSearchFolderFileRequest_ThenReturnFilePath()
        {
            var batchFiles = new List<BatchFile>();
            A.CallTo(() => fakefileShareService.SearchFolderDetails(FakeBatchId, FakeCorrelationId, A<string>.That.Contains(fakefileShareServiceConfig.Value.ContentInfo))).Returns(batchFiles);

            var result = await fulfilmentFileShareService.SearchFolderDetails(FakeBatchId, FakeCorrelationId, fakefileShareServiceConfig.Value.Info);

            Assert.That(result, Is.EqualTo(batchFiles));
        }

        [Test]
        public async Task WhenAdcSearchFolderFileRequest_ThenReturnFilePath()
        {
            var batchFiles = new List<BatchFile>();
            A.CallTo(() => fakefileShareService.SearchFolderDetails(FakeBatchId, FakeCorrelationId, A<string>.That.Contains(fakefileShareServiceConfig.Value.Content))).Returns(batchFiles);

            var result = await fulfilmentFileShareService.SearchFolderDetails(FakeBatchId, FakeCorrelationId, fakefileShareServiceConfig.Value.Adc);

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
            A.CallTo(() => fakefileShareService.DownloadFolderDetails(FakeBatchId, FakeCorrelationId, batchFiles, FakeExchangeSetMediaInfoPath)).Returns(isFileDownloaded);

            var result = await fulfilmentFileShareService.DownloadFolderDetails(FakeBatchId, FakeCorrelationId, batchFiles, FakeExchangeSetMediaInfoPath);

            Assert.That(result, Is.EqualTo(isFileDownloaded));
            A.CallTo(() => fakefileShareService.DownloadFolderDetails(FakeBatchId, FakeCorrelationId, batchFiles, FakeExchangeSetMediaInfoPath)).MustHaveHappenedOnceExactly();
        }

        #endregion DownloadFolderDetails

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenRequestCommitExchangeSet_ThenReturnsBoolForBatchCommitted(bool isBatchCommitted)
        {
            A.CallTo(() => fakefileShareService.CommitBatchToFss(FakeBatchId, FakeCorrelationId, FakeBatchPath, A<string>.Ignored)).Returns(isBatchCommitted);

            var result = await fulfilmentFileShareService.CommitExchangeSet(FakeBatchId, FakeCorrelationId, FakeBatchPath);

            Assert.That(result, Is.EqualTo(isBatchCommitted));
            A.CallTo(() => fakefileShareService.CommitBatchToFss(FakeBatchId, FakeCorrelationId, FakeBatchPath, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenSearchIhoPubFilePath_ThenReturnsPath()
        {
            const string pathResponse = "path";
            A.CallTo(() => fakefileShareService.SearchIhoPubFilePath(FakeBatchId, FakeCorrelationId)).Returns(pathResponse);

            var result = await fulfilmentFileShareService.SearchIhoPubFilePath(FakeBatchId, FakeCorrelationId);

            Assert.That(result, Is.EqualTo(pathResponse));
            A.CallTo(() => fakefileShareService.SearchIhoPubFilePath(FakeBatchId, FakeCorrelationId)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenSearchIhoCrtFilePath_ThenReturnsPath()
        {
            const string pathResponse = "path";
            A.CallTo(() => fakefileShareService.SearchIhoCrtFilePath(FakeBatchId, FakeCorrelationId)).Returns(pathResponse);

            var result = await fulfilmentFileShareService.SearchIhoCrtFilePath(FakeBatchId, FakeCorrelationId);

            Assert.That(result, Is.EqualTo(pathResponse));
            A.CallTo(() => fakefileShareService.SearchIhoCrtFilePath(FakeBatchId, FakeCorrelationId)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenDownloadIhoPubFile_ThenReturnsBool(bool isFileDownloaded)
        {
            const string filePath = "path";
            A.CallTo(() => fakefileShareService.DownloadIhoPubFile(filePath, FakeBatchId, FakeAioExchangeSetPath, FakeCorrelationId)).Returns(isFileDownloaded);

            var result = await fulfilmentFileShareService.DownloadIhoPubFile(filePath, FakeBatchId, FakeAioExchangeSetPath, FakeCorrelationId);

            Assert.That(result, Is.EqualTo(isFileDownloaded));
            A.CallTo(() => fakefileShareService.DownloadIhoPubFile(filePath, FakeBatchId, FakeAioExchangeSetPath, FakeCorrelationId)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenDownloadIhoCrtFile_ThenReturnsBool(bool isFileDownloaded)
        {
            const string filePath = "path";
            A.CallTo(() => fakefileShareService.DownloadIhoCrtFile(filePath, FakeBatchId, FakeAioExchangeSetPath, FakeCorrelationId)).Returns(isFileDownloaded);

            var result = await fulfilmentFileShareService.DownloadIhoCrtFile(filePath, FakeBatchId, FakeAioExchangeSetPath, FakeCorrelationId);

            Assert.That(result, Is.EqualTo(isFileDownloaded));
            A.CallTo(() => fakefileShareService.DownloadIhoCrtFile(filePath, FakeBatchId, FakeAioExchangeSetPath, FakeCorrelationId)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenDownloadReadMeFileFromCacheAsync_ThenReturnsBool(bool isFileDownloaded)
        {
            A.CallTo(() => fakefileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchId, FakeExchangeSetEncRootPath, FakeCorrelationId)).Returns(isFileDownloaded);

            var result = await fulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchId, FakeExchangeSetEncRootPath, FakeCorrelationId);

            Assert.That(result, Is.EqualTo(isFileDownloaded));
            A.CallTo(() => fakefileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchId, FakeExchangeSetEncRootPath, FakeCorrelationId)).MustHaveHappenedOnceExactly();
        }
    }
}
