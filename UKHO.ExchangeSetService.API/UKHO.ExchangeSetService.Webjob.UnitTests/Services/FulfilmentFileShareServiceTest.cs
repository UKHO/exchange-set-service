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
        private IOptions<FileShareServiceConfiguration> _fakefileShareServiceConfig;
        private IFileShareService _fakefileShareService;
        private FulfilmentFileShareService _fulfilmentFileShareService;
        private ILogger<FulfilmentFileShareService> _fakeLogger;
        private const string FakeExchangeSetRootPath = @"D:\\Downloads\";
        private const string FakeBatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
        private const string FakeMediaFolderName = "M01X01.zip";
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private const string FakeExchangeSetZipPath = @"D:\Home\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();

        [SetUp]
        public void Setup()
        {
            _fakefileShareService = A.Fake<IFileShareService>();
            _fakefileShareServiceConfig = Options.Create(new FileShareServiceConfiguration()
            {
                Limit = 100,
                Start = 0,
                ProductLimit = 4,
                UpdateNumberLimit = 10,
                EncRoot = "ENC_ROOT",
                ExchangeSetFileFolder = "V01X01",
                Info = "INFO",
                ProductType = "ProductType",
                S63BusinessUnit = "ADDS",
                ContentInfo = "DVD INFO",
                Content = "Catalogue",
                Adc = "ADC"
            });
            _fakeLogger = A.Fake<ILogger<FulfilmentFileShareService>>();

            _fulfilmentFileShareService = new FulfilmentFileShareService(_fakefileShareServiceConfig, _fakefileShareService, _fakeLogger);
        }

        private static List<Products> GetProductdetails()
        {
            return [
                new Products {
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
                    new BatchDetail {
                        BatchId ="63d38bde-5191-4a59-82d5-aa22ca1cc6dc",
                        BusinessUnit = businessUnit
                    } ],
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
            A.CallTo(() => _fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetSearchBatchResponse(businessUnit));

            var result = await _fulfilmentFileShareService.QueryFileShareServiceData(GetProductdetails(), GetScsResponseQueueMessage(), null, CancellationToken.None, string.Empty, businessUnit);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf(typeof(List<FulfilmentDataResponse>)));
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public async Task WhenRequestQueryFileShareServiceData_ThenReturnsFulfilmentDataNullResponse(string businessUnit)
        {
            A.CallTo(() => _fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetSearchBatchResponse(businessUnit));

            var result = await _fulfilmentFileShareService.QueryFileShareServiceData(null, GetScsResponseQueueMessage(), null, CancellationToken.None, string.Empty, businessUnit);

            Assert.That(result, Is.Null);
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public void WhenIsCancellationRequestedinQueryFileShareServiceData_ThenThrowCancelledException(string businessUnit)
        {
            A.CallTo(() => _fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetSearchBatchResponse(businessUnit));

            _cancellationTokenSource.Cancel();
            var cancellationToken = _cancellationTokenSource.Token;
            Assert.ThrowsAsync<OperationCanceledException>(async () => await _fulfilmentFileShareService.QueryFileShareServiceData(GetProductdetails(), GetScsResponseQueueMessage(), _cancellationTokenSource, cancellationToken, string.Empty, businessUnit));
        }

        [Test]
        public async Task WhenValidSearchReadMeFileRequest_ThenReturnFilePath()
        {
            const string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            const string exchangeSetRootPath = @"batch/" + batchId + "/files/README.TXT";
            A.CallTo(() => _fakefileShareService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(exchangeSetRootPath);

            var result = await _fulfilmentFileShareService.SearchReadMeFilePath(batchId, null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public async Task WhenInvalidSearchReadMeFileRequest_ThenReturnEmptyFilePath()
        {
            var exchangeSetRootPath = string.Empty;
            var batchId = Guid.NewGuid().ToString();
            A.CallTo(() => _fakefileShareService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(exchangeSetRootPath);

            var result = await _fulfilmentFileShareService.SearchReadMeFilePath(batchId, null);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task WhenRequestDownloadReadMeFile_ThenReturnsTrueIfFileIsDownloaded()
        {
            const bool isFileDownloaded = true;
            const string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            const string exchangeSetRootPath = @"D:\\Downloads";
            const string filePath = "TestFilePath";
            A.CallTo(() => _fakefileShareService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isFileDownloaded);

            var result = await _fulfilmentFileShareService.DownloadReadMeFileFromFssAsync(filePath, batchId, exchangeSetRootPath, null);
            Assert.That(result, Is.EqualTo(isFileDownloaded));
        }

        [Test]
        public async Task WhenRequestDownloadReadMeFile_ThenReturnsFalseIfFileIsNotDownloaded()
        {
            const bool isFileDownloaded = false;
            const string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            const string filePath = "TestFilePath";
            A.CallTo(() => _fakefileShareService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isFileDownloaded);

            var result = await _fulfilmentFileShareService.DownloadReadMeFileFromFssAsync(filePath, batchId, FakeExchangeSetRootPath, null);
            Assert.That(result, Is.EqualTo(isFileDownloaded));
        }

        [Test]
        public async Task WhenValidUploadZipFileRequest_ThenReturnTrue()
        {
            const bool isFileUploaded = true;
            A.CallTo(() => _fakefileShareService.UploadFileToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isFileUploaded);

            var result = await _fulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchId, FakeExchangeSetRootPath, null, _fakefileShareServiceConfig.Value.ExchangeSetFileName);
            Assert.That(result, Is.EqualTo(isFileUploaded));
        }

        [Test]
        public async Task WhenInvalidUploadZipFileRequest_ThenReturnFalse()
        {
            const bool isFileUploaded = false;
            A.CallTo(() => _fakefileShareService.UploadFileToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isFileUploaded);

            var result = await _fulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchId, FakeExchangeSetRootPath, null, _fakefileShareServiceConfig.Value.ExchangeSetFileName);
            Assert.That(result, Is.EqualTo(isFileUploaded));
        }

        [Test]
        public async Task WhenValidCreateZipFileRequest_ThenReturnTrue()
        {
            const bool isZipFileCreated = true;
            A.CallTo(() => _fakefileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isZipFileCreated);

            var result = await _fulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchId, FakeExchangeSetRootPath, null);
            Assert.That(result, Is.EqualTo(isZipFileCreated));
        }

        [Test]
        public async Task WhenInvalidCreateZipFileRequest_ThenReturnFalse()
        {
            const bool isZipFileCreated = false;
            A.CallTo(() => _fakefileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isZipFileCreated);

            var result = await _fulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchId, string.Empty, null);
            Assert.That(result, Is.EqualTo(isZipFileCreated));
        }

        #region LargeMediaExchangeSet

        [Test]
        public async Task WhenValidUploadZipFileForLargeMediaExchangeSetToFileShareService_ThenReturnTrue()
        {
            const bool isFileUploaded = true;
            A.CallTo(() => _fakefileShareService.UploadLargeMediaFileToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isFileUploaded);

            var result = await _fulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(FakeBatchId, FakeExchangeSetRootPath, null, FakeMediaFolderName);
            Assert.That(result, Is.EqualTo(isFileUploaded));
        }

        [Test]
        public async Task WhenInvalidUploadZipFileForLargeMediaExchangeSetToFileShareService_ThenReturnFalse()
        {
            const bool isFileUploaded = false;
            A.CallTo(() => _fakefileShareService.UploadLargeMediaFileToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isFileUploaded);

            var result = await _fulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(FakeBatchId, string.Empty, null, FakeMediaFolderName);
            Assert.That(result, Is.EqualTo(isFileUploaded));
        }

        [Test]
        public async Task WhenValidCommitLargeMediaExchangeSet_ThenReturnTrue()
        {
            const bool isBatchCommitted = true;
            A.CallTo(() => _fakefileShareService.CommitAndGetBatchStatusForLargeMediaExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isBatchCommitted);

            var result = await _fulfilmentFileShareService.CommitLargeMediaExchangeSet(FakeBatchId, FakeExchangeSetRootPath, null);
            Assert.That(result, Is.EqualTo(isBatchCommitted));
        }

        [Test]
        public async Task WhenInvalidCommitLargeMediaExchangeSet_ThenReturnFalse()
        {
            const bool isBatchCommitted = false;
            A.CallTo(() => _fakefileShareService.CommitAndGetBatchStatusForLargeMediaExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isBatchCommitted);

            var result = await _fulfilmentFileShareService.CommitLargeMediaExchangeSet(FakeBatchId, FakeExchangeSetRootPath, null);
            Assert.That(result, Is.EqualTo(isBatchCommitted));
        }

        #endregion

        #region SearchFolderFile
        [Test]
        public async Task WhenValidSearchFolderFileRequest_ThenReturnFilePath()
        {
            var batchFileList = new List<BatchFile> {
                new() {  Filename = "TPNMS Diagrams.zip", FileSize = 400, Links = new Links { Get = new Link { Href = "" } } }
            };

            A.CallTo(() => _fakefileShareService.SearchFolderDetails(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(batchFileList);
            var batchInfoResult = await _fulfilmentFileShareService.SearchFolderDetails(FakeBatchId, null, _fakefileShareServiceConfig.Value.Info);
            var batchAdcResult = await _fulfilmentFileShareService.SearchFolderDetails(FakeBatchId, null, _fakefileShareServiceConfig.Value.Adc);

            Assert.Multiple(() =>
            {
                Assert.That(batchInfoResult, Is.EqualTo(batchFileList));
                Assert.That(batchAdcResult, Is.EqualTo(batchFileList));
            });
        }

        [Test]
        public async Task WhenInvalidSearchFolderFileRequest_ThenReturnEmptyFileList()
        {
            var batchFileList = new List<BatchFile>() { };

            A.CallTo(() => _fakefileShareService.SearchFolderDetails(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(batchFileList);

            var batchInfoResult = await _fulfilmentFileShareService.SearchFolderDetails(FakeBatchId, null, _fakefileShareServiceConfig.Value.Info);
            var batchAdcResult = await _fulfilmentFileShareService.SearchFolderDetails(FakeBatchId, null, _fakefileShareServiceConfig.Value.Adc);

            Assert.Multiple(() =>
            {
                Assert.That(batchInfoResult, Is.Empty);
                Assert.That(batchAdcResult, Is.Empty);
            });
        }

        #endregion SearchFolderFile

        #region DownloadFolderDetails
        [Test]
        public async Task WhenRequestDownloadFolderDetails_ThenReturnsTrueIfFileIsDownloaded()
        {
            const bool isFileDownloaded = true;
            var batchFileList = new List<BatchFile> {
                new() {  Filename = "TPNMS Diagrams.zip", FileSize = 400, Links = new Links { Get = new Link { Href = "" } } }
            };

            A.CallTo(() => _fakefileShareService.DownloadFolderDetails(A<string>.Ignored, A<string>.Ignored, A<List<BatchFile>>.Ignored, A<string>.Ignored)).Returns(isFileDownloaded);

            var result = await _fulfilmentFileShareService.DownloadFolderDetails(FakeExchangeSetRootPath, FakeBatchId, batchFileList, null);

            Assert.That(result, Is.EqualTo(isFileDownloaded));
        }

        [Test]
        public async Task WhenRequestDownloadFolderDetails_ThenReturnsFalseIfFileIsNotDownloaded()
        {
            const bool isFileDownloaded = false;
            var batchFileList = new List<BatchFile> {
                new() {  Filename = "TPNMS Diagrams.zip", FileSize = 400, Links = new Links { Get = new Link { Href = "" } } }
            };

            A.CallTo(() => _fakefileShareService.DownloadFolderDetails(A<string>.Ignored, A<string>.Ignored, A<List<BatchFile>>.Ignored, A<string>.Ignored)).Returns(isFileDownloaded);

            var result = await _fulfilmentFileShareService.DownloadFolderDetails(FakeExchangeSetRootPath, FakeBatchId, batchFileList, null);

            Assert.That(result, Is.EqualTo(isFileDownloaded));
        }

        #endregion DownloadFolderDetails

        [Test]
        public async Task WhenRequestCommitExchangeSet_ThenReturnsTrueIfBatchCommitted()
        {
            A.CallTo(() => _fakefileShareService.CommitBatchToFss(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);

            var result = await _fulfilmentFileShareService.CommitExchangeSet(FakeBatchId, _fakeCorrelationId, FakeExchangeSetZipPath);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task WhenRequestCommitExchangeSet_ThenReturnsFalseIfBatchNotCommitted()
        {
            A.CallTo(() => _fakefileShareService.CommitBatchToFss(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(false);

            var result = await _fulfilmentFileShareService.CommitExchangeSet(FakeBatchId, _fakeCorrelationId, FakeExchangeSetZipPath);

            Assert.That(result, Is.False);
        }
    }
}
