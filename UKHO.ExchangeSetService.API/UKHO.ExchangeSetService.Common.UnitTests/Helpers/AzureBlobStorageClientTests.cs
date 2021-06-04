using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Blob;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class AzureBlobStorageClientTests
    {
        private ISalesCatalogueStorageService fakeScsStorageService;       
        private IOptions<EssFulfilmentStorageConfiguration> fakeStorageConfig;
        private IAzureMessageQueueHelper fakeazureMessageQueueHelper;
        private ILogger<AzureBlobStorageClient> fakeLogger;
        private AzureBlobStorageClient service;

        [SetUp]
        public void Setup()
        {
            fakeStorageConfig = A.Fake<IOptions<EssFulfilmentStorageConfiguration>>();
            fakeScsStorageService = A.Fake<ISalesCatalogueStorageService>();
            fakeazureMessageQueueHelper = A.Fake<IAzureMessageQueueHelper>();
            fakeLogger = A.Fake<ILogger<AzureBlobStorageClient>>(); 
            service = new AzureBlobStorageClient(fakeScsStorageService,fakeStorageConfig, fakeazureMessageQueueHelper, fakeLogger);
        }

        #region GetSalesCatalogueProductResponse

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
        public async Task WhenValidBatchId_ThenStoreSaleCatalogueServiceResponseAsyncReturnsTrue()
        {
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string uploadFileName = string.Concat(batchId, ".json");
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            CancellationToken cancellationToken = CancellationToken.None;
            CloudBlockBlob cloudBlockBlob;
            bool isSCSResponseAdded = true;
            string containerName = "ess-fulfilment";
            string callBackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";
            string accountName = "esstestdevstorage";
            string ScsStorageAccountAccessKeyValue = "sQRuRwZ/HRn5dVsitQIaOyv8nYg5qOjYk4Kq22SEeZO7qUJhjt57ND39WpkhV+KBqJhB+YtNo6cQ2QQ0gORJpQ==";
            string connectionString = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={ScsStorageAccountAccessKeyValue};EndpointSuffix=core.windows.net";
            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString())
                            .Returns(connectionString);
            cloudBlockBlob = service.GetCloudBlockBlob(uploadFileName, connectionString,containerName);
            await service.UploadSalesCatalogueServiceResponseToBlobAsync(cloudBlockBlob,salesCatalogueResponse);
            await service.AddQueueMessage(batchId,salesCatalogueResponse,callBackUri,correlationId,cloudBlockBlob);
            isSCSResponseAdded = await service.StoreSaleCatalogueServiceResponseAsync(containerName, batchId, salesCatalogueResponse, callBackUri, correlationId, cancellationToken);
            Assert.AreEqual(true, isSCSResponseAdded);
        }        
    }
}
