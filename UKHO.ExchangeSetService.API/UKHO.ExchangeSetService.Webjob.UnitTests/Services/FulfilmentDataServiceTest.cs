using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentDataServiceTest
    {
        public ISalesCatalogueStorageService fakeScsStorageService;
        public IOptions<EssFulfilmentStorageConfiguration> fakeEssFulfilmentStorageConfiguration;
        public FulfilmentDataService fulfilmentDataService;
        public IAzureBlobStorageService fakeAzureBlobStorageService;
        public IFulfilmentFileShareService fakeQueryFssService;
        public ILogger<FulfilmentDataService> fakeLogger;
        public IOptions<FileShareServiceConfiguration> fakeFileShareServiceConfig;
        public IConfiguration fakeConfiguration;
        public IFulfilmentAncillaryFiles fakeFulfilmentAncillaryFiles;
        public IFulfilmentSalesCatalogueService fakeFulfilmentSalesCatalogueService;
        public IFulfilmentCallBackService fakeFulfilmentCallBackService;
        public IFulfilmentDataService fakeFulfilmentDataService;
        public string currentUtcDate = DateTime.UtcNow.ToString("ddMMMyyyy");
        public IMonitorHelper fakeMonitorHelper;
        public IFileSystemHelper fakeFileSystemHelper;
        public IOptions<PeriodicOutputServiceConfiguration> fakePeriodicOutputServiceConfiguration;

        [SetUp]
        public void Setup()
        {
            fakeScsStorageService = A.Fake<ISalesCatalogueStorageService>();
            fakeAzureBlobStorageService = A.Fake<IAzureBlobStorageService>();
            fakeQueryFssService = A.Fake<IFulfilmentFileShareService>();
            fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakeFulfilmentAncillaryFiles = A.Fake<IFulfilmentAncillaryFiles>();
            fakeFulfilmentDataService = A.Fake<IFulfilmentDataService>();
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
                CommentVersion = "VERSION=1.0"
            });
            fakeEssFulfilmentStorageConfiguration = Options.Create(new EssFulfilmentStorageConfiguration()
            { QueueName = "", StorageAccountKey = "", StorageAccountName = "", StorageContainerName = "" });
            fakeFulfilmentSalesCatalogueService = A.Fake<IFulfilmentSalesCatalogueService>();
            fakeFulfilmentCallBackService = A.Fake<IFulfilmentCallBackService>();
            fakeMonitorHelper = A.Fake<IMonitorHelper>();
            fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            fakePeriodicOutputServiceConfiguration = Options.Create(new PeriodicOutputServiceConfiguration()
            {
                LargeMediaExchangeSetSizeInMB = 1,
                LargeExchangeSetFolderName = "M0{0}X02"
            });

            fulfilmentDataService = new FulfilmentDataService(fakeAzureBlobStorageService, fakeQueryFssService, fakeLogger, fakeFileShareServiceConfig, fakeConfiguration, fakeFulfilmentAncillaryFiles, fakeFulfilmentSalesCatalogueService, fakeFulfilmentCallBackService, fakeMonitorHelper, fakeFileSystemHelper, fakePeriodicOutputServiceConfiguration);
        }

        #region GetScsResponseQueueMessage
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
        #endregion

        #region GetSalesCatalogueResponse
        private SalesCatalogueProductResponse GetSalesCatalogueResponse()
        {
            return new SalesCatalogueProductResponse
            {
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 6,
                    RequestedProductsAlreadyUpToDateCount = 8,
                    ReturnedProductCount = 2,
                    RequestedProductsNotReturned = new List<RequestedProductsNotReturned> {
                                new RequestedProductsNotReturned { ProductName = "GB123456", Reason = "productWithdrawn" },
                                new RequestedProductsNotReturned { ProductName = "GB123789", Reason = "invalidProduct" }
                            }
                },
                Products = new List<Products> {
                            new Products {
                                ProductName = "productName",
                                EditionNumber = 2,
                                UpdateNumbers = new List<int?> { 3, 4 },
                                 Dates = new List<Dates> {
                            new Dates{ UpdateNumber= 4, UpdateApplicationDate= DateTime.Today, IssueDate = DateTime.Today}
                        },
                                Cancellation = new Cancellation {
                                    EditionNumber = 4,
                                    UpdateNumber = 6
                                },
                                FileSize = 400
                            }
                        }
            };
        }
        #endregion

        #region GetSalesCatalogueDataResponse
        private SalesCatalogueDataResponse GetSalesCatalogueDataResponse()
        {
            return new SalesCatalogueDataResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody = new List<SalesCatalogueDataProductResponse>()
                {
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
                        CancelledCellReplacements = new List<string>() { },
                        Compression = true,
                        IssueDateLatestUpdate = DateTime.Today,
                        LastUpdateNumberPreviousEdition = 0,
                        TenDataCoverageCoordinates = ",,,,,,,,,,,,,,,,,,,",
                    }
                }
            };
        }
        #endregion

        public void WhenScsStorageAccountAccessKeyValueNotfound_ThenGetStorageAccountConnectionStringReturnsKeyNotFoundException()
        {
            SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage = GetScsResponseQueueMessage();

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString(null, null))
              .Throws(new KeyNotFoundException("Storage account accesskey not found"));

            Assert.ThrowsAsync(Is.TypeOf<KeyNotFoundException>().And.Message.EqualTo("Storage account accesskey not found"),
                    async delegate { await fulfilmentDataService.CreateExchangeSet(scsResponseQueueMessage, currentUtcDate); });
        }

        [Test]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsExchangeSetCreatedSuccessfully()
        {
            SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage = GetScsResponseQueueMessage();
            SalesCatalogueProductResponse salesCatalogueProductResponse = GetSalesCatalogueResponse();

            var fulfilmentDataResponse = new List<FulfilmentDataResponse>() {
                new FulfilmentDataResponse{ BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "Demo", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" } }
            };

            string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            fakeConfiguration["HOME"] = @"D:\\Downloads";
            fakeFileShareServiceConfig.Value.ExchangeSetFileFolder = "V01X01";
            fakeFileShareServiceConfig.Value.EncRoot = "ENC_ROOT";
            SalesCatalogueDataResponse salesCatalogueDataResponse = GetSalesCatalogueDataResponse();

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString(null, null))
              .Returns(storageAccountConnectionString);
            string filePath = @"D:\\Downloads";
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, fulfilmentDataResponse, salesCatalogueDataResponse, salesCatalogueProductResponse)).Returns(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateProductFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, salesCatalogueDataResponse)).Returns(true);
            A.CallTo(() => fakeFulfilmentCallBackService.SendCallBackResponse(A<SalesCatalogueProductResponse>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored)).Returns(true);

            string salesCatalogueResponseFile = await fulfilmentDataService.CreateExchangeSet(scsResponseQueueMessage, currentUtcDate);

            Assert.AreEqual("Exchange Set Created Successfully", salesCatalogueResponseFile);
        }
        [Test]
        public void WhenIsCancellationRequestedinExchangeSet_ThenThrowCancelledException()
        {
            SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage = GetScsResponseQueueMessage();
            SalesCatalogueProductResponse salesCatalogueProductResponse = GetSalesCatalogueResponse();

            string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            fakeConfiguration["HOME"] = @"D:\\Downloads";

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString(null, null))
             .Returns(storageAccountConnectionString);
            var productList = new List<Products> {
                            new Products {
                                ProductName = "DE5NOBRK",
                                EditionNumber = 0,
                                UpdateNumbers = new List<int?> {0,1},
                                FileSize = 400
                            }
                        };
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueProductResponse);

            Assert.ThrowsAsync<TaskCanceledException>(async () => await fulfilmentDataService.QueryFileShareServiceFiles(scsResponseQueueMessage, productList, null, cancellationTokenSource, cancellationToken));
        }

        [Test]
        public async Task WhenInvalidMessageQueueTrigger_ThenReturnsExchangeSetIsNotCreated()
        {
            SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage = GetScsResponseQueueMessage();
            SalesCatalogueProductResponse salesCatalogueProductResponse = GetSalesCatalogueResponse();
            string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            fakeConfiguration["HOME"] = @"D:\\Downloads";
            fakeFileShareServiceConfig.Value.ExchangeSetFileFolder = "V01X01";
            fakeFileShareServiceConfig.Value.EncRoot = "ENC_ROOT";
            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString(null, null))
              .Returns(storageAccountConnectionString);
            string filePath = @"D:\\Downloads";
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeQueryFssService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(filePath);
            A.CallTo(() => fakeQueryFssService.DownloadReadMeFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeQueryFssService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(false);

            string salesCatalogueResponseFile = await fulfilmentDataService.CreateExchangeSet(scsResponseQueueMessage, currentUtcDate);

            Assert.AreEqual("Exchange Set Is Not Created", salesCatalogueResponseFile);
        }

        [Test]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsLargeMediaExchangeSetCreatedSuccessfully()
        {
            SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage = GetScsResponseQueueMessage();
            CommonHelper.IsPeriodicOutputService = true;

            A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateMediaFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored));

            string largeExchangeSet = await fulfilmentDataService.CreateExchangeSet(scsResponseQueueMessage, currentUtcDate);

            Assert.AreEqual("Exchange Set Created", largeExchangeSet);
        }
    }
}