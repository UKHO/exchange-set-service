using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.FulfilmentService.Validation;
using Attribute = UKHO.ExchangeSetService.Common.Models.FileShareService.Response.Attribute;
using Links = UKHO.ExchangeSetService.Common.Models.FileShareService.Response.Links;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentDataServiceTest
    {
        private ISalesCatalogueStorageService fakeScsStorageService;
        private FulfilmentDataService fulfilmentDataService;
        private IAzureBlobStorageService fakeAzureBlobStorageService;
        private IFulfilmentFileShareService fakeQueryFssService;
        private ILogger<FulfilmentDataService> fakeLogger;
        private IOptions<FileShareServiceConfiguration> fakeFileShareServiceConfig;
        private IConfiguration fakeConfiguration;
        private IFulfilmentAncillaryFiles fakeFulfilmentAncillaryFiles;
        private IFulfilmentSalesCatalogueService fakeFulfilmentSalesCatalogueService;
        private IFulfilmentCallBackService fakeFulfilmentCallBackService;
        private readonly string currentUtcDate = DateTime.UtcNow.ToString("ddMMMyyyy");
        private IMonitorHelper fakeMonitorHelper;
        private IFileSystemHelper fakeFileSystemHelper;
        private IProductDataValidator fakeproductDataValidator;
        private const string FulfilmentExceptionMessage = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. Please contact UKHO Customer Services quoting error code : {0} and correlation ID : {1}";
        private readonly DateTime fakeScsRequestDateTime = DateTime.UtcNow;
        private IOptions<AioConfiguration> fakeAioConfiguration;
        private const string EncExchangeSet = "V01X01";
        private const string AioExchangeSet = "AIO";

        [SetUp]
        public void Setup()
        {
            fakeScsStorageService = A.Fake<ISalesCatalogueStorageService>();
            fakeAzureBlobStorageService = A.Fake<IAzureBlobStorageService>();
            fakeQueryFssService = A.Fake<IFulfilmentFileShareService>();
            fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakeFulfilmentAncillaryFiles = A.Fake<IFulfilmentAncillaryFiles>();
            fakeFileShareServiceConfig = Options.Create(new FileShareServiceConfiguration()
            {
                BaseUrl = "http://tempuri.org",
                CellName = "DE260001",
                EditionNumber = "1",
                Limit = 10,
                Start = 0,
                ProductCode = "AVCS",
                ProductLimit = 4,
                UpdateNumber = "0",
                UpdateNumberLimit = 10,
                ParallelSearchTaskCount = 10,
                EncRoot = "ENC_ROOT",
                ExchangeSetFileFolder = "V01X01",
                Info = "INFO",
                ProductFileName = "TEST.TXT",
                CatalogFileName = "CATALOG.031",
                CommentVersion = "VERSION=1.0",
                ContentInfo = "DVD INFO",
                Content = "Catalogue",
                Adc = "ADC",
                AioExchangeSetFileFolder = "AIO",
                AioExchangeSetFileName = "AIO.zip",
                ExchangeSetFileName = "V01X01.zip",
                IhoCrtFileName = "IHO.crt",
                IhoPubFileName = "IHO.pub",
                ReadMeFileName = "README.txt",
                S63BusinessUnit = "ADDS",
                S57BusinessUnit = "ADDS-S57"
            });
            fakeFulfilmentSalesCatalogueService = A.Fake<IFulfilmentSalesCatalogueService>();
            fakeFulfilmentCallBackService = A.Fake<IFulfilmentCallBackService>();
            fakeMonitorHelper = A.Fake<IMonitorHelper>();
            fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            fakeproductDataValidator = A.Fake<IProductDataValidator>();
            fakeAioConfiguration = A.Fake<IOptions<AioConfiguration>>();

            fulfilmentDataService = new FulfilmentDataService(fakeAzureBlobStorageService, fakeQueryFssService, fakeLogger, fakeFileShareServiceConfig, fakeConfiguration, fakeFulfilmentAncillaryFiles, fakeFulfilmentSalesCatalogueService, fakeFulfilmentCallBackService, fakeMonitorHelper, fakeFileSystemHelper, fakeproductDataValidator, fakeAioConfiguration);
        }

        private static List<BatchFile> GetFiles()
        {
            return
            [
                new BatchFile { Filename = "Test1.txt", FileSize = 400, MimeType = "text/plain", Links = new Links { Get = new Link { Href = "" } } },
                new BatchFile { Filename = "Test2.001", FileSize = 400, MimeType = "text/plain", Links = new Links { Get = new Link { Href = "" } } },
                new BatchFile { Filename = "Test3.000", FileSize = 400, MimeType = "application/s63", Links = new Links { Get = new Link { Href = "" } },
                                Attributes = new List<Attribute> { new() { Key = "s57-CRC", Value = "1234CRC" } } },
                new BatchFile { Filename = "Test5.001", FileSize = 400, MimeType = "application/s63", Links = new Links { Get = new Link { Href = "" } },
                                Attributes = new List<Attribute> { new() { Key = "s57-CRC", Value = "1234CRC" } } },
                new BatchFile { Filename = "TEST4.TIF", FileSize = 400, MimeType = "IMAGE/TIFF", Links = new Links { Get = new Link { Href = "" } } },
                new BatchFile { Filename = "Default.img", FileSize = 400, MimeType = "image/jpeg", Links = new Links { Get = new Link { Href = "" } } }
            ];
        }

        #region GetScsResponseQueueMessage

        private static SalesCatalogueServiceResponseQueueMessage GetScsResponseQueueMessage(string exchangeSetStandard)
        {
            return new SalesCatalogueServiceResponseQueueMessage
            {
                BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272",
                FileSize = 4000,
                ScsResponseUri = "https://test/ess-test/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272.json",
                CallbackUri = "https://test-callbackuri.com",
                CorrelationId = "727c5230-2c25-4244-9580-13d90004584a",
                IsEmptyEncExchangeSet = false,
                IsEmptyAioExchangeSet = false,
                ExchangeSetStandard = exchangeSetStandard
            };
        }

        #endregion

        #region GetSalesCatalogueResponse

        private static SalesCatalogueProductResponse GetSalesCatalogueResponse()
        {
            return new SalesCatalogueProductResponse
            {
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 6,
                    RequestedProductsAlreadyUpToDateCount = 8,
                    ReturnedProductCount = 2,
                    RequestedProductsNotReturned =
                    [
                        new RequestedProductsNotReturned { ProductName = "GB123456", Reason = "productWithdrawn" },
                        new RequestedProductsNotReturned { ProductName = "GB123789", Reason = "invalidProduct" }
                    ]
                },
                Products =
                [
                    new Products
                    {
                        ProductName = "productName",
                        EditionNumber = 2,
                        UpdateNumbers = [3, 4],
                        Dates =
                        [
                            new Dates { UpdateNumber= 4, UpdateApplicationDate= DateTime.Today, IssueDate = DateTime.Today }
                        ],
                        Cancellation = new Cancellation
                        {
                            EditionNumber = 4,
                            UpdateNumber = 6
                        },
                        FileSize = 400,
                        Bundle =
                        [
                            new Bundle { BundleType = "DVD", Location = "M1:B1" }
                        ]
                    },
                    new Products
                    {
                        ProductName = "GB800001",
                        EditionNumber = 1,
                        UpdateNumbers = [1],
                        Dates =
                        [
                            new Dates { UpdateNumber= 1, UpdateApplicationDate= DateTime.Today, IssueDate = DateTime.Today }
                        ],
                        Cancellation = new Cancellation
                        {
                            EditionNumber = 4,
                            UpdateNumber = 6
                        },
                        FileSize = 400,
                        Bundle =
                        [
                            new Bundle { BundleType = "DVD", Location = "M1:B1" }
                        ]
                    }
                ]
            };
        }

        private static SalesCatalogueProductResponse GetSalesCatalogueResponseForAio()
        {
            return new SalesCatalogueProductResponse
            {
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 6,
                    RequestedProductsAlreadyUpToDateCount = 8,
                    ReturnedProductCount = 2,
                    RequestedProductsNotReturned =
                    [
                        new RequestedProductsNotReturned { ProductName = "GB123456", Reason = "productWithdrawn" },
                        new RequestedProductsNotReturned { ProductName = "GB123789", Reason = "invalidProduct" }
                    ]
                },
                Products =
                [
                    new Products
                    {
                        ProductName = "GB800001",
                        EditionNumber = 1,
                        UpdateNumbers = [1],
                        Dates =
                        [
                            new Dates { UpdateNumber= 1, UpdateApplicationDate= DateTime.Today, IssueDate = DateTime.Today }
                        ],
                        Cancellation = new Cancellation
                        {
                            EditionNumber = 4,
                            UpdateNumber = 6
                        },
                        FileSize = 400,
                        Bundle =
                        [
                            new Bundle { BundleType = "DVD", Location = "M1:B1" }
                        ]
                    }
                ]
            };
        }

        #endregion

        #region GetSalesCatalogueDataResponse

        private static SalesCatalogueDataResponse GetSalesCatalogueDataResponse()
        {
            return new SalesCatalogueDataResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody =
                [
                    new SalesCatalogueDataProductResponse
                    {
                        ProductName = "10000002",
                        LatestUpdateNumber = 5,
                        FileSize = 600,
                        CellLimitSouthernmostLatitude = 24,
                        CellLimitWesternmostLatitude = 119,
                        CellLimitNorthernmostLatitude = 25,
                        CellLimitEasternmostLatitude = 120,
                        BaseCellEditionNumber = 3,
                        BaseCellLocation = "M0;B0",
                        BaseCellIssueDate = DateTime.Today,
                        BaseCellUpdateNumber = 0,
                        Encryption = true,
                        CancelledCellReplacements = [],
                        Compression = true,
                        IssueDateLatestUpdate = DateTime.Today,
                        LastUpdateNumberPreviousEdition = 0,
                        TenDataCoverageCoordinates = ",,,,,,,,,,,,,,,,,,,"
                    }
                ]
            };
        }

        private static SalesCatalogueDataResponse GetSalesCatalogueDataResponseForAio()
        {
            return new SalesCatalogueDataResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody =
                [
                    new SalesCatalogueDataProductResponse
                    {
                        ProductName = "GB800001",
                        LatestUpdateNumber = 5,
                        FileSize = 600,
                        CellLimitSouthernmostLatitude = 24,
                        CellLimitWesternmostLatitude = 119,
                        CellLimitNorthernmostLatitude = 25,
                        CellLimitEasternmostLatitude = 120,
                        BaseCellEditionNumber = 3,
                        BaseCellLocation = "M0;B0",
                        BaseCellIssueDate = DateTime.Today,
                        BaseCellUpdateNumber = 0,
                        Encryption = true,
                        CancelledCellReplacements = [],
                        Compression = true,
                        IssueDateLatestUpdate = DateTime.Today,
                        LastUpdateNumberPreviousEdition = 0,
                        TenDataCoverageCoordinates = ",,,,,,,,,,,,,,,,,,,"
                    }
                ]
            };
        }

        #endregion

        private IDirectoryInfo GetSubDirectories(string exchangeSetType)
        {
            var fakeDirectoryInfo = A.Fake<IDirectoryInfo>();

            if (exchangeSetType == EncExchangeSet)
            {
                A.CallTo(() => fakeDirectoryInfo.Name).Returns(fakeFileShareServiceConfig.Value.ExchangeSetFileFolder);
                A.CallTo(() => fakeDirectoryInfo.FullName).Returns(fakeFileShareServiceConfig.Value.ExchangeSetFileFolder);
                A.CallTo(() => fakeDirectoryInfo.GetDirectories()).Returns([fakeDirectoryInfo]);
            }
            else if (exchangeSetType == AioExchangeSet)
            {
                A.CallTo(() => fakeDirectoryInfo.Name).Returns(fakeFileShareServiceConfig.Value.AioExchangeSetFileFolder);
                A.CallTo(() => fakeDirectoryInfo.FullName).Returns(fakeFileShareServiceConfig.Value.AioExchangeSetFileFolder);
                A.CallTo(() => fakeDirectoryInfo.GetDirectories()).Returns([fakeDirectoryInfo]);
            }

            return fakeDirectoryInfo;
        }

        private IFileInfo[] GetZipFiles()
        {
            var fakeFileInfo = A.Fake<IFileInfo>();
            A.CallTo(() => fakeFileInfo.Name).Returns(fakeFileShareServiceConfig.Value.ExchangeSetFileName);

            return [fakeFileInfo];
        }

        [Test]
        [TestCase("s63")]
        [TestCase("s57")]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsExchangeSetCreatedSuccessfully(string exchangeSetStandard)
        {
            var scsResponseQueueMessage = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueResponse();

            const string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            fakeConfiguration["HOME"] = @"D:\\Downloads";
            fakeFileShareServiceConfig.Value.ExchangeSetFileFolder = "V01X01";
            fakeFileShareServiceConfig.Value.EncRoot = "ENC_ROOT";
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString(null, null)).Returns(storageAccountConnectionString);
            const string filePath = @"D:\\Downloads";
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CommitExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, salesCatalogueDataResponse, fakeScsRequestDateTime, A<bool>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateSerialEncFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentCallBackService.SendCallBackResponse(A<SalesCatalogueProductResponse>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored)).Returns(true);
            var fakeDirectoryInfo = GetSubDirectories(EncExchangeSet);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(A<string>.Ignored)).Returns([fakeDirectoryInfo]);
            var fileInfos = GetZipFiles();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(A<string>.Ignored)).Returns(fileInfos);

            var salesCatalogueResponseFile = await fulfilmentDataService.CreateExchangeSet(scsResponseQueueMessage, currentUtcDate);

            Assert.That(salesCatalogueResponseFile, Is.EqualTo("Exchange Set Created Successfully"));

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateProductFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create product file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateCatalogFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create catalog file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExchangeSetCreated.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.DownloadReadMeFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateZipFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.UploadExchangeSetToFssStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("s63", "ADDS")]
        [TestCase("s57", "ADDS-S57")]
        public void WhenIsCancellationRequestedInExchangeSet_ThenThrowCancelledException(string exchangeSetStandard, string businessUnit)
        {
            var scsResponseQueueMessage = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueResponse();

            const string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            fakeConfiguration["HOME"] = @"D:\\Downloads";

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString(null, null)).Returns(storageAccountConnectionString);
            var productList = new List<Products>
            {
                new() {
                    ProductName = "DE5NOBRK",
                    EditionNumber = 0,
                    UpdateNumbers = [0, 1],
                    FileSize = 400
                }
            };
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            var cancellationToken = cancellationTokenSource.Token;
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueProductResponse);

            Assert.ThrowsAsync<TaskCanceledException>(async () => await fulfilmentDataService.QueryFileShareServiceFiles(scsResponseQueueMessage, productList, null, cancellationTokenSource, cancellationToken, businessUnit));
        }

        [Test]
        [TestCase("s63")]
        [TestCase("s57")]
        public void WhenValidMessageQueueTriggerAndZipFolderNotCreated_ThenThrowsFulfilmentException(string exchangeSetStandard)
        {
            var scsResponseQueueMessage = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueResponse();

            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var filePath = @"D:\\Downloads";

            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(false);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, salesCatalogueDataResponse, fakeScsRequestDateTime, A<bool>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateSerialEncFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            var fakeDirectoryInfo = GetSubDirectories(EncExchangeSet);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(A<string>.Ignored)).Returns([fakeDirectoryInfo]);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentDataService.CreateExchangeSet(scsResponseQueueMessage, currentUtcDate); });

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateProductFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create product file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateCatalogFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create catalog file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.DownloadReadMeFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateZipFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ErrorInCreatingZipFile.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in creating exchange set zip:{ExchangeSetFileName} for BatchId:{BatchId} and _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("test")]
        public void WhenExchangeSetStandardParameterOtherThanS63AndS57_ThenThrowsException(string exchangeSetStandard)
        {
            var scsResponseQueueMessage = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueResponse();

            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();

            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentDataService.CreateExchangeSet(scsResponseQueueMessage, currentUtcDate); });
        }

        [Test]
        public async Task WhenReadmeFileExistInCacheData_ThenFileDownloadedFromCache()
        {
            const string correlationId = "d6cd4d37-4d89-470d-9a33-82b3d7f54b6e";
            const string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            const string exchangeSetRootPath = @"C:\\HOME";
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromCacheAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);

            var result = await fulfilmentDataService.DownloadReadMeFileAsync(batchId, exchangeSetRootPath, correlationId);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SearchDownloadReadmeCacheEventStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Cache Search and Download readme.txt file started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SearchDownloadReadmeCacheEventCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Cache Search and Download readme.txt file completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ReadMeTextFileNotFound.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Cache Search and Download readme.txt file not found in blob cache for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustNotHaveHappened();

            Assert.That(result, Is.EqualTo(true));
        }

        [Test]
        public async Task WhenReadmeFileNotExistInCacheData_ThenFileDownloadedFromFss()
        {
            const string correlationId = "d6cd4d37-4d89-470d-9a33-82b3d7f54b6e";
            const string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            const string exchangeSetRootPath = @"C:\\HOME";
            const string readmeFilePath = "/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/README.txt";
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromCacheAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(false);
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(readmeFilePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);

            var result = await fulfilmentDataService.DownloadReadMeFileAsync(batchId, exchangeSetRootPath, correlationId);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SearchDownloadReadmeCacheEventStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Cache Search and Download readme.txt file started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SearchDownloadReadmeCacheEventCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Cache Search and Download readme.txt file completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ReadMeTextFileNotFound.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Cache Search and Download readme.txt file not found in blob cache for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            Assert.That(result, Is.EqualTo(true));
        }

        [Test]
        public async Task WhenReadmeFileNotExistInCacheDataAndFss_ThenLoggedErrorMessage()
        {
            const string correlationId = "d6cd4d37-4d89-470d-9a33-82b3d7f54b6e";
            const string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            const string exchangeSetRootPath = @"C:\\HOME";
            const string readmeFilePath = "/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/README.txt";
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromCacheAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(false);
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(readmeFilePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(false);

            var result = await fulfilmentDataService.DownloadReadMeFileAsync(batchId, exchangeSetRootPath, correlationId);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SearchDownloadReadmeCacheEventStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Cache Search and Download readme.txt file started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SearchDownloadReadmeCacheEventCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Cache Search and Download readme.txt file completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ReadMeTextFileNotFound.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Cache Search and Download readme.txt file not found in blob cache for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ErrorInDownloadReadMeFile.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error while downloading readme.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            Assert.That(result, Is.EqualTo(false));
        }

        [Test]
        public async Task WhenExceptionOccuredInDownloadReadMeFile_ThenLoggedErrorMessage()
        {
            const string correlationId = "d6cd4d37-4d89-470d-9a33-82b3d7f54b6e";
            const string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            const string exchangeSetRootPath = @"C:\\HOME";
            const string readmeFilePath = "/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/README.txt";
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromCacheAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(false);
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(readmeFilePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Throws<ApplicationException>().Once();

            var result = await fulfilmentDataService.DownloadReadMeFileAsync(batchId, exchangeSetRootPath, correlationId);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SearchDownloadReadmeCacheEventStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Cache Search and Download readme.txt file started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SearchDownloadReadmeCacheEventCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Cache Search and Download readme.txt file completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ReadMeTextFileNotFound.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Cache Search and Download readme.txt file not found in blob cache for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ErrorInDownloadReadMeFile.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error while downloading readme.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} and Exception:{Message}").MustHaveHappenedOnceExactly();

            Assert.That(result, Is.EqualTo(false));
        }

        #region AIO

        [Test]
        [TestCase("s63")]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsStandardAndAioExchangeSetCreatedSuccessfully(string exchangeSetStandard)
        {
            var scsResponseQueueMessage = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueResponse();
            fakeAioConfiguration.Value.AioCells = "GB800001";

            var fulfilmentDataResponse = new List<FulfilmentDataResponse>
            {
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "Demo", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() },
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 1, ProductName = "GB800001", UpdateNumber = 1, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() }
            };

            const string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            fakeConfiguration["HOME"] = @"D:\\TEST";
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString(null, null)).Returns(storageAccountConnectionString);
            const string filePath = @"D:\\Downloads";
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.SearchIhoCrtFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.SearchIhoPubFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.DownloadIhoCrtFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.DownloadIhoPubFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CommitExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<DateTime>.Ignored, A<bool>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentCallBackService.SendCallBackResponse(A<SalesCatalogueProductResponse>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.QueryFileShareServiceData(A<List<Products>>.Ignored,
                A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored,
                A<string>.Ignored, A<string>.Ignored)).Returns(fulfilmentDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateSerialAioFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<SalesCatalogueDataResponse>.Ignored)).Returns(true);
            var fakeDirectoryInfo = GetSubDirectories(EncExchangeSet);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(A<string>.Ignored)).Returns([fakeDirectoryInfo]);
            var fileInfos = GetZipFiles();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(A<string>.Ignored)).Returns(fileInfos);

            var result = await fulfilmentDataService.CreateExchangeSet(scsResponseQueueMessage, currentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Created Successfully"));

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.QueryFileShareServiceENCFilesRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query and download request for ENC files from BusinessUnit:{businessUnit} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.QueryFileShareServiceReadMeFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateProductFileRequestForAioStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create aio exchange set product file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateSerialAioFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create serial aio file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateCatalogFileForAioRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create AIO exchange set catalog file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.QueryFileShareServiceIhoCrtFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.DownloadIhoPubFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service download request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test, Description("Creating ENC exchange set without AIO exchange set")]
        [TestCase("s63")]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsExchangeSetCreatedSuccessfullyWithoutAioCell(string exchangeSetStandard)
        {
            var scsResponseQueueMessage = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueResponse();
            fakeAioConfiguration.Value.AioCells = null;

            var fulfilmentDataResponse = new List<FulfilmentDataResponse>
            {
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "Demo", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() }
            };

            const string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            fakeConfiguration["HOME"] = @"D:\\TEST";
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString(null, null)).Returns(storageAccountConnectionString);
            const string filePath = @"D:\\Downloads";
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CommitExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, salesCatalogueDataResponse, fakeScsRequestDateTime, A<bool>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateSerialEncFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentCallBackService.SendCallBackResponse(A<SalesCatalogueProductResponse>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.QueryFileShareServiceData(A<List<Products>>.Ignored,
                A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored,
                A<string>.Ignored, A<string>.Ignored)).Returns(fulfilmentDataResponse);
            var fakeDirectoryInfo = GetSubDirectories(EncExchangeSet);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(A<string>.Ignored)).Returns([fakeDirectoryInfo]);
            var fileInfos = GetZipFiles();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(A<string>.Ignored)).Returns(fileInfos);

            var result = await fulfilmentDataService.CreateExchangeSet(scsResponseQueueMessage, currentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Created Successfully"));

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.QueryFileShareServiceENCFilesRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query and download request for ENC files from BusinessUnit:{businessUnit} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExchangeSetCreated.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test, Description("Creating Empty Aio exchange set without ENC exchange set")]
        [TestCase("s63")]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsEmptyAioExchangeSetCreatedSuccessfully(string exchangeSetStandard)
        {
            var scsResponseQueueMessage = GetScsResponseQueueMessage(exchangeSetStandard);
            scsResponseQueueMessage.IsEmptyAioExchangeSet = true;
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse
            {
                ProductCounts = new(),
                Products = []
            };
            fakeAioConfiguration.Value.AioCells = "GB800001";

            const string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            fakeConfiguration["HOME"] = @"D:\\TEST";
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponseForAio();

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString(null, null)).Returns(storageAccountConnectionString);
            const string filePath = @"D:\\Downloads";
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.SearchIhoCrtFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.SearchIhoPubFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.DownloadIhoCrtFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.DownloadIhoPubFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CommitExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<DateTime>.Ignored, A<bool>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentCallBackService.SendCallBackResponse(A<SalesCatalogueProductResponse>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored)).Returns(true);

            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateSerialAioFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<SalesCatalogueDataResponse>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(true);

            var fakeDirectoryInfo = GetSubDirectories(AioExchangeSet);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(A<string>.Ignored)).Returns([fakeDirectoryInfo]);
            var fileInfos = GetZipFiles();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(A<string>.Ignored)).Returns(fileInfos);

            var result = await fulfilmentDataService.CreateExchangeSet(scsResponseQueueMessage, currentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Created Successfully"));

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.QueryFileShareServiceReadMeFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateProductFileRequestForAioStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create aio exchange set product file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateSerialAioFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create serial aio file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateCatalogFileForAioRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create AIO exchange set catalog file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test, Description("Creating Empty ENC exchange set without AIO exchange set")]
        [TestCase("s63")]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsEmptyExchangeSetCreatedSuccessfullyWithoutAioCell(string exchangeSetStandard)
        {
            var scsResponseQueueMessage = GetScsResponseQueueMessage(exchangeSetStandard);
            scsResponseQueueMessage.IsEmptyEncExchangeSet = true;
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse
            {
                ProductCounts = new(),
                Products = []
            };
            fakeAioConfiguration.Value.AioCells = null;

            const string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            fakeConfiguration["HOME"] = @"D:\\TEST";
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString(null, null)).Returns(storageAccountConnectionString);
            const string filePath = @"D:\\Downloads";
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CommitExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, salesCatalogueDataResponse, fakeScsRequestDateTime, A<bool>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateSerialEncFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentCallBackService.SendCallBackResponse(A<SalesCatalogueProductResponse>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored)).Returns(true);

            var fakeDirectoryInfo = GetSubDirectories(EncExchangeSet);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(A<string>.Ignored)).Returns([fakeDirectoryInfo]);
            var fileInfos = GetZipFiles();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(A<string>.Ignored)).Returns(fileInfos);

            var result = await fulfilmentDataService.CreateExchangeSet(scsResponseQueueMessage, currentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Created Successfully"));

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.QueryFileShareServiceReadMeFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateProductFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create product file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateSerialFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create serial enc file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.QueryFileShareServiceReadMeFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateCatalogFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create catalog file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExchangeSetCreated.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("s63")]
        public async Task WhenInValidMessageQueueTrigger_ThenReturnsStandardAndAioExchangeSetIsNotCreated(string exchangeSetStandard)
        {
            var scsResponseQueueMessage = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueResponse();
            fakeAioConfiguration.Value.AioCells = "GB800001";

            var fulfilmentDataResponse = new List<FulfilmentDataResponse>
            {
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "Demo", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() },
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 1, ProductName = "GB800001", UpdateNumber = 1, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() }
            };

            const string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            fakeConfiguration["HOME"] = @"D:\\TEST";
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString(null, null)).Returns(storageAccountConnectionString);
            const string filePath = @"D:\\Downloads";
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.SearchIhoCrtFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.SearchIhoPubFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.DownloadIhoCrtFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.DownloadIhoPubFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(false);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, salesCatalogueDataResponse, fakeScsRequestDateTime, A<bool>.Ignored)).Returns(false);
            A.CallTo(() => fakeFulfilmentCallBackService.SendCallBackResponse(A<SalesCatalogueProductResponse>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.QueryFileShareServiceData(A<List<Products>>.Ignored,
            A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored,
            A<string>.Ignored, A<string>.Ignored)).Returns(fulfilmentDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateSerialAioFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<SalesCatalogueDataResponse>.Ignored)).Returns(true);

            var result = await fulfilmentDataService.CreateExchangeSet(scsResponseQueueMessage, currentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Is Not Created"));

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.QueryFileShareServiceENCFilesRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query and download request for ENC files from BusinessUnit:{businessUnit} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.QueryFileShareServiceReadMeFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateProductFileRequestForAioStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create aio exchange set product file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateSerialAioFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create serial aio file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("s63")]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsLargeMediaAndAioExchangeSetCreatedSuccessfully(string exchangeSetStandard)
        {
            const string filePath = @"D:\\Downloads";
            var b1 = A.Fake<IDirectoryInfo>();
            var b2 = A.Fake<IDirectoryInfo>();
            var m1 = A.Fake<IDirectoryInfo>();

            A.CallTo(() => b1.Name).Returns("B1");
            A.CallTo(() => b2.Name).Returns("B2");
            A.CallTo(() => m1.Name).Returns("M01X02");

            IDirectoryInfo[] directoryInfos = [b1, b2, m1];

            var fulfilmentDataResponse = new List<FulfilmentDataResponse>
            {
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "Demo", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() },
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 1, ProductName = "GB800001", UpdateNumber = 1, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() }
            };

            var scsResponseQueueMessage = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            IEnumerable<BatchFile> batchFiles = GetFiles();

            fakeAioConfiguration.Value.AioCells = "GB800001";

            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetSalesCatalogueResponse());
            A.CallTo(() => fakeproductDataValidator.Validate(A<List<Products>>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(A<string>.Ignored)).Returns(directoryInfos);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateMediaFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored));
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.SearchIhoCrtFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.SearchIhoPubFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.DownloadIhoCrtFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.DownloadIhoPubFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, salesCatalogueDataResponse, fakeScsRequestDateTime, A<bool>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.SearchFolderDetails(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(batchFiles);
            A.CallTo(() => fakeQueryFssService.DownloadFolderDetails(A<string>.Ignored, A<string>.Ignored, batchFiles, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateEncUpdateCsv(salesCatalogueDataResponse, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.UploadZipFileForLargeMediaExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CommitLargeMediaExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.QueryFileShareServiceData(A<List<Products>>.Ignored,
                A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored,
                A<string>.Ignored, A<string>.Ignored)).Returns(fulfilmentDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateSerialAioFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<SalesCatalogueDataResponse>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<DateTime>.Ignored, A<bool>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(true);

            var result = await fulfilmentDataService.CreateLargeExchangeSet(scsResponseQueueMessage, currentUtcDate, "M0{0}X02");

            Assert.That(result, Is.EqualTo("Large Media Exchange Set Created Successfully"));

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.QueryFileShareServiceENCFilesRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query and download request for ENC files from BusinessUnit:{businessUnit} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.QueryFileShareServiceReadMeFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateProductFileRequestForAioStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create aio exchange set product file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateSerialAioFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create serial aio file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExchangeSetCreated.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Large media exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("s63")]
        public void WhenSerialAIOCreationFails_ThenReturnsLargeMediaAndAioExchangeSetIsNotCreated(string exchangeSetStandard)
        {
            const string filePath = @"D:\\Downloads";
            var b1 = A.Fake<IDirectoryInfo>();
            var b2 = A.Fake<IDirectoryInfo>();
            var m1 = A.Fake<IDirectoryInfo>();

            A.CallTo(() => b1.Name).Returns("B1");
            A.CallTo(() => b2.Name).Returns("B2");
            A.CallTo(() => m1.Name).Returns("M01X02");

            IDirectoryInfo[] directoryInfos = [b1, b2, m1];

            var fulfilmentDataResponse = new List<FulfilmentDataResponse>
            {
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "Demo", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() },
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 1, ProductName = "GB800001", UpdateNumber = 1, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() }
            };

            var scsResponseQueueMessage = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponseForAio();
            IEnumerable<BatchFile> batchFiles = GetFiles();

            fakeAioConfiguration.Value.AioCells = "GB800001";

            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetSalesCatalogueResponse());
            A.CallTo(() => fakeproductDataValidator.Validate(A<List<Products>>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(A<string>.Ignored)).Returns(directoryInfos);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateMediaFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored));
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.SearchIhoCrtFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.SearchIhoPubFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.DownloadIhoCrtFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.DownloadIhoPubFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, salesCatalogueDataResponse, fakeScsRequestDateTime, A<bool>.Ignored)).Returns(false);
            A.CallTo(() => fakeQueryFssService.SearchFolderDetails(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(batchFiles);
            A.CallTo(() => fakeQueryFssService.DownloadFolderDetails(A<string>.Ignored, A<string>.Ignored, batchFiles, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateEncUpdateCsv(salesCatalogueDataResponse, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.UploadZipFileForLargeMediaExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(false);
            A.CallTo(() => fakeQueryFssService.CommitLargeMediaExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.QueryFileShareServiceData(A<List<Products>>.Ignored,
                A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored,
                A<string>.Ignored, A<string>.Ignored)).Returns(fulfilmentDataResponse);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateSerialAioFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<SalesCatalogueDataResponse>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentDataService.CreateLargeExchangeSet(scsResponseQueueMessage, currentUtcDate, "M0{0}X02"); });

            A.CallTo(fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Error
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO exchange creation failed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}"
             ).MustHaveHappenedOnceExactly();
        }

        [Test, Description("Creating large media exchange set without AIO cell")]
        [TestCase("s63")]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsLargeMediaEncExchangeSetCreatedSuccessfully(string exchangeSetStandard)
        {
            const string filePath = @"D:\\Downloads";
            var b1 = A.Fake<IDirectoryInfo>();
            var b2 = A.Fake<IDirectoryInfo>();
            var m1 = A.Fake<IDirectoryInfo>();

            A.CallTo(() => b1.Name).Returns("B1");
            A.CallTo(() => b2.Name).Returns("B2");
            A.CallTo(() => m1.Name).Returns("M01X02");

            IDirectoryInfo[] directoryInfos = [b1, b2, m1];

            var fulfilmentDataResponse = new List<FulfilmentDataResponse>
            {
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "Demo", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() },
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 1, ProductName = "ABC20001", UpdateNumber = 1, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() }
            };

            var scsResponseQueueMessage = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var salesCatalogueProductResponse = GetSalesCatalogueResponse();

            var response = new LargeExchangeSetDataResponse()
            {
                SalesCatalogueDataResponse = salesCatalogueDataResponse,
                SalesCatalogueProductResponse = salesCatalogueProductResponse
            };

            IEnumerable<BatchFile> batchFiles = GetFiles();

            fakeAioConfiguration.Value.AioCells = null;

            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetSalesCatalogueResponse());
            A.CallTo(() => fakeproductDataValidator.Validate(A<List<Products>>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(A<string>.Ignored)).Returns(directoryInfos);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateMediaFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored));
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.SearchIhoCrtFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.SearchIhoPubFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.DownloadIhoCrtFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.DownloadIhoPubFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, salesCatalogueDataResponse, fakeScsRequestDateTime, A<bool>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateSerialEncFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.SearchFolderDetails(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(batchFiles);
            A.CallTo(() => fakeQueryFssService.DownloadFolderDetails(A<string>.Ignored, A<string>.Ignored, batchFiles, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateEncUpdateCsv(response.SalesCatalogueDataResponse, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.UploadZipFileForLargeMediaExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CommitLargeMediaExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeQueryFssService.QueryFileShareServiceData(A<List<Products>>.Ignored,
                A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored,
                A<string>.Ignored, A<string>.Ignored)).Returns(fulfilmentDataResponse);

            var result = await fulfilmentDataService.CreateLargeExchangeSet(scsResponseQueueMessage, currentUtcDate, "M0{0}X02");

            Assert.That(result, Is.EqualTo("Large Media Exchange Set Created Successfully"));

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExchangeSetCreated.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Large media exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test, Description("Creating only AIO exchange set")]
        [TestCase("s63")]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsAIOExchangeSetCreatedSuccessfully(string exchangeSetStandard)
        {
            var scsResponseQueueMessage = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueResponseForAio();
            fakeAioConfiguration.Value.AioCells = "GB800001";

            var fulfilmentDataResponse = new List<FulfilmentDataResponse>
            {
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "Demo", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() },
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 1, ProductName = "GB800001", UpdateNumber = 1, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() }
            };

            const string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            fakeConfiguration["HOME"] = @"D:\\TEST";
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponseForAio();

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString(null, null)).Returns(storageAccountConnectionString);
            const string filePath = @"D:\\Downloads";
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.SearchIhoCrtFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.SearchIhoPubFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.DownloadIhoCrtFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.DownloadIhoPubFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CommitExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<DateTime>.Ignored, A<bool>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentCallBackService.SendCallBackResponse(A<SalesCatalogueProductResponse>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.QueryFileShareServiceData(A<List<Products>>.Ignored,
                A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored,
                A<string>.Ignored, A<string>.Ignored)).Returns(fulfilmentDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateSerialAioFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<SalesCatalogueDataResponse>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(true);

            var fakeDirectoryInfo = GetSubDirectories(AioExchangeSet);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(A<string>.Ignored)).Returns([fakeDirectoryInfo]);
            var fileInfos = GetZipFiles();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(A<string>.Ignored)).Returns(fileInfos);

            var result = await fulfilmentDataService.CreateExchangeSet(scsResponseQueueMessage, currentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Created Successfully"));

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.QueryFileShareServiceENCFilesRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query and download request for ENC files from BusinessUnit:{businessUnit} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.QueryFileShareServiceReadMeFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateProductFileRequestForAioStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create aio exchange set product file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateSerialAioFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create serial aio file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateCatalogFileForAioRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Create AIO exchange set catalog file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("s63")]
        public void WhenCreationOfCatalogueFileForAIOFails_ThenThrowsException(string exchangeSetStandard)
        {
            const string filePath = @"D:\\Downloads";
            var b1 = A.Fake<IDirectoryInfo>();
            var b2 = A.Fake<IDirectoryInfo>();
            var m1 = A.Fake<IDirectoryInfo>();

            A.CallTo(() => b1.Name).Returns("B1");
            A.CallTo(() => b2.Name).Returns("B2");
            A.CallTo(() => m1.Name).Returns("M01X02");

            IDirectoryInfo[] directoryInfos = [b1, b2, m1];

            var fulfilmentDataResponse = new List<FulfilmentDataResponse>
            {
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "Demo", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() },
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 1, ProductName = "GB800001", UpdateNumber = 1, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() }
            };

            var scsResponseQueueMessage = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponseForAio();
            IEnumerable<BatchFile> batchFiles = GetFiles();

            fakeAioConfiguration.Value.AioCells = "GB800001";

            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetSalesCatalogueResponse());
            A.CallTo(() => fakeproductDataValidator.Validate(A<List<Products>>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(A<string>.Ignored)).Returns(directoryInfos);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateMediaFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored));
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.SearchIhoCrtFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.SearchIhoPubFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.DownloadIhoCrtFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.DownloadIhoPubFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, salesCatalogueDataResponse, fakeScsRequestDateTime, A<bool>.Ignored)).Returns(false);
            A.CallTo(() => fakeQueryFssService.SearchFolderDetails(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(batchFiles);
            A.CallTo(() => fakeQueryFssService.DownloadFolderDetails(A<string>.Ignored, A<string>.Ignored, batchFiles, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateEncUpdateCsv(salesCatalogueDataResponse, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(false);
            A.CallTo(() => fakeQueryFssService.QueryFileShareServiceData(A<List<Products>>.Ignored,
                A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored,
                A<string>.Ignored, A<string>.Ignored)).Returns(fulfilmentDataResponse);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateSerialAioFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<SalesCatalogueDataResponse>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentDataService.CreateLargeExchangeSet(scsResponseQueueMessage, currentUtcDate, "M0{0}X02"); });

            A.CallTo(fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Error
             && call.GetArgument<EventId>(1) == EventIds.LargeExchangeSetCreatedWithError.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Large media exchange creation failed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}"
             ).MustHaveHappenedOnceExactly();
        }

        #endregion
    }
}
