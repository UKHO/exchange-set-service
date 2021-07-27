using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Blob;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers
{
    public class AzureBlobStorageServiceTest
    {
        public ISalesCatalogueStorageService fakeScsStorageService;
        public IOptions<EssFulfilmentStorageConfiguration> fakeStorageConfig;
        public IAzureMessageQueueHelper fakeAzureMessageQueueHelper;
        public ILogger<AzureBlobStorageService> fakeLogger;
        public IAzureBlobStorageClient fakeAzureBlobStorageClient;
        public AzureBlobStorageService azureBlobStorageService;

        [SetUp]
        public void Setup()
        {
            fakeScsStorageService = A.Fake<ISalesCatalogueStorageService>();
            fakeStorageConfig = Options.Create(new EssFulfilmentStorageConfiguration()
                                { QueueName = "", StorageAccountKey = "", StorageAccountName = "", StorageContainerName = "" });

            fakeAzureMessageQueueHelper = A.Fake<IAzureMessageQueueHelper>();
            fakeLogger = A.Fake<ILogger<AzureBlobStorageService>>();
            fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();

            azureBlobStorageService = new AzureBlobStorageService(fakeScsStorageService, fakeStorageConfig, fakeAzureMessageQueueHelper, fakeLogger, fakeAzureBlobStorageClient);
        }

        private static SalesCatalogueProductResponse GetSalesCatalogueServiceResponse()
        {
            return new SalesCatalogueProductResponse()
            {
                ProductCounts = new ProductCounts()
                {
                    RequestedProductCount = 12,
                    RequestedProductsAlreadyUpToDateCount = 5,
                    RequestedProductsNotReturned = new List<RequestedProductsNotReturned>
                    {
                        new RequestedProductsNotReturned()
                        {
                            ProductName = "test",
                            Reason = "notfound"
                        }
                    },
                    ReturnedProductCount = 4
                },
                Products = new List<Products> {
                            new Products {
                                ProductName = "DE5NOBRK",
                                EditionNumber = 0,
                                UpdateNumbers = new List<int?> {0,1},
                                FileSize = 400
                            }
                        }
            };
        }

        #region StoreSaleCatalogueServiceResponseAsync
        [Test]
        public async Task WhenCallStoreSaleCatalogueServiceResponseAsync_ThenReturnsTrue()
        {
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string containerName = "testContainer";
            string callBackUri = "https://essTest/myCallback?secret=test&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";
            string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            SalesCatalogueProductResponse salesCatalogueProductResponse = GetSalesCatalogueServiceResponse();
            CancellationToken cancellationToken = CancellationToken.None;

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString()).Returns(storageAccountConnectionString);

            A.CallTo(() => fakeAzureBlobStorageClient.GetCloudBlockBlob(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(new CloudBlockBlob(new System.Uri("http://tempuri.org/blob")));

            var response = await azureBlobStorageService.StoreSaleCatalogueServiceResponseAsync(containerName, batchId, salesCatalogueProductResponse, callBackUri, correlationId, cancellationToken);
           
            Assert.IsTrue(response);
        }
        #endregion

        #region DownloadSalesCatalogueResponse

        [Test]
        public void WhenScsStorageAccountAccessKeyValueNotFound_ThenReturnKeyNotFoundException()
        {
            string scsResponseUri = "https://essTest/myCallback?secret=test&po=1234";
            string fakeBatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString())
              .Throws(new KeyNotFoundException("Storage account accesskey not found"));

            Assert.ThrowsAsync(Is.TypeOf<KeyNotFoundException>()
                   .And.Message.EqualTo("Storage account accesskey not found")
                    , async delegate { await azureBlobStorageService.DownloadSalesCatalogueResponse(scsResponseUri, fakeBatchId, null); });
        }

        [Test]
        public async Task WhenCallDownloadSalesCatalogueResponse_ThenReturnsSalesCatalogueProductResponse()
        {
            string scsResponseUri = "https://essTest/myCallback?secret=test&po=1234";
            string fakeBatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString()).Returns(storageAccountConnectionString);

            A.CallTo(() => fakeAzureBlobStorageClient.GetCloudBlockBlobByUri(A<string>.Ignored, A<string>.Ignored)).Returns(new CloudBlockBlob(new System.Uri("http://tempuri.org/blob")));

            A.CallTo(() => fakeAzureBlobStorageClient.DownloadTextAsync(A<CloudBlockBlob>.Ignored)).Returns("{\"Products\":[{\"productName\":\"DE5NOBRK\",\"editionNumber\":1,\"updateNumbers\":[0,1],\"fileSize\":200}],\"ProductCounts\":{\"RequestedProductCount\":1,\"ReturnedProductCount\":1,\"RequestedProductsAlreadyUpToDateCount\":0,\"RequestedProductsNotReturned\":[]}}");

            var response = await azureBlobStorageService.DownloadSalesCatalogueResponse(scsResponseUri, fakeBatchId, null);

            Assert.IsInstanceOf<SalesCatalogueProductResponse>(response);
            Assert.AreEqual("DE5NOBRK", response.Products[0].ProductName);
            Assert.AreEqual(1, response.Products[0].EditionNumber);
            Assert.AreEqual(0, response.Products[0].UpdateNumbers[0].Value);
        }
        #endregion
    }
}
