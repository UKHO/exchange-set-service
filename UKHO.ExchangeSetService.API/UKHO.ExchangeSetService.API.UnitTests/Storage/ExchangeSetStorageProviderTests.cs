using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.API.UnitTests.Storage
{
    [TestFixture]
    public class ExchangeSetStorageProviderTests
    {
        private ExchangeSetStorageProvider service;
        private IOptions<EssFulfilmentStorageConfiguration> fakeStorageConfig;
        private IAzureBlobStorageService fakeAzureBlobStorageClient;

        [SetUp]
        public void Setup()
        {
            fakeStorageConfig = A.Fake<IOptions<EssFulfilmentStorageConfiguration>>();
            fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageService>();
            service = new ExchangeSetStorageProvider(fakeStorageConfig, fakeAzureBlobStorageClient);
        }

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
        public async Task WhenValidBatchId_ThenSaveSalesCatalogueResponseReturnsTrue()
        {           
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            CancellationToken cancellationToken = CancellationToken.None;
            bool isSCSResponseAdded = true;
            string callBackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";
            A.CallTo(() => fakeAzureBlobStorageClient.StoreSaleCatalogueServiceResponseAsync(A<string>.Ignored, A<string>.Ignored, A<SalesCatalogueProductResponse>.Ignored, A<string>.Ignored, A<string>.Ignored, cancellationToken)).Returns(true);
            isSCSResponseAdded = await service.SaveSalesCatalogueStorageDetails(salesCatalogueResponse, batchId, callBackUri, correlationId);
            Assert.AreEqual(true, isSCSResponseAdded);            
        }

        [Test]
        public async Task WhenInvalidBatchId_ThenSaveSalesCatalogueResponseReturnsFalse()
        {
            string batchId = null;
            string callBackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            CancellationToken cancellationToken = CancellationToken.None;           
            A.CallTo(() => fakeAzureBlobStorageClient.StoreSaleCatalogueServiceResponseAsync(A<string>.Ignored, A<string>.Ignored, A<SalesCatalogueProductResponse>.Ignored, A<string>.Ignored, A<string>.Ignored, cancellationToken)).Returns(false);
            bool isSCSResponseAdded = await service.SaveSalesCatalogueStorageDetails(salesCatalogueResponse, batchId, callBackUri, correlationId);
            Assert.AreEqual(false, isSCSResponseAdded);
        }
    }
}
