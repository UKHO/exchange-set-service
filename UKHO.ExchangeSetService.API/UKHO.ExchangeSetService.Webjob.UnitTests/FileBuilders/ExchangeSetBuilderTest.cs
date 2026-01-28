using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.WebJobs;
using UKHO.ExchangeSetService.FulfilmentService.Downloads;
using UKHO.ExchangeSetService.FulfilmentService.FileBuilders;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.FulfilmentService.Validation;
using UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.FileBuilders
{
    [TestFixture]
    public class ExchangeSetBuilderTest
    {
        private ILogger<FulfilmentDataService> _fakeLogger;
        private IMonitorHelper _fakeMonitorHelper;
        private IFileSystemHelper _fakeFileSystemHelper;
        private IProductDataValidator _fakeProductDataValidator;
        private IFulfilmentFileShareService _fakeFulfilmentFileShareService;
        private IFulfilmentAncillaryFiles _fakeFulfilmentAncillaryFiles;
        private IFileBuilder _fakeFileBuilder;
        private IDownloader _fakeDownloader;
        private ExchangeSetBuilder _exchangeSetBuilder;
        // Replace the invalid field initializers with typed fields and add proper setup/teardown.

        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _cancellationTokenSource?.Dispose();
        }

        private static SalesCatalogueServiceResponseQueueMessage CreateMessage() =>
            new()
            {
                BatchId = FakeBatchValue.BatchId,
                CorrelationId = FakeBatchValue.CorrelationId,
                ScsRequestDateTime = DateTime.UtcNow,
                ExchangeSetStandard = "s63"
            };

        private static List<Products> CreateProducts(int count = 2, string prefix = "GB")
        {
            var list = new List<Products>();

            for (var i = 1; i <= count; i++)
            {
                list.Add(new Products
                {
                    ProductName = $"{prefix}{800000 + i + 1}",
                    EditionNumber = 1,
                    UpdateNumbers = [0],
                    FileSize = 100
                });
            }

            return list;
        }

        private static List<Products> CreateProductsAio()
        {
            return
            [
                new()
                {
                    ProductName = FakeBatchValue.AioCell1,
                    EditionNumber = 1,
                    UpdateNumbers = [0],
                    FileSize = 100
                }
            ];
        }

        private static List<FulfilmentDataResponse> CreateFulfilmentDataResponses(List<Products> products)
        {
            return [.. products.Select((p, idx) => new FulfilmentDataResponse
            {
                BatchId = FakeBatchValue.BatchId,
                ProductName = p.ProductName,
                EditionNumber = p.EditionNumber ?? 1,
                UpdateNumber = p.UpdateNumbers.First() ?? 0,
                Files = new List<BatchFile>(),
                FileShareServiceSearchQueryCount = idx == 0 ? 1 : 0
            })];
        }

        [SetUp]
        public void Setup()
        {
            _fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();
            _fakeMonitorHelper = A.Fake<IMonitorHelper>();
            _fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            _fakeProductDataValidator = A.Fake<IProductDataValidator>();
            _fakeFulfilmentFileShareService = A.Fake<IFulfilmentFileShareService>();
            _fakeFulfilmentAncillaryFiles = A.Fake<IFulfilmentAncillaryFiles>();
            _fakeFileBuilder = A.Fake<IFileBuilder>();
            _fakeDownloader = A.Fake<IDownloader>();

            _exchangeSetBuilder = new ExchangeSetBuilder(_fakeLogger, _fakeMonitorHelper, _fakeFileSystemHelper, _fakeProductDataValidator, FakeBatchValue.FileShareServiceConfiguration, FakeBatchValue.AioConfiguration, _fakeFulfilmentFileShareService, _fakeFulfilmentAncillaryFiles, _fakeFileBuilder, _fakeDownloader);
        }

        [Test]
        public async Task WhenCreateAioExchangeSet_WithProducts_ReturnsTrueAndQueriesFss()
        {
            var message = CreateMessage();
            var batch = new FulfilmentServiceBatch(FakeBatchValue.Configuration, message);
            var products = CreateProductsAio();
            var fulfilmentDataResponses = CreateFulfilmentDataResponses(products);
            var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();

            A.CallTo(() => _fakeFulfilmentFileShareService.QueryFileShareServiceData(A<List<Products>>.Ignored, message, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, FakeBatchValue.AioExchangeSetEncRootPath, FakeBatchValue.S63BusinessUnit)).Returns(fulfilmentDataResponses);
            A.CallTo(() => _fakeFileBuilder.CreateAncillaryFilesForAio(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, message.ScsRequestDateTime, salesCatalogueProductResponse, A<List<FulfilmentDataResponse>>.That.Matches(l => l.Count == fulfilmentDataResponses.Count))).Returns(true);

            var result = await _exchangeSetBuilder.CreateAioExchangeSet(batch, products, salesCatalogueDataResponse, salesCatalogueProductResponse);

            Assert.That(result, Is.True);
            A.CallTo(() => _fakeFulfilmentFileShareService.QueryFileShareServiceData(A<List<Products>>.Ignored, message, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, FakeBatchValue.AioExchangeSetEncRootPath, FakeBatchValue.S63BusinessUnit)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileBuilder.CreateAncillaryFilesForAio(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, message.ScsRequestDateTime, salesCatalogueProductResponse, A<List<FulfilmentDataResponse>>.That.Matches(l => l.Count == fulfilmentDataResponses.Count))).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceENCFilesRequestStart, "File share service search query and download request for ENC files from BusinessUnit:{businessUnit} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceENCFilesRequestCompleted, "File share service search query and download request for ENC files from BusinessUnit:{businessUnit} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
        }

        [Test]
        public async Task WhenCreateAioExchangeSet_WithEmptyProducts_DoesNotQueryFssButStillCreatesAncillary()
        {
            var message = CreateMessage();
            var batch = new FulfilmentServiceBatch(FakeBatchValue.Configuration, message);
            var empty = new List<Products>();
            var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();

            A.CallTo(() => _fakeFileBuilder.CreateAncillaryFilesForAio(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, message.ScsRequestDateTime, salesCatalogueProductResponse, A<List<FulfilmentDataResponse>>.That.Matches(l => l.Count == 0))).Returns(true);

            var result = await _exchangeSetBuilder.CreateAioExchangeSet(batch, empty, salesCatalogueDataResponse, salesCatalogueProductResponse);

            Assert.That(result, Is.True);
            A.CallTo(() => _fakeFulfilmentFileShareService.QueryFileShareServiceData(A<List<Products>>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => _fakeFileBuilder.CreateAncillaryFilesForAio(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, message.ScsRequestDateTime, salesCatalogueProductResponse, A<List<FulfilmentDataResponse>>.That.Matches(l => l.Count == 0))).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceENCFilesRequestStart, "File share service search query and download request for ENC files from BusinessUnit:{businessUnit} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceENCFilesRequestCompleted, "File share service search query and download request for ENC files from BusinessUnit:{businessUnit} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 0);
        }

        [Test]
        public async Task WhenCreateStandardExchangeSet_WithS63BusinessUnit_EncryptionTrue()
        {
            var message = CreateMessage();
            var products = CreateProducts();
            var fulfilmentDataResponses = CreateFulfilmentDataResponses(products);
            var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();

            A.CallTo(() => _fakeFulfilmentFileShareService.QueryFileShareServiceData(A<List<Products>>.Ignored, message, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.S63BusinessUnit)).Returns(fulfilmentDataResponses);

            await _exchangeSetBuilder.CreateStandardExchangeSet(message, salesCatalogueProductResponse, products, FakeBatchValue.ExchangeSetPath, salesCatalogueDataResponse, FakeBatchValue.S63BusinessUnit);

            A.CallTo(() => _fakeFulfilmentFileShareService.QueryFileShareServiceData(A<List<Products>>.Ignored, message, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.S63BusinessUnit)).MustHaveHappened(products.Count, Times.Exactly);
            A.CallTo(() => _fakeFileBuilder.CreateAncillaryFiles(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId, A<List<FulfilmentDataResponse>>.Ignored, salesCatalogueProductResponse, message.ScsRequestDateTime, salesCatalogueDataResponse, true)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceENCFilesRequestStart, "File share service search query and download request for ENC files from BusinessUnit:{businessUnit} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 2);
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceENCFilesRequestCompleted, "File share service search query and download request for ENC files from BusinessUnit:{businessUnit} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 2);
        }

        [Test]
        public async Task WhenCreateStandardExchangeSet_WithS57BusinessUnit_EncryptionFalse()
        {
            var message = CreateMessage();
            var products = CreateProducts();
            var fulfilmentDataResponses = CreateFulfilmentDataResponses(products);
            var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();

            A.CallTo(() => _fakeFulfilmentFileShareService.QueryFileShareServiceData(A<List<Products>>.Ignored, message, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.S57BusinessUnit)).Returns(fulfilmentDataResponses);

            await _exchangeSetBuilder.CreateStandardExchangeSet(message, salesCatalogueProductResponse, products, FakeBatchValue.ExchangeSetPath, salesCatalogueDataResponse, FakeBatchValue.S57BusinessUnit);

            A.CallTo(() => _fakeFulfilmentFileShareService.QueryFileShareServiceData(A<List<Products>>.Ignored, message, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.S57BusinessUnit)).MustHaveHappened(products.Count, Times.Exactly);
            A.CallTo(() => _fakeFileBuilder.CreateAncillaryFiles(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId, A<List<FulfilmentDataResponse>>.Ignored, salesCatalogueProductResponse, message.ScsRequestDateTime, salesCatalogueDataResponse, false)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceENCFilesRequestStart, "File share service search query and download request for ENC files from BusinessUnit:{businessUnit} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 2);
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceENCFilesRequestCompleted, "File share service search query and download request for ENC files from BusinessUnit:{businessUnit} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 2);
        }

        [Test]
        public async Task WhenCreateStandardExchangeSet_WithEmptyProducts_DoesNotQueryFssButStillCreatesAncillary()
        {
            var message = CreateMessage();
            var empty = new List<Products>();
            var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();

            await _exchangeSetBuilder.CreateStandardExchangeSet(message, salesCatalogueProductResponse, empty, FakeBatchValue.ExchangeSetPath, salesCatalogueDataResponse, FakeBatchValue.S57BusinessUnit);

            A.CallTo(() => _fakeFulfilmentFileShareService.QueryFileShareServiceData(A<List<Products>>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => _fakeFileBuilder.CreateAncillaryFiles(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId, A<List<FulfilmentDataResponse>>.Ignored, salesCatalogueProductResponse, message.ScsRequestDateTime, salesCatalogueDataResponse, false)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceENCFilesRequestStart, "File share service search query and download request for ENC files from BusinessUnit:{businessUnit} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.QueryFileShareServiceENCFilesRequestCompleted, "File share service search query and download request for ENC files from BusinessUnit:{businessUnit} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 0);
        }

        [Test]
            public async Task WhenCreateStandardLargeMediaExchangeSet_Valid_ReturnsTrue()
            {
                var message = CreateMessage();
                var batch = new FulfilmentServiceBatch(FakeBatchValue.Configuration, message);
                var products = CreateProducts(1); // one non-AIO cell
                var fulfilmentDataResponses = CreateFulfilmentDataResponses(products);
                var salesCatalogueDataResponse = new SalesCatalogueDataResponse();
                var salesCatalogueProductResponse = new SalesCatalogueProductResponse { Products = products };
                var largeExchangeSetDataResponse = new LargeExchangeSetDataResponse
                {
                    SalesCatalogueProductResponse = salesCatalogueProductResponse,
                    SalesCatalogueDataResponse = salesCatalogueDataResponse
                };

                // Validation passes
                A.CallTo(() => _fakeProductDataValidator.Validate(A<List<Products>>.Ignored)).Returns(Task.FromResult(new ValidationResult()));

                // Query to FSS returns fulfilment data
                A.CallTo(() => _fakeFulfilmentFileShareService.QueryFileShareServiceData(A<List<Products>>.Ignored, message, _cancellationTokenSource, _cancellationToken, FakeBatchValue.LargeExchangeSetEncRootPattern, FakeBatchValue.S63BusinessUnit)).Returns(fulfilmentDataResponses);

                // Root M0* directories
                var m05 = A.Fake<IDirectoryInfo>();
                A.CallTo(() => m05.Name).Returns(FakeBatchValue.LargeExchangeSetFolderName5);
                A.CallTo(() => m05.ToString()).Returns(FakeBatchValue.LargeExchangeSetMediaPath5);
                var m06 = A.Fake<IDirectoryInfo>();
                A.CallTo(() => m06.Name).Returns(FakeBatchValue.LargeExchangeSetFolderName6);
                A.CallTo(() => m06.ToString()).Returns(FakeBatchValue.LargeExchangeSetMediaPath6);

                A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.BatchPath)).Returns([m05, m06]);

                // Ancillary / file builder & downloader calls
                A.CallTo(() => _fakeFulfilmentAncillaryFiles.CreateMediaFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath5, FakeBatchValue.CorrelationId, FakeBatchValue.MediaBaseNumber5)).Returns(Task.FromResult(true));
                A.CallTo(() => _fakeFulfilmentAncillaryFiles.CreateMediaFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath6, FakeBatchValue.CorrelationId, FakeBatchValue.MediaBaseNumber6)).Returns(Task.FromResult(true));
                A.CallTo(() => _fakeDownloader.DownloadLargeMediaReadMeFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath5, FakeBatchValue.CorrelationId)).Returns(Task.CompletedTask);
                A.CallTo(() => _fakeDownloader.DownloadLargeMediaReadMeFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath6, FakeBatchValue.CorrelationId)).Returns(Task.CompletedTask);
                A.CallTo(() => _fakeFileBuilder.CreateLargeMediaSerialEncFile(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.LargeExchangeSetFolderName5, FakeBatchValue.CorrelationId)).Returns(Task.FromResult(true));
                A.CallTo(() => _fakeFileBuilder.CreateLargeMediaSerialEncFile(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.LargeExchangeSetFolderName6, FakeBatchValue.CorrelationId)).Returns(Task.FromResult(true));
                A.CallTo(() => _fakeFileBuilder.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoPath5, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, message.ScsRequestDateTime, true)).Returns(Task.FromResult(true));
                A.CallTo(() => _fakeFileBuilder.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoPath6, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, message.ScsRequestDateTime, true)).Returns(Task.FromResult(true));
                A.CallTo(() => _fakeDownloader.DownloadInfoFolderFiles(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoPath5, FakeBatchValue.CorrelationId)).Returns(Task.CompletedTask);
                A.CallTo(() => _fakeDownloader.DownloadInfoFolderFiles(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoPath6, FakeBatchValue.CorrelationId)).Returns(Task.CompletedTask);
                A.CallTo(() => _fakeDownloader.DownloadAdcFolderFiles(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoAdcPath5, FakeBatchValue.CorrelationId)).Returns(Task.CompletedTask);
                A.CallTo(() => _fakeDownloader.DownloadAdcFolderFiles(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoAdcPath6, FakeBatchValue.CorrelationId)).Returns(Task.CompletedTask);
                A.CallTo(() => _fakeFulfilmentAncillaryFiles.CreateEncUpdateCsv(salesCatalogueDataResponse, FakeBatchValue.LargeExchangeSetMediaInfoPath5, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(Task.FromResult(true));
                A.CallTo(() => _fakeFulfilmentAncillaryFiles.CreateEncUpdateCsv(salesCatalogueDataResponse, FakeBatchValue.LargeExchangeSetMediaInfoPath6, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(Task.FromResult(true));
                A.CallTo(() => _fakeFileBuilder.CreateLargeMediaExchangesetCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath5, FakeBatchValue.CorrelationId, fulfilmentDataResponses, salesCatalogueDataResponse, salesCatalogueProductResponse)).Returns(Task.FromResult(true));
                A.CallTo(() => _fakeFileBuilder.CreateLargeMediaExchangesetCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath6, FakeBatchValue.CorrelationId, fulfilmentDataResponses, salesCatalogueDataResponse, salesCatalogueProductResponse)).Returns(Task.FromResult(true));

                var result = await _exchangeSetBuilder.CreateStandardLargeMediaExchangeSet(batch, largeExchangeSetDataResponse, FakeBatchValue.LargeExchangeSetFolderNamePattern, FakeBatchValue.BatchPath, _cancellationTokenSource, _cancellationToken);

                Assert.That(result, Is.True);
                A.CallTo(() => _fakeFulfilmentFileShareService.QueryFileShareServiceData(A<List<Products>>.Ignored, message, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, FakeBatchValue.LargeExchangeSetEncRootPattern, FakeBatchValue.S63BusinessUnit)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();

                // The following calls are made twice, once per M0* directory
                A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.LargeExchangeSetMediaPath5)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.LargeExchangeSetMediaInfoPath5)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.LargeExchangeSetMediaInfoAdcPath5)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.LargeExchangeSetMediaPath6)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.LargeExchangeSetMediaInfoPath6)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.LargeExchangeSetMediaInfoAdcPath6)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeFulfilmentAncillaryFiles.CreateMediaFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath5, FakeBatchValue.CorrelationId, FakeBatchValue.MediaBaseNumber5)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeFulfilmentAncillaryFiles.CreateMediaFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath6, FakeBatchValue.CorrelationId, FakeBatchValue.MediaBaseNumber6)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeDownloader.DownloadLargeMediaReadMeFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath5, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeDownloader.DownloadLargeMediaReadMeFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath6, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeFileBuilder.CreateLargeMediaSerialEncFile(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.LargeExchangeSetFolderName5, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeFileBuilder.CreateLargeMediaSerialEncFile(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.LargeExchangeSetFolderName6, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeFileBuilder.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoPath5, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, message.ScsRequestDateTime, true)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeFileBuilder.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoPath6, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, message.ScsRequestDateTime, true)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeDownloader.DownloadInfoFolderFiles(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoPath5, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeDownloader.DownloadInfoFolderFiles(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoPath6, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeDownloader.DownloadAdcFolderFiles(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoAdcPath5, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeDownloader.DownloadAdcFolderFiles(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaInfoAdcPath6, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeFulfilmentAncillaryFiles.CreateEncUpdateCsv(salesCatalogueDataResponse, FakeBatchValue.LargeExchangeSetMediaInfoPath5, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeFulfilmentAncillaryFiles.CreateEncUpdateCsv(salesCatalogueDataResponse, FakeBatchValue.LargeExchangeSetMediaInfoPath6, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeFileBuilder.CreateLargeMediaExchangesetCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath5, FakeBatchValue.CorrelationId, fulfilmentDataResponses, salesCatalogueDataResponse, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _fakeFileBuilder.CreateLargeMediaExchangesetCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath6, FakeBatchValue.CorrelationId, fulfilmentDataResponses, salesCatalogueDataResponse, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();

                _fakeLogger.VerifyLogEntry(EventIds.LargeExchangeSetCreatedWithError, "Large media exchange set is not created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", logLevel: LogLevel.Error, times: 0);
                _fakeLogger.VerifyLogEntry(EventIds.LargeExchangeSetCreatedWithError, "Operation Cancelled as product validation failed for BatchId:{BatchId}, _X-Correlation-ID:{CorrelationId} and Validation message :{Message}", logLevel: LogLevel.Error, times: 0);
            }

        [Test]
        public void WhenCreateStandardLargeMediaExchangeSet_ProductValidationFails_ThrowsFulfilmentException()
        {
            var message = CreateMessage();
            var batch = new FulfilmentServiceBatch(FakeBatchValue.Configuration, message);
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse { Products = CreateProducts(1) };
            var largeResponse = new LargeExchangeSetDataResponse
            {
                SalesCatalogueProductResponse = salesCatalogueProductResponse,
                SalesCatalogueDataResponse = new SalesCatalogueDataResponse()
            };

            // Validation returns error
            var validationResult = new ValidationResult(new List<ValidationFailure> { new("ProductName", "Invalid product") });

            A.CallTo(() => _fakeProductDataValidator.Validate(A<List<Products>>.Ignored)).Returns(Task.FromResult(validationResult));

            Assert.ThrowsAsync<FulfilmentException>(async () => await _exchangeSetBuilder.CreateStandardLargeMediaExchangeSet(batch, largeResponse, FakeBatchValue.LargeExchangeSetFolderNamePattern, FakeBatchValue.BatchPath, _cancellationTokenSource, _cancellationToken));

            _fakeLogger.VerifyLogEntry(EventIds.LargeExchangeSetCreatedWithError, "Large media exchange set is not created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", logLevel: LogLevel.Error);
            _fakeLogger.VerifyLogEntry(EventIds.LargeExchangeSetCreatedWithError, "Operation Cancelled as product validation failed for BatchId:{BatchId}, _X-Correlation-ID:{CorrelationId} and Validation message :{Message}", logLevel: LogLevel.Error);
        }

        [Test]
        public async Task WhenQueryFileShareServiceFiles_ReturnsFulfilmentData()
        {
            var message = CreateMessage();
            var products = CreateProducts(1);
            var fulfilmentDataResponses = CreateFulfilmentDataResponses(products);
            var cancellationTokenSource = new CancellationTokenSource();

            A.CallTo(() => _fakeFulfilmentFileShareService.QueryFileShareServiceData(products, message, cancellationTokenSource, cancellationTokenSource.Token, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.S63BusinessUnit)).Returns(fulfilmentDataResponses);

            var result = await _exchangeSetBuilder.QueryFileShareServiceFiles(message, products, FakeBatchValue.ExchangeSetEncRootPath, cancellationTokenSource, cancellationTokenSource.Token, FakeBatchValue.S63BusinessUnit);

            Assert.That(result, Is.EqualTo(fulfilmentDataResponses));
            A.CallTo(() => _fakeFulfilmentFileShareService.QueryFileShareServiceData(products, message, cancellationTokenSource, cancellationTokenSource.Token, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.S63BusinessUnit)).MustHaveHappenedOnceExactly();
        }
    }
}
