using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
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

        [SetUp]
        public void Setup()
        {
            fakeScsStorageService = A.Fake<ISalesCatalogueStorageService>();
            fakeAzureBlobStorageService = A.Fake<IAzureBlobStorageService>();
            fakeQueryFssService = A.Fake<IFulfilmentFileShareService>();
            fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakeFileShareServiceConfig = Options.Create(new FileShareServiceConfiguration()
            { BaseUrl = "http://tempuri.org", CellName = "DE260001", EditionNumber = "1", Limit = 10, Start = 0, 
                ProductCode = "AVCS", ProductLimit = 4, UpdateNumber = "0", UpdateNumberLimit = 10, ParallelSearchTaskCount = 10,
                EncRoot = "ENC_ROOT",
                ExchangeSetFileFolder = "V01X01"
            });
            fakeEssFulfilmentStorageConfiguration = Options.Create(new EssFulfilmentStorageConfiguration() 
                                                    { QueueName="",StorageAccountKey="",StorageAccountName="",StorageContainerName=""});

            fulfilmentDataService = new FulfilmentDataService(fakeAzureBlobStorageService, fakeQueryFssService,fakeLogger, fakeFileShareServiceConfig, fakeConfiguration);
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
            return  new SalesCatalogueProductResponse
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

        [Test]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsStringDownloadcompletedSuccessfully()
        {
            SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage = GetScsResponseQueueMessage();
            SalesCatalogueProductResponse salesCatalogueProductResponse = GetSalesCatalogueResponse();
            string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString())
              .Returns(storageAccountConnectionString);

            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored)).Returns(salesCatalogueProductResponse);

            string salesCatalogueResponseFile = await fulfilmentDataService.CreateExchangeSet(scsResponseQueueMessage);

            Assert.AreEqual("Received Fulfilment Data Successfully!!!!", salesCatalogueResponseFile);
        }
    }
}
