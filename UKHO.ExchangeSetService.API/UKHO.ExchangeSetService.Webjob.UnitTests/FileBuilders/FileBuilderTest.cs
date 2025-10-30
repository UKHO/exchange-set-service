using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Downloads;
using UKHO.ExchangeSetService.FulfilmentService.FileBuilders;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.FileBuilders
{
    [TestFixture]
    public class FileBuilderTest
    {
        private ILogger<FulfilmentDataService> _fakeLogger;
        private IMonitorHelper _fakeMonitorHelper;
        private IFileSystemHelper _fakeFileSystemHelper;
        private IDownloader _downloader;
        private IFulfilmentAncillaryFiles _fulfilmentAncillaryFiles;
        private FileBuilder _fileBuilder;

        [SetUp]
        public void Setup()
        {
            _fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();
            _fakeMonitorHelper = A.Fake<IMonitorHelper>();
            _fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            _downloader = A.Fake<IDownloader>();
            _fulfilmentAncillaryFiles = A.Fake<IFulfilmentAncillaryFiles>();

            _fileBuilder = new FileBuilder(_fakeLogger, _fakeMonitorHelper, _fakeFileSystemHelper, _downloader, _fulfilmentAncillaryFiles, FakeBatchValue.FileShareServiceConfiguration);
        }

        [Test]
        public async Task WhenValidCreateAncillaryFilesForAio_ThenReturnTrue()
        {
            var scsRequestDateTime = DateTime.UtcNow;
            var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();
            var listFulfilmentAioData = new List<FulfilmentDataResponse>();
            A.CallTo(() => _downloader.DownloadReadMeFileAsync(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => _downloader.DownloadIhoCrtFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => _downloader.DownloadIhoPubFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateSerialAioFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse)).Returns(true);
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetInfoPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, scsRequestDateTime, true)).Returns(true);
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetEncRootPath, FakeBatchValue.CorrelationId, listFulfilmentAioData, salesCatalogueDataResponse, salesCatalogueProductResponse)).Returns(true);

            var result = await _fileBuilder.CreateAncillaryFilesForAio(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, scsRequestDateTime, salesCatalogueProductResponse, listFulfilmentAioData);

            Assert.That(result, Is.True);
            A.CallTo(() => _downloader.DownloadReadMeFileAsync(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _downloader.DownloadIhoCrtFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _downloader.DownloadIhoPubFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateSerialAioFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetInfoPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, scsRequestDateTime, true)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetEncRootPath, FakeBatchValue.CorrelationId, listFulfilmentAioData, salesCatalogueDataResponse, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidCreateSerialAioFile_ThenReturnBool(bool expectedResponse)
        {
            var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateSerialAioFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse)).Returns(expectedResponse);

            var result = await _fileBuilder.CreateSerialAioFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse);

            Assert.That(result, Is.EqualTo(expectedResponse));
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateSerialAioFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidCreateProductFileForAio_ThenReturnBool(bool expectedResponse)
        {
            var scsRequestDateTime = DateTime.UtcNow;
            var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetInfoPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, scsRequestDateTime, true)).Returns(expectedResponse);

            var result = await _fileBuilder.CreateProductFileForAio(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetInfoPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, scsRequestDateTime);

            Assert.That(result, Is.EqualTo(expectedResponse));
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetInfoPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, scsRequestDateTime, true)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidCreateCatalogFileForAio_ThenReturnBool(bool expectedResponse)
        {
            var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();
            var listFulfilmentAioData = new List<FulfilmentDataResponse>();
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetEncRootPath, FakeBatchValue.CorrelationId, listFulfilmentAioData, salesCatalogueDataResponse, salesCatalogueProductResponse)).Returns(expectedResponse);

            var result = await _fileBuilder.CreateCatalogFileForAio(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetEncRootPath, FakeBatchValue.CorrelationId, listFulfilmentAioData, salesCatalogueDataResponse, salesCatalogueProductResponse);

            Assert.That(result, Is.EqualTo(expectedResponse));
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetEncRootPath, FakeBatchValue.CorrelationId, listFulfilmentAioData, salesCatalogueDataResponse, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenCreateLargeMediaSerialEncFile_ReturnsBool(bool secondResponse)
        {
            var mediaDir1 = A.Fake<IDirectoryInfo>();
            A.CallTo(() => mediaDir1.Name).Returns(FakeBatchValue.LargeExchangeSetFolderName5);
            A.CallTo(() => mediaDir1.ToString()).Returns(FakeBatchValue.LargeExchangeSetMediaPath5);
            var mediaDir2 = A.Fake<IDirectoryInfo>();
            A.CallTo(() => mediaDir2.Name).Returns(FakeBatchValue.LargeExchangeSetFolderName6);
            A.CallTo(() => mediaDir2.ToString()).Returns(FakeBatchValue.LargeExchangeSetMediaPath6);
            var mediaDir3 = A.Fake<IDirectoryInfo>();
            A.CallTo(() => mediaDir3.Name).Returns("Not an M directory");
            A.CallTo(() => mediaDir3.ToString()).Returns(Path.Combine(FakeBatchValue.BatchPath, "Not an M directory"));

            var baseDir1 = A.Fake<IDirectoryInfo>();
            A.CallTo(() => baseDir1.Name).Returns("B1");
            A.CallTo(() => baseDir1.ToString()).Returns(Path.Combine(FakeBatchValue.LargeExchangeSetMediaPath5, "B1"));
            var baseDir2 = A.Fake<IDirectoryInfo>();
            A.CallTo(() => baseDir2.Name).Returns("B2");
            A.CallTo(() => baseDir2.ToString()).Returns(Path.Combine(FakeBatchValue.LargeExchangeSetMediaPath5, "B2"));

            var lastBaseDir1 = A.Fake<IDirectoryInfo>();
            A.CallTo(() => lastBaseDir1.Name).Returns("B3");
            A.CallTo(() => lastBaseDir1.ToString()).Returns(Path.Combine(FakeBatchValue.LargeExchangeSetMediaPath6, "B3"));

            // Root directories (to find last M0*)
            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.BatchPath)).Returns([mediaDir1, mediaDir2, mediaDir3]);

            // Base directories under rootfolder
            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.LargeExchangeSetMediaPath5)).Returns([baseDir1, baseDir2]);

            // For determining last base directory
            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.LargeExchangeSetMediaPath6)).Returns([lastBaseDir1]);

            A.CallTo(() => _fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(FakeBatchValue.BatchId, baseDir1.ToString(), FakeBatchValue.CorrelationId, "1", "3")).Returns(true);
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(FakeBatchValue.BatchId, baseDir2.ToString(), FakeBatchValue.CorrelationId, "2", "3")).Returns(secondResponse);

            var result = await _fileBuilder.CreateLargeMediaSerialEncFile(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.LargeExchangeSetFolderName5, FakeBatchValue.CorrelationId);

            Assert.That(result, Is.EqualTo(secondResponse));
            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.LargeExchangeSetMediaPath5)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.LargeExchangeSetMediaPath6)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(FakeBatchValue.BatchId, baseDir1.ToString(), FakeBatchValue.CorrelationId, "1", "3")).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(FakeBatchValue.BatchId, baseDir2.ToString(), FakeBatchValue.CorrelationId, "2", "3")).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenCreateLargeMediaExchangesetCatalogFile_AllTrue_ReturnsTrue(bool secondResponse)
        {
            var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();

            var baseDir1 = A.Fake<IDirectoryInfo>();
            A.CallTo(() => baseDir1.Name).Returns("B1");
            A.CallTo(() => baseDir1.ToString()).Returns(Path.Combine(FakeBatchValue.LargeExchangeSetMediaPath5, "B1"));
            var baseDir2 = A.Fake<IDirectoryInfo>();
            A.CallTo(() => baseDir2.Name).Returns("B2");
            A.CallTo(() => baseDir2.ToString()).Returns(Path.Combine(FakeBatchValue.LargeExchangeSetMediaPath5, "B2"));
            var baseDir3 = A.Fake<IDirectoryInfo>();
            A.CallTo(() => baseDir3.Name).Returns("Not a base directory");
            A.CallTo(() => baseDir3.ToString()).Returns(Path.Combine(FakeBatchValue.LargeExchangeSetMediaPath5, "Not a base directory"));

            // Base directories at exchangeSetPath
            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.LargeExchangeSetMediaPath5)).Returns([baseDir1, baseDir2, baseDir3]);

            // ENC_ROOT folders under each base
            var encRoot1 = Path.Combine(baseDir1.ToString(), FakeBatchValue.EncRoot);
            var encRoot2 = Path.Combine(baseDir2.ToString(), FakeBatchValue.EncRoot);

            // Country code directories (names whose last 2 chars used)
            var d1a = A.Fake<IDirectoryInfo>(); A.CallTo(() => d1a.Name).Returns("XXXGB");
            var d1b = A.Fake<IDirectoryInfo>(); A.CallTo(() => d1b.Name).Returns("YYYFR");
            var d2a = A.Fake<IDirectoryInfo>(); A.CallTo(() => d2a.Name).Returns("ZZZDE");

            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(encRoot1)).Returns([d1a, d1b]);
            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(encRoot2)).Returns([d2a]);

            var listFulfilmentData = new List<FulfilmentDataResponse>
            {
                new() { ProductName = "GB1234" },
                new() { ProductName = "FR5678" },
                new() { ProductName = "DE9999" },
                new() { ProductName = "NO0001" } // should not match
            };

            A.CallTo(() => _fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(
                FakeBatchValue.BatchId, encRoot1, FakeBatchValue.CorrelationId,
                A<List<FulfilmentDataResponse>>.That.Matches(l => l.Count == 2 && l.Any(p => p.ProductName.StartsWith("GB")) && l.Any(p => p.ProductName.StartsWith("FR"))),
                salesCatalogueDataResponse, salesCatalogueProductResponse)).Returns(true);

            A.CallTo(() => _fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(
                FakeBatchValue.BatchId, encRoot2, FakeBatchValue.CorrelationId,
                A<List<FulfilmentDataResponse>>.That.Matches(l => l.Count == 1 && l.Single().ProductName.StartsWith("DE")),
                salesCatalogueDataResponse, salesCatalogueProductResponse)).Returns(secondResponse);

            var result = await _fileBuilder.CreateLargeMediaExchangesetCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath5, FakeBatchValue.CorrelationId, listFulfilmentData, salesCatalogueDataResponse, salesCatalogueProductResponse);

            Assert.That(result, Is.EqualTo(secondResponse));
            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.LargeExchangeSetMediaPath5)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(
                FakeBatchValue.BatchId, encRoot1, FakeBatchValue.CorrelationId,
                A<List<FulfilmentDataResponse>>.That.Matches(l => l.Count == 2 && l.Any(p => p.ProductName.StartsWith("GB")) && l.Any(p => p.ProductName.StartsWith("FR"))),
                salesCatalogueDataResponse, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(
                FakeBatchValue.BatchId, encRoot2, FakeBatchValue.CorrelationId,
                A<List<FulfilmentDataResponse>>.That.Matches(l => l.Count == 1 && l.Single().ProductName.StartsWith("DE")),
                salesCatalogueDataResponse, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenCreateCatalogFileCalled_ThenReturnsExpected(bool expected)
        {
            var listFulfilmentData = new List<FulfilmentDataResponse>();
            var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId, listFulfilmentData, salesCatalogueDataResponse, salesCatalogueProductResponse)).Returns(expected);

            var result = await _fileBuilder.CreateCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId, listFulfilmentData, salesCatalogueDataResponse, salesCatalogueProductResponse);

            Assert.That(result, Is.EqualTo(expected));
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId, listFulfilmentData, salesCatalogueDataResponse, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenCreateCatalogFileWithEmptyPath_ThenReturnsFalse()
        {
            var listFulfilmentData = new List<FulfilmentDataResponse>();
            var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();

            var result = await _fileBuilder.CreateCatalogFile(FakeBatchValue.BatchId, string.Empty, FakeBatchValue.CorrelationId, listFulfilmentData, salesCatalogueDataResponse, salesCatalogueProductResponse);

            Assert.That(result, Is.False);
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).MustNotHaveHappened();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenCreateProductFileCalled_ThenReturnsExpected(bool expected)
        {
            var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
            var scsRequestDateTime = DateTime.UtcNow;
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetInfoPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, scsRequestDateTime, true)).Returns(expected);

            var result = await _fileBuilder.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetInfoPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, scsRequestDateTime, true);

            Assert.That(result, Is.EqualTo(expected));
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetInfoPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, scsRequestDateTime, true)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenCreateProductFileWithEmptyPath_ThenReturnsFalse()
        {
            var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
            var scsRequestDateTime = DateTime.UtcNow;

            var result = await _fileBuilder.CreateProductFile(FakeBatchValue.BatchId, string.Empty, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, scsRequestDateTime, false);

            Assert.That(result, Is.False);
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<DateTime>.Ignored, A<bool>.Ignored)).MustNotHaveHappened();
        }

        [Test]
        public async Task WhenCreateSerialEncFileCalled_ThenUnderlyingAncillaryIsCalled()
        {
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateSerialEncFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);

            await _fileBuilder.CreateSerialEncFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId);

            A.CallTo(() => _fulfilmentAncillaryFiles.CreateSerialEncFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenCreateAncillaryFilesWithEncryptionTrue_AllExpectedDependenciesCalled()
        {
            var listFulfilmentData = new List<FulfilmentDataResponse>();
            var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();
            var scsRequestDateTime = DateTime.UtcNow;
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetInfoPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, scsRequestDateTime, true)).Returns(true);
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateSerialEncFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => _downloader.DownloadReadMeFileAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId, listFulfilmentData, salesCatalogueDataResponse, salesCatalogueProductResponse)).Returns(true);

            await _fileBuilder.CreateAncillaryFiles(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId, listFulfilmentData, salesCatalogueProductResponse, scsRequestDateTime, salesCatalogueDataResponse, true);

            A.CallTo(() => _fulfilmentAncillaryFiles.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetInfoPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, scsRequestDateTime, true)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateSerialEncFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _downloader.DownloadReadMeFileAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId, listFulfilmentData, salesCatalogueDataResponse, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenCreateAncillaryFilesWithEncryptionFalse_SerialEncSkipped()
        {
            var listFulfilmentData = new List<FulfilmentDataResponse>();
            var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();
            var scsRequestDateTime = DateTime.UtcNow;
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetInfoPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, scsRequestDateTime, false)).Returns(true);
            A.CallTo(() => _downloader.DownloadReadMeFileAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId, listFulfilmentData, salesCatalogueDataResponse, salesCatalogueProductResponse)).Returns(true);

            await _fileBuilder.CreateAncillaryFiles(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId, listFulfilmentData, salesCatalogueProductResponse, scsRequestDateTime, salesCatalogueDataResponse, false);

            A.CallTo(() => _fulfilmentAncillaryFiles.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetInfoPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, scsRequestDateTime, false)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateSerialEncFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => _downloader.DownloadReadMeFileAsync(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fulfilmentAncillaryFiles.CreateCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId, listFulfilmentData, salesCatalogueDataResponse, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log" && call.GetArgument<EventId>(1) == EventIds.SerialFileCreationSkipped.ToEventId()).MustHaveHappenedOnceExactly();
        }
    }
}
