using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net;
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
        public IScsStorageService fakeScsStorageService;
        public IOptions<EssFulfilmentStorageConfiguration> fakeEssFulfilmentStorageConfiguration;
        public FulfilmentDataService fulfilmentDataService;
        public IAzureBlobStorageClient fakeAzureBlobStorageClient;
        public IQueryFssService fakeQueryFssService;

        [SetUp]
        public void Setup()
        {
            fakeScsStorageService = A.Fake<IScsStorageService>();
            fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();
            fakeQueryFssService = A.Fake<IQueryFssService>();
            fakeEssFulfilmentStorageConfiguration = Options.Create(new EssFulfilmentStorageConfiguration() 
                                                    { QueueName="",StorageAccountKey="",StorageAccountName="",StorageContainerName=""});

            fulfilmentDataService = new FulfilmentDataService(fakeScsStorageService, fakeAzureBlobStorageClient, fakeQueryFssService,
                fakeEssFulfilmentStorageConfiguration);
        }

        private ScsResponseQueueMessage GetScsResponseQueueMessage()
        {
            return new ScsResponseQueueMessage
            {
                BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272",
                FileSize = 4000,
                ScsResponseUri = "https://esstestdevstorage.blob.core.windows.net/ess-fulfilment/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272.json"
            };
        }

        #region GetSalesCatalogueResponse
        private SalesCatalogueResponse GetSalesCatalogueResponse()
        {
            return new SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.BadRequest,
                ResponseBody = new SalesCatalogueProductResponse
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
                }
            };
        }
        #endregion

        [Test]
        public async void WhenScsStorageAccountAccessKeyValueNotfound_ThenGetStorageAccountConnectionStringReturnsKeyNotFoundException()
        {
            ScsResponseQueueMessage scsResponseQueueMessage = GetScsResponseQueueMessage();

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString())
              .Returns("Storage account accesskey not found");

            string salesCatalogueResponseFile =  await fulfilmentDataService.DownloadSalesCatalogueResponse(scsResponseQueueMessage.BatchId);

            Assert.ThrowsAsync(Is.TypeOf<KeyNotFoundException>()
                   .And.Message.EqualTo("Storage account accesskey not found")
                    , async delegate { await fulfilmentDataService.DownloadSalesCatalogueResponse(scsResponseQueueMessage.BatchId); });
        }

        [Test]
        public async void TestKeyNotFoundExceptionWhenScsStorageAccountAccessKeyValueNotfound()
        {
            ScsResponseQueueMessage scsResponseQueueMessage = GetScsResponseQueueMessage();
            SalesCatalogueResponse salesCatalogueResponse = GetSalesCatalogueResponse();
            string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString())
              .Returns(storageAccountConnectionString);

            A.CallTo(() => fakeAzureBlobStorageClient.DownloadScsResponse(A<string>.Ignored)).Returns(salesCatalogueResponse);

            string salesCatalogueResponseFile = await fulfilmentDataService.DownloadSalesCatalogueResponse(scsResponseQueueMessage.BatchId);

            Assert.AreEqual("Download completed Successfully!!!!",salesCatalogueResponseFile);
        }
    }
}
