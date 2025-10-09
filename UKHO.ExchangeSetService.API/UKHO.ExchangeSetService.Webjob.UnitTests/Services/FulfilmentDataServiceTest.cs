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
using UKHO.ExchangeSetService.FulfilmentService.FileBuilders;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.FulfilmentService.Validation;
using UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper;
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
        private IFulfilmentFileShareService fakeFulfilmentFileShareService;
        private ILogger<FulfilmentDataService> fakeLogger;
        private IOptions<FileShareServiceConfiguration> fakeFileShareServiceConfig;
        private IConfiguration fakeConfiguration;
        private IFulfilmentAncillaryFiles fakeFulfilmentAncillaryFiles;
        private IFulfilmentSalesCatalogueService fakeFulfilmentSalesCatalogueService;
        private IFulfilmentCallBackService fakeFulfilmentCallBackService;
        private IExchangeSetBuilder fakeExchangeSetBuilder;
        private readonly string currentUtcDate = DateTime.UtcNow.ToString("ddMMMyyyy");
        private IMonitorHelper fakeMonitorHelper;
        private IFileSystemHelper fakeFileSystemHelper;
        private IProductDataValidator fakeproductDataValidator;
        private const string FulfilmentExceptionMessage = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. Please contact UKHO Customer Services quoting error code : {0} and correlation ID : {1}";
        private readonly DateTime fakeScsRequestDateTime = DateTime.UtcNow;
        private IOptions<AioConfiguration> _aioConfiguration;
        private const string EncExchangeSet = "V01X01";
        private const string AioExchangeSet = "AIO";

        [SetUp]
        public void Setup()
        {
            _aioConfiguration = Options.Create(new AioConfiguration());
            fakeScsStorageService = A.Fake<ISalesCatalogueStorageService>();
            fakeAzureBlobStorageService = A.Fake<IAzureBlobStorageService>();
            fakeFulfilmentFileShareService = A.Fake<IFulfilmentFileShareService>();
            fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakeConfiguration["HOME"] = FakeBatchValue.HomeDirectoryPath;
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
            fakeExchangeSetBuilder = A.Fake<IExchangeSetBuilder>();
            fulfilmentDataService = new FulfilmentDataService(fakeAzureBlobStorageService, fakeFulfilmentFileShareService, fakeLogger, FakeBatchValue.FileShareServiceConfiguration, fakeConfiguration, fakeFulfilmentSalesCatalogueService, fakeFulfilmentCallBackService, fakeMonitorHelper, FakeBatchValue.AioConfiguration, fakeFileSystemHelper, fakeExchangeSetBuilder);
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
                new BatchFile { Filename = "Test6.000", FileSize = 400, MimeType = "application/s57", Links = new Links { Get = new Link { Href = "" } },
                                Attributes = new List<Attribute>{new() { Key = "s57-CRC", Value = "1234CRC" } } },
                new BatchFile { Filename = "TEST4.TIF", FileSize = 400, MimeType = "IMAGE/TIFF", Links = new Links { Get = new Link { Href = "" } } },
                new BatchFile { Filename = "Default.img", FileSize = 400, MimeType = "image/jpeg", Links = new Links { Get = new Link { Href = "" } } }
            ];
        }

        #region GetScsResponseQueueMessage

        private static SalesCatalogueServiceResponseQueueMessage GetScsResponseQueueMessage(string exchangeSetStandard)
        {
            return new SalesCatalogueServiceResponseQueueMessage
            {
                BatchId = FakeBatchValue.BatchId,
                FileSize = 4000,
                ScsResponseUri = $"https://test/ess-test/{FakeBatchValue.BatchId}.json",
                CallbackUri = "https://test-callbackuri.com",
                CorrelationId = FakeBatchValue.CorrelationId,
                IsEmptyEncExchangeSet = false,
                IsEmptyAioExchangeSet = false,
                ExchangeSetStandard = exchangeSetStandard
            };
        }

        #endregion

        #region GetSalesCatalogueResponse

        private static SalesCatalogueProductResponse GetSalesCatalogueProductResponse_Old()
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

        private static SalesCatalogueProductResponse GetSalesCatalogueProductResponse(bool includeAio = false)
        {
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse
            {
                Products =
                [
                    new Products { ProductName = "GB800002", EditionNumber = 1, UpdateNumbers = [0], FileSize = 100 },
                    new Products { ProductName = "GB800003", EditionNumber = 1, UpdateNumbers = [0], FileSize = 100 }
                ]
            };

            if (includeAio)
            {
                salesCatalogueProductResponse.Products.Add(new Products { ProductName = FakeBatchValue.AioCell1, EditionNumber = 1, UpdateNumbers = [0], FileSize = 100 });
            }

            return salesCatalogueProductResponse;
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
                        ProductName = FakeBatchValue.AioCell1,
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

        private static IDirectoryInfo[] GetSubDirectories(bool standard, bool aio)
        {
            var count = 0;
            count += standard ? 1 : 0;
            count += aio ? 1 : 0;
            var fakeDirectoryInfos = new IDirectoryInfo[count];
            var currentEntry = 0;

            if (standard)
            {
                var fakeStandardDirectoryInfo = A.Fake<IDirectoryInfo>();
                A.CallTo(() => fakeStandardDirectoryInfo.Name).Returns(FakeBatchValue.ExchangeSetFileFolder);
                A.CallTo(() => fakeStandardDirectoryInfo.FullName).Returns(FakeBatchValue.ExchangeSetPath);
                fakeDirectoryInfos[currentEntry] = fakeStandardDirectoryInfo;
                currentEntry++;
            }

            if (aio)
            {
                var fakeAioDirectoryInfo = A.Fake<IDirectoryInfo>();
                A.CallTo(() => fakeAioDirectoryInfo.Name).Returns(FakeBatchValue.AioExchangeSetFileFolder);
                A.CallTo(() => fakeAioDirectoryInfo.FullName).Returns(FakeBatchValue.AioExchangeSetPath);
                fakeDirectoryInfos[currentEntry] = fakeAioDirectoryInfo;
            }

            return fakeDirectoryInfos;
        }

        private IFileInfo[] GetZipFiles()
        {
            var fakeFileInfo = A.Fake<IFileInfo>();
            A.CallTo(() => fakeFileInfo.Name).Returns(fakeFileShareServiceConfig.Value.ExchangeSetFileName);

            return [fakeFileInfo];
        }

        private static IFileInfo[] GetZipFiles(bool standard, bool aio)
        {
            var count = 0;
            count += standard ? 1 : 0;
            count += aio ? 1 : 0;
            var fakeFileInfos = new IFileInfo[count];
            var currentEntry = 0;

            if (standard)
            {
                var fakeStandardFileInfo = A.Fake<IFileInfo>();
                A.CallTo(() => fakeStandardFileInfo.Name).Returns(FakeBatchValue.ExchangeSetZipFileName);
                A.CallTo(() => fakeStandardFileInfo.FullName).Returns(FakeBatchValue.ExchangeSetZipFilePath);
                fakeFileInfos[currentEntry] = fakeStandardFileInfo;
                currentEntry++;
            }

            if (aio)
            {
                var fakeAioFileInfo = A.Fake<IFileInfo>();
                A.CallTo(() => fakeAioFileInfo.Name).Returns(FakeBatchValue.AioExchangeSetZipFileName);
                A.CallTo(() => fakeAioFileInfo.FullName).Returns(FakeBatchValue.AioExchangeSetZipFilePath);
                fakeFileInfos[currentEntry] = fakeAioFileInfo;
            }

            return fakeFileInfos;
        }

        [Test]
        [TestCase("s63", FakeBatchValue.S63BusinessUnit)]
        [TestCase("s57", FakeBatchValue.S57BusinessUnit)]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsStandardExchangeSetNoAioCreatedSuccessfully(string exchangeSetStandard, string businessUnit)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).Returns(GetSubDirectories(true, false));
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).Returns(GetZipFiles(true, false));
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).Returns(true);

            var result = await fulfilmentDataService.CreateExchangeSet(message, FakeBatchValue.CurrentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Created Successfully"));
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardExchangeSet(message, salesCatalogueProductResponse, A<List<Products>>.Ignored, FakeBatchValue.ExchangeSetPath, salesCatalogueDataResponse, businessUnit)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();

            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssStart, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssCompleted, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreated, "Exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
        }

        [Test]
        [TestCase("s63", FakeBatchValue.S63BusinessUnit)]
        [TestCase("s57", FakeBatchValue.S57BusinessUnit)]
        public void WhenValidMessageQueueTriggerAndZipFolderNotCreated_ThenThrowsFulfilmentException(string exchangeSetStandard, string businessUnit)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).Returns(GetSubDirectories(true, false));
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentDataService.CreateExchangeSet(message, FakeBatchValue.CurrentUtcDate); });

            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardExchangeSet(message, salesCatalogueProductResponse, A<List<Products>>.Ignored, FakeBatchValue.ExchangeSetPath, salesCatalogueDataResponse, businessUnit)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.ErrorInCreatingZipFile, "Error in creating exchange set zip:{ExchangeSetFileName} for BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", logLevel: LogLevel.Error);
        }

        [Test]
        [TestCase("test")]
        public void WhenExchangeSetStandardParameterOtherThanS63AndS57_ThenThrowsException(string exchangeSetStandard)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentDataService.CreateExchangeSet(message, FakeBatchValue.CurrentUtcDate); });
        }

        #region AIO

        [Test]
        [TestCase("s63", FakeBatchValue.S63BusinessUnit)]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsStandardAndAioExchangeSetCreatedSuccessfully(string exchangeSetStandard, string businessUnit)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).Returns(GetSubDirectories(true, true));
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).Returns(GetZipFiles(true, true));
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetZipFileName)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).Returns(true);

            var result = await fulfilmentDataService.CreateExchangeSet(message, FakeBatchValue.CurrentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Created Successfully"));
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardExchangeSet(message, salesCatalogueProductResponse, A<List<Products>>.Ignored, FakeBatchValue.ExchangeSetPath, salesCatalogueDataResponse, businessUnit)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(message, FakeBatchValue.CurrentUtcDate, FakeBatchValue.HomeDirectoryPath, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();

            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 2);
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 2);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssStart, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 2);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssCompleted, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 2);
            fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreated, "Exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
        }

        [Test, Description("Creating ENC exchange set without AIO exchange set")]
        [TestCase("s63", FakeBatchValue.S63BusinessUnit)]
        public async Task WhenValidMessageQueueTrigger_WithNoAioCellsConfigured_ThenReturnsExchangeSetCreatedSuccessfullyWithoutAioCell(string exchangeSetStandard, string businessUnit)
        {
            FakeBatchValue.AioConfiguration.Value.AioCells = string.Empty; // no AIO cells configured
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).Returns(GetSubDirectories(true, false));
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).Returns(GetZipFiles(true, false));
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).Returns(true);

            var result = await fulfilmentDataService.CreateExchangeSet(message, FakeBatchValue.CurrentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Created Successfully"));
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardExchangeSet(message, salesCatalogueProductResponse, A<List<Products>>.Ignored, FakeBatchValue.ExchangeSetPath, salesCatalogueDataResponse, businessUnit)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, FakeBatchValue.AioExchangeSetPath, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, FakeBatchValue.AioExchangeSetZipFileName)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();

            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssStart, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssCompleted, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreated, "Exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
        }

        [Test, Description("Creating Empty Aio exchange set without ENC exchange set")]
        [TestCase("s63", FakeBatchValue.S63BusinessUnit)]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsEmptyAioExchangeSetCreatedSuccessfully(string exchangeSetStandard, string businessUnit)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponseForAio();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            message.IsEmptyAioExchangeSet = true;
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse { Products = [] };
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).Returns(GetSubDirectories(false, true));
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).Returns(GetZipFiles(false, true));
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetZipFileName)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).Returns(true);

            var result = await fulfilmentDataService.CreateExchangeSet(message, FakeBatchValue.CurrentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Created Successfully"));
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardExchangeSet(A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<SalesCatalogueProductResponse>.Ignored, A<List<Products>>.Ignored, A<string>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(message, FakeBatchValue.CurrentUtcDate, FakeBatchValue.HomeDirectoryPath, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, FakeBatchValue.ExchangeSetPath, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, FakeBatchValue.ExchangeSetZipFileName)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();

            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssStart, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssCompleted, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreated, "Exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
        }

        [Test, Description("Creating Empty ENC exchange set without AIO exchange set")]
        [TestCase("s63", FakeBatchValue.S63BusinessUnit)]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsEmptyExchangeSetCreatedSuccessfullyWithoutAioCell(string exchangeSetStandard, string businessUnit)
        {
            FakeBatchValue.AioConfiguration.Value.AioCells = null; // no AIO cells configured
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            message.IsEmptyEncExchangeSet = true;
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse { Products = [] };
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).Returns(GetSubDirectories(true, false));
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).Returns(GetZipFiles(true, false));
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).Returns(true);

            var result = await fulfilmentDataService.CreateExchangeSet(message, FakeBatchValue.CurrentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Created Successfully"));
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardExchangeSet(message, salesCatalogueProductResponse, A<List<Products>>.Ignored, FakeBatchValue.ExchangeSetPath, salesCatalogueDataResponse, businessUnit)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, FakeBatchValue.AioExchangeSetPath, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, FakeBatchValue.AioExchangeSetZipFileName)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();

            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssStart, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssCompleted, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreated, "Exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
        }

        [Test]
        [TestCase("s63", FakeBatchValue.S63BusinessUnit)]
        public async Task WhenInValidMessageQueueTrigger_ThenReturnsStandardAndAioExchangeSetIsNotCreated(string exchangeSetStandard, string businessUnit)
        {
            var scsResponseQueueMessage = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse_Old();
            _aioConfiguration.Value.AioCells = "GB800001";

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
            A.CallTo(() => fakeFulfilmentFileShareService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.SearchIhoCrtFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.SearchIhoPubFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadIhoCrtFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadIhoPubFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(false);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, salesCatalogueDataResponse, fakeScsRequestDateTime, A<bool>.Ignored)).Returns(false);
            A.CallTo(() => fakeFulfilmentCallBackService.SendCallBackResponse(A<SalesCatalogueProductResponse>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.QueryFileShareServiceData(A<List<Products>>.Ignored,
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

            _aioConfiguration.Value.AioCells = "GB800001";

            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetSalesCatalogueProductResponse_Old());
            A.CallTo(() => fakeproductDataValidator.Validate(A<List<Products>>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(A<string>.Ignored)).Returns(directoryInfos);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateMediaFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored));
            A.CallTo(() => fakeFulfilmentFileShareService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.SearchIhoCrtFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.SearchIhoPubFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadIhoCrtFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadIhoPubFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, salesCatalogueDataResponse, fakeScsRequestDateTime, A<bool>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.SearchFolderDetails(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(batchFiles);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadFolderDetails(A<string>.Ignored, A<string>.Ignored, batchFiles, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateEncUpdateCsv(salesCatalogueDataResponse, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitLargeMediaExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.QueryFileShareServiceData(A<List<Products>>.Ignored,
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

            _aioConfiguration.Value.AioCells = "GB800001";

            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetSalesCatalogueProductResponse_Old());
            A.CallTo(() => fakeproductDataValidator.Validate(A<List<Products>>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(A<string>.Ignored)).Returns(directoryInfos);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateMediaFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored));
            A.CallTo(() => fakeFulfilmentFileShareService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.SearchIhoCrtFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.SearchIhoPubFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadIhoCrtFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadIhoPubFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, salesCatalogueDataResponse, fakeScsRequestDateTime, A<bool>.Ignored)).Returns(false);
            A.CallTo(() => fakeFulfilmentFileShareService.SearchFolderDetails(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(batchFiles);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadFolderDetails(A<string>.Ignored, A<string>.Ignored, batchFiles, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateEncUpdateCsv(salesCatalogueDataResponse, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(false);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitLargeMediaExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.QueryFileShareServiceData(A<List<Products>>.Ignored,
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
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse_Old();

            var response = new LargeExchangeSetDataResponse()
            {
                SalesCatalogueDataResponse = salesCatalogueDataResponse,
                SalesCatalogueProductResponse = salesCatalogueProductResponse
            };

            IEnumerable<BatchFile> batchFiles = GetFiles();

            _aioConfiguration.Value.AioCells = null;

            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetSalesCatalogueProductResponse_Old());
            A.CallTo(() => fakeproductDataValidator.Validate(A<List<Products>>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(A<string>.Ignored)).Returns(directoryInfos);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateMediaFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored));
            A.CallTo(() => fakeFulfilmentFileShareService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.SearchIhoCrtFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.SearchIhoPubFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadIhoCrtFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadIhoPubFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, salesCatalogueDataResponse, fakeScsRequestDateTime, A<bool>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateSerialEncFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.SearchFolderDetails(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(batchFiles);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadFolderDetails(A<string>.Ignored, A<string>.Ignored, batchFiles, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateEncUpdateCsv(response.SalesCatalogueDataResponse, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitLargeMediaExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeFulfilmentFileShareService.QueryFileShareServiceData(A<List<Products>>.Ignored,
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
            _aioConfiguration.Value.AioCells = "GB800001";

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
            A.CallTo(() => fakeFulfilmentFileShareService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.SearchIhoCrtFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.SearchIhoPubFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadIhoCrtFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadIhoPubFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<DateTime>.Ignored, A<bool>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentCallBackService.SendCallBackResponse(A<SalesCatalogueProductResponse>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.QueryFileShareServiceData(A<List<Products>>.Ignored,
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

            _aioConfiguration.Value.AioCells = "GB800001";

            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(GetSalesCatalogueProductResponse_Old());
            A.CallTo(() => fakeproductDataValidator.Validate(A<List<Products>>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(A<string>.Ignored)).Returns(directoryInfos);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateMediaFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored));
            A.CallTo(() => fakeFulfilmentFileShareService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.SearchIhoCrtFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.SearchIhoPubFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadReadMeFileFromFssAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadIhoCrtFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadIhoPubFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, salesCatalogueDataResponse, fakeScsRequestDateTime, A<bool>.Ignored)).Returns(false);
            A.CallTo(() => fakeFulfilmentFileShareService.SearchFolderDetails(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(batchFiles);
            A.CallTo(() => fakeFulfilmentFileShareService.DownloadFolderDetails(A<string>.Ignored, A<string>.Ignored, batchFiles, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateEncUpdateCsv(salesCatalogueDataResponse, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<FulfilmentDataResponse>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).Returns(false);
            A.CallTo(() => fakeFulfilmentFileShareService.QueryFileShareServiceData(A<List<Products>>.Ignored,
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
