using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.FulfilmentService.Downloads;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Downloads
{
    [TestFixture]
    public class DownloaderTest
    {
        private ILogger<FulfilmentDataService> _fakeLogger;
        private IMonitorHelper _fakeMonitorHelper;
        private IFileSystemHelper _fakeFileSystemHelper;
        private IFulfilmentFileShareService _fakeFulfilmentFileShareService;
        private Downloader _downloader;

        [SetUp]
        public void Setup()
        {
            _fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();
            _fakeMonitorHelper = A.Fake<IMonitorHelper>();
            _fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            _fakeFulfilmentFileShareService = A.Fake<IFulfilmentFileShareService>();

            _downloader = new Downloader(_fakeLogger, _fakeMonitorHelper, _fakeFileSystemHelper, _fakeFulfilmentFileShareService, FakeBatchValue.FileShareServiceConfiguration);
        }

        [Test]
        public async Task DownloadReadMeFileAsync_CacheHit_ReturnsTrue()
        {
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).Returns(true);

            var result = await _downloader.DownloadReadMeFileAsync(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetEncRootPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.True);
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.SearchDownloadReadmeCacheEventStart, "Cache Search and Download readme.txt file started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.SearchDownloadReadmeCacheEventCompleted, "Cache Search and Download readme.txt file completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
        }

        [Test]
        public async Task DownloadReadMeFileAsync_CacheMiss_FssSuccess_ReturnsTrue()
        {
            const string readMeUrl = "/batch/xyz/readme.txt";
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).Returns(false);
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchReadMeFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(readMeUrl);
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromFssAsync(readMeUrl, FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).Returns(true);

            var result = await _downloader.DownloadReadMeFileAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.True);
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchReadMeFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromFssAsync(readMeUrl, FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.SearchDownloadReadmeCacheEventStart, "Cache Search and Download readme.txt file started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.ReadMeTextFileNotFound, "Cache Search and Download readme.txt file not found in blob cache for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceReadMeFileRequestStart, "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceReadMeFileRequestCompleted, "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadReadMeFileRequestStart, "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.DownloadReadMeFileRequestCompleted, "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
        }

        [Test]
        public async Task DownloadReadMeFileAsync_CacheMiss_FssPathNotFound_ReturnsFalse()
        {
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).Returns(false);
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchReadMeFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(string.Empty);

            var result = await _downloader.DownloadReadMeFileAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.False);
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchReadMeFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.SearchDownloadReadmeCacheEventStart, "Cache Search and Download readme.txt file started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.ReadMeTextFileNotFound, "Cache Search and Download readme.txt file not found in blob cache for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceReadMeFileRequestStart, "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceReadMeFileRequestCompleted, "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadReadMeFileRequestStart, "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadReadMeFileRequestCompleted, "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 0);
        }

        [Test]
        public async Task DownloadReadMeFileAsync_CacheMiss_FssDownloadFails_ReturnsFalse()
        {
            const string readMeUrl = "/batch/xyz/readme.txt";
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).Returns(false);
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchReadMeFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(readMeUrl);
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromFssAsync(readMeUrl, FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).Returns(false);

            var result = await _downloader.DownloadReadMeFileAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.False);
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchReadMeFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromFssAsync(readMeUrl, FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.SearchDownloadReadmeCacheEventStart, "Cache Search and Download readme.txt file started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.ReadMeTextFileNotFound, "Cache Search and Download readme.txt file not found in blob cache for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceReadMeFileRequestStart, "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceReadMeFileRequestCompleted, "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadReadMeFileRequestStart, "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.DownloadReadMeFileRequestCompleted, "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
        }

        [Test]
        public async Task DownloadReadMeFileAsync_Exception_ReturnsFalse()
        {
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).Throws<InvalidOperationException>();

            var result = await _downloader.DownloadReadMeFileAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.False);
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.SearchDownloadReadmeCacheEventStart, "Cache Search and Download readme.txt file started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.ErrorInDownloadReadMeFile, "Error while downloading readme.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} and Exception:{Message}", logLevel: LogLevel.Error);
        }

        [Test]
        public async Task DownloadReadMeFileFromFssAsync_PathFound_Success_ReturnsTrue()
        {
            const string readMeUrl = "/batch/xyz/readme.txt";
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchReadMeFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(readMeUrl);
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromFssAsync(readMeUrl, FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).Returns(true);

            var result = await _downloader.DownloadReadMeFileFromFssAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.True);
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchReadMeFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromFssAsync(readMeUrl, FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceReadMeFileRequestStart, "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceReadMeFileRequestCompleted, "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadReadMeFileRequestStart, "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.DownloadReadMeFileRequestCompleted, "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
        }

        [Test]
        public async Task DownloadReadMeFileFromFssAsync_PathFound_DownloadFails_ReturnsFalse()
        {
            const string readMeUrl = "/batch/xyz/readme.txt";
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchReadMeFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(readMeUrl);
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromFssAsync(readMeUrl, FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).Returns(false);

            var result = await _downloader.DownloadReadMeFileFromFssAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.False);
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchReadMeFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromFssAsync(readMeUrl, FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceReadMeFileRequestStart, "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceReadMeFileRequestCompleted, "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadReadMeFileRequestStart, "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.DownloadReadMeFileRequestCompleted, "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
        }

        [Test]
        public async Task DownloadReadMeFileFromFssAsync_PathNotFound_ReturnsFalse()
        {
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchReadMeFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(string.Empty);

            var result = await _downloader.DownloadReadMeFileFromFssAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.False);
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchReadMeFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceReadMeFileRequestStart, "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceReadMeFileRequestCompleted, "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadReadMeFileRequestStart, "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadReadMeFileRequestCompleted, "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 0);
        }

        [Test]
        public async Task DownloadIhoCrtFile_PathFound_Success_ReturnsTrue()
        {
            const string ihoCrtFilePath = "/batch/iho/IHO.crt";
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchIhoCrtFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(ihoCrtFilePath);
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadIhoCrtFile(ihoCrtFilePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);

            var result = await _downloader.DownloadIhoCrtFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.True);
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchIhoCrtFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadIhoCrtFile(ihoCrtFilePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceIhoCrtFileRequestStart, "File share service search query request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceIhoCrtFileRequestCompleted, "File share service search query request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadIhoCrtFileRequestStart, "File share service download request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.DownloadIhoCrtFileRequestCompleted, "File share service download request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
        }

        [Test]
        public async Task DownloadIhoCrtFile_PathFound_Fails_ReturnsFalse()
        {
            const string ihoCrtFilePath = "/batch/iho/IHO.crt";
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchIhoCrtFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(ihoCrtFilePath);
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadIhoCrtFile(ihoCrtFilePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(false);

            var result = await _downloader.DownloadIhoCrtFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.False);
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchIhoCrtFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadIhoCrtFile(ihoCrtFilePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceIhoCrtFileRequestStart, "File share service search query request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceIhoCrtFileRequestCompleted, "File share service search query request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadIhoCrtFileRequestStart, "File share service download request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.DownloadIhoCrtFileRequestCompleted, "File share service download request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
        }

        [Test]
        public async Task DownloadIhoCrtFile_PathNotFound_ReturnsFalse()
        {
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchIhoCrtFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(string.Empty);

            var result = await _downloader.DownloadIhoCrtFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.False);
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchIhoCrtFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadIhoCrtFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceIhoCrtFileRequestStart, "File share service search query request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceIhoCrtFileRequestCompleted, "File share service search query request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadIhoCrtFileRequestStart, "File share service download request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadIhoCrtFileRequestCompleted, "File share service download request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 0);
        }

        [Test]
        public async Task DownloadIhoPubFile_PathFound_Success_ReturnsTrue()
        {
            const string ihoPubFilePath = "/batch/iho/IHO.pub";
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchIhoPubFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(ihoPubFilePath);
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadIhoPubFile(ihoPubFilePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);

            var result = await _downloader.DownloadIhoPubFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.True);
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchIhoPubFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadIhoPubFile(ihoPubFilePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceIhoPubFileRequestStart, "File share service search query request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceIhoPubFileRequestCompleted, "File share service search query request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadIhoPubFileRequestStart, "File share service download request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.DownloadIhoPubFileRequestCompleted, "File share service download request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
        }

        [Test]
        public async Task DownloadIhoPubFile_PathFound_Fails_ReturnsFalse()
        {
            const string ihoPubFilePath = "/batch/iho/IHO.pub";
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchIhoPubFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(ihoPubFilePath);
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadIhoPubFile(ihoPubFilePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(false);

            var result = await _downloader.DownloadIhoPubFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.False);
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchIhoPubFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadIhoPubFile(ihoPubFilePath, FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceIhoPubFileRequestStart, "File share service search query request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceIhoPubFileRequestCompleted, "File share service search query request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadIhoPubFileRequestStart, "File share service download request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.DownloadIhoPubFileRequestCompleted, "File share service download request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
        }

        [Test]
        public async Task DownloadIhoPubFile_PathNotFound_ReturnsFalse()
        {
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchIhoPubFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(string.Empty);

            var result = await _downloader.DownloadIhoPubFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.False);
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchIhoPubFilePath(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadIhoPubFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceIhoPubFileRequestStart, "File share service search query request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceIhoPubFileRequestCompleted, "File share service search query request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadIhoPubFileRequestStart, "File share service download request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadIhoPubFileRequestCompleted, "File share service download request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 0);
        }

        [Test]
        public async Task DownloadInfoFolderFiles_FilesFound_DownloadInvoked()
        {
            var batchFiles = new List<BatchFile> { new() };
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.Info)).Returns(batchFiles);
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, batchFiles, FakeBatchValue.LargeExchangeSetMediaInfoPath5)).Returns(true);

            await _downloader.DownloadInfoFolderFiles(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoPath5, FakeBatchValue.CorrelationId);

            A.CallTo(() => _fakeFulfilmentFileShareService.SearchFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.Info)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, batchFiles, FakeBatchValue.LargeExchangeSetMediaInfoPath5)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.DownloadInfoFolderRequestStart, "File share service search query request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.DownloadInfoFolderRequestCompleted, "File share service search query request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadInfoFolderRequestStart, "File share service download request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.DownloadInfoFolderRequestCompleted, "File share service download request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
        }

        [Test]
        public async Task DownloadInfoFolderFiles_NullFiles_NoDownload()
        {
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.Info)).Returns((IEnumerable<BatchFile>)null);

            await _downloader.DownloadInfoFolderFiles(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoPath5, FakeBatchValue.CorrelationId);

            A.CallTo(() => _fakeFulfilmentFileShareService.SearchFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.Info)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadFolderDetails(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<BatchFile>>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.DownloadInfoFolderRequestStart, "File share service search query request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.DownloadInfoFolderRequestCompleted, "File share service search query request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadInfoFolderRequestStart, "File share service download request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadInfoFolderRequestCompleted, "File share service download request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 0);
        }

        [Test]
        public async Task DownloadInfoFolderFiles_NoFiles_NoDownload()
        {
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.Info)).Returns([]);

            await _downloader.DownloadInfoFolderFiles(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoPath5, FakeBatchValue.CorrelationId);

            A.CallTo(() => _fakeFulfilmentFileShareService.SearchFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.Info)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadFolderDetails(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<BatchFile>>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.DownloadInfoFolderRequestStart, "File share service search query request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.DownloadInfoFolderRequestCompleted, "File share service search query request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadInfoFolderRequestStart, "File share service download request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadInfoFolderRequestCompleted, "File share service download request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 0);
        }

        [Test]
        public async Task DownloadAdcFolderFiles_FilesFound_DownloadInvoked()
        {
            var batchFiles = new List<BatchFile> { new() };
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.Adc)).Returns(batchFiles);
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, batchFiles, FakeBatchValue.LargeExchangeSetMediaInfoAdcPath5)).Returns(true);

            await _downloader.DownloadAdcFolderFiles(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoAdcPath5, FakeBatchValue.CorrelationId);

            A.CallTo(() => _fakeFulfilmentFileShareService.SearchFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.Adc)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, batchFiles, FakeBatchValue.LargeExchangeSetMediaInfoAdcPath5)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceAdcFolderFilesRequestStart, "File share service search query request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceAdcFolderFilesRequestCompleted, "File share service search query request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadAdcFolderFilesStart, "File share service download request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.DownloadAdcFolderFilesCompleted, "File share service download request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
        }

        [Test]
        public async Task DownloadAdcFolderFiles_NullFiles_NoDownload()
        {
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.Adc)).Returns((IEnumerable<BatchFile>)null);

            await _downloader.DownloadAdcFolderFiles(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoAdcPath5, FakeBatchValue.CorrelationId);

            A.CallTo(() => _fakeFulfilmentFileShareService.SearchFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.Adc)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadFolderDetails(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<BatchFile>>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceAdcFolderFilesRequestStart, "File share service search query request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceAdcFolderFilesRequestCompleted, "File share service search query request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadAdcFolderFilesStart, "File share service download request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadAdcFolderFilesCompleted, "File share service download request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 0);
        }

        [Test]
        public async Task DownloadAdcFolderFiles_NoFiles_NoDownload()
        {
            A.CallTo(() => _fakeFulfilmentFileShareService.SearchFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.Adc)).Returns([]);

            await _downloader.DownloadAdcFolderFiles(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoAdcPath5, FakeBatchValue.CorrelationId);

            A.CallTo(() => _fakeFulfilmentFileShareService.SearchFolderDetails(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.Adc)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadFolderDetails(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<BatchFile>>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceAdcFolderFilesRequestStart, "File share service search query request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceAdcFolderFilesRequestCompleted, "File share service search query request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadAdcFolderFilesStart, "File share service download request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.DownloadAdcFolderFilesCompleted, "File share service download request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 0);
        }

        [Test]
        public async Task DownloadLargeMediaReadMeFile_TwoBaseDirectories_InvokesReadmeTwice()
        {
            var b1 = A.Fake<IDirectoryInfo>();
            A.CallTo(() => b1.Name).Returns("B1");
            A.CallTo(() => b1.ToString()).Returns(Path.Combine(FakeBatchValue.LargeExchangeSetMediaPath5, "B1"));

            var b2 = A.Fake<IDirectoryInfo>();
            A.CallTo(() => b2.Name).Returns("B2");
            A.CallTo(() => b2.ToString()).Returns(Path.Combine(FakeBatchValue.LargeExchangeSetMediaPath5, "B2"));

            var unrelated = A.Fake<IDirectoryInfo>();
            A.CallTo(() => unrelated.Name).Returns("Logs");
            A.CallTo(() => unrelated.ToString()).Returns(Path.Combine(FakeBatchValue.LargeExchangeSetMediaPath5, "Logs"));

            var b1EncRoot = Path.Combine(b1.ToString(), FakeBatchValue.EncRoot);
            var b2EncRoot = Path.Combine(b2.ToString(), FakeBatchValue.EncRoot);

            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.LargeExchangeSetMediaPath5)).Returns([b1, b2, unrelated]);

            // Any call inside DownloadReadMeFileAsync -> simulate success
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchValue.BatchId, A<string>.Ignored, FakeBatchValue.CorrelationId)).Returns(true);

            await _downloader.DownloadLargeMediaReadMeFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath5, FakeBatchValue.CorrelationId);

            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.LargeExchangeSetMediaPath5)).MustHaveHappenedOnceExactly();
            // Expect 2 calls (B1 and B2 enc root)
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchValue.BatchId, b1EncRoot, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(FakeBatchValue.BatchId, b2EncRoot, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
        }
    }
}
