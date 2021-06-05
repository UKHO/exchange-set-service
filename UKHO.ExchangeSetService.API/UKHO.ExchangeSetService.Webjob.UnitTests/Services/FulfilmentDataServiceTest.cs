using FakeItEasy;
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
        public IAzureBlobStorageService fakeAzureBlobStorageClient;
        public IFulfilmentFileShareService fakeQueryFssService;
        public ILogger<FulfilmentDataService> fakeLogger;

        [SetUp]
        public void Setup()
        {
            fakeScsStorageService = A.Fake<ISalesCatalogueStorageService>();
            fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageService>();
            fakeQueryFssService = A.Fake<IFulfilmentFileShareService>();
            fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();
            fakeEssFulfilmentStorageConfiguration = Options.Create(new EssFulfilmentStorageConfiguration() 
                                                    { QueueName="",StorageAccountKey="",StorageAccountName="",StorageContainerName=""});

            fulfilmentDataService = new FulfilmentDataService(fakeScsStorageService, fakeAzureBlobStorageClient, fakeQueryFssService,
                fakeEssFulfilmentStorageConfiguration, fakeLogger);
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
        public void WhenScsStorageAccountAccessKeyValueNotfound_ThenGetStorageAccountConnectionStringReturnsKeyNotFoundException()
        {
            SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage = GetScsResponseQueueMessage();

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString())
              .Throws(new KeyNotFoundException("Storage account accesskey not found"));

            Assert.ThrowsAsync(Is.TypeOf<KeyNotFoundException>()
                   .And.Message.EqualTo("Storage account accesskey not found")
                    , async delegate { await fulfilmentDataService.CreateExchangeSet(scsResponseQueueMessage.ScsResponseUri, scsResponseQueueMessage.BatchId); });
        }

        [Test]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsStringDownloadcompletedSuccessfully()
        {
            SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage = GetScsResponseQueueMessage();
            SalesCatalogueProductResponse salesCatalogueProductResponse = GetSalesCatalogueResponse();
            string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString())
              .Returns(storageAccountConnectionString);

            A.CallTo(() => fakeAzureBlobStorageClient.DownloadSalesCatalogueResponse(A<string>.Ignored)).Returns(salesCatalogueProductResponse);

            string salesCatalogueResponseFile = await fulfilmentDataService.CreateExchangeSet(scsResponseQueueMessage.ScsResponseUri, scsResponseQueueMessage.BatchId);

            Assert.AreEqual("Received Fulfilment Data Successfully!!!!", salesCatalogueResponseFile);
        }
    }
}
