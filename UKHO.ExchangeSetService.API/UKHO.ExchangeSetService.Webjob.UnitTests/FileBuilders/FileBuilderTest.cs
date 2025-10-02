using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Helpers;
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

        //Task<bool> CreateLargeMediaSerialEncFile(string batchId, string exchangeSetPath, string rootfolder, string correlationId);

        //public async Task<bool> CreateLargeMediaSerialEncFile(string batchId, string exchangeSetPath, string rootfolder, string correlationId)
        //{
        //    DateTime createLargeMediaSerialEncFileTaskStartedAt = DateTime.UtcNow;

        //    return await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateSerialFileRequestStart,
        //              EventIds.CreateSerialFileRequestCompleted,
        //              "Create large media serial enc file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
        //              async () =>
        //              {
        //                  var rootLastDirectoryPath = fileSystemHelper.GetDirectoryInfo(exchangeSetPath)
        //                                          .LastOrDefault(di => di.Name.StartsWith("M0"));

        //                  var baseDirectoryies = fileSystemHelper.GetDirectoryInfo(Path.Combine(exchangeSetPath, rootfolder))
        //                                          .Where(di => di.Name.StartsWith("B") && di.Name.Length <= 3 && CommonHelper.IsNumeric(di.Name[^(di.Name.Length - 1)..]));

        //                  var baseLastDirectory = fileSystemHelper.GetDirectoryInfo(rootLastDirectoryPath?.ToString())
        //                                          .LastOrDefault(di => di.Name.StartsWith("B") && di.Name.Length <= 3 && CommonHelper.IsNumeric(di.Name[^(di.Name.Length - 1)..]));

        //                  string lastBaseDirectoryNumber = baseLastDirectory.ToString().Replace(Path.Combine(rootLastDirectoryPath.ToString(), "B"), "");

        //                  var ParallelBaseFolderTasks = new List<Task<bool>> { };
        //                  Parallel.ForEach(baseDirectoryies, baseDirectoryFolder =>
        //                  {
        //                      string baseDirectoryNumber = baseDirectoryFolder.ToString().Replace(Path.Combine(exchangeSetPath, rootfolder, "B"), "");
        //                      ParallelBaseFolderTasks.Add(fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(batchId, baseDirectoryFolder.ToString(), correlationId, baseDirectoryNumber.ToString(), lastBaseDirectoryNumber));
        //                  });
        //                  await Task.WhenAll(ParallelBaseFolderTasks);

        //                  DateTime createLargeMediaSerialEncFileTaskCompletedAt = DateTime.UtcNow;
        //                  monitorHelper.MonitorRequest("Create Large Media Serial Enc File Task", createLargeMediaSerialEncFileTaskStartedAt, createLargeMediaSerialEncFileTaskCompletedAt, correlationId, null, null, null, batchId);

        //                  return await Task.FromResult(ParallelBaseFolderTasks.All(x => x.Result.Equals(true)));
        //              },
        //          batchId, correlationId);
        //}



        //Task<bool> CreateAncillaryFilesForAio(string batchId, string aioExchangeSetPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime, SalesCatalogueProductResponse salesCatalogueProductResponse, List<FulfilmentDataResponse> listFulfilmentAioData);

        //Task<bool> CreateCatalogFile(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse);

        //Task<bool> CreateProductFile(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime, bool encryption);

        //Task CreateSerialEncFile(string batchId, string exchangeSetPath, string correlationId);

        //Task<bool> CreateLargeMediaExchangesetCatalogFile(string batchId, string exchangeSetPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse);
    }
}
