﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.Response;
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
        private ISmallExchangeSetInstance fakeSmallExchangeSetInstance;
        private IMediumExchangeSetInstance fakeMediumExchangeSetInstance;
        private ILargeExchangeSetInstance fakeLargeExchangeSetInstance;
        public string fakeExpiryDate = "2021-07-23T06:59:13Z";
        private readonly DateTime fakeScsRequestDateTime = DateTime.UtcNow;
        private readonly bool fakeIsEmptyEncExchangeSet = false;
        private static bool fakeIsEmptyAioExchangeSet = false;

        [SetUp]
        public void Setup()
        {
            fakeScsStorageService = A.Fake<ISalesCatalogueStorageService>();
            fakeStorageConfig = Options.Create(new EssFulfilmentStorageConfiguration()
            {
                QueueName = "",
                StorageAccountKey = "",
                StorageAccountName = "",
                StorageContainerName = "",
                DynamicQueueName = "ess-{0}-test",
                LargeExchangeSetAccountKey = "LargeExchangeSetAccountKey",
                LargeExchangeSetAccountName = "LargeExchangeSetAccountName",
                LargeExchangeSetInstance = 2,
                LargeExchangeSetSizeInMB = 300,
                MediumExchangeSetAccountKey = "MediumExchangeSetAccountKey",
                MediumExchangeSetAccountName = "MediumExchangeSetAccountName",
                MediumExchangeSetInstance = 3,
                SmallExchangeSetAccountKey = "SmallExchangeSetAccountKey",
                SmallExchangeSetAccountName = "SmallExchangeSetAccountName",
                SmallExchangeSetInstance = 2,
                SmallExchangeSetSizeInMB = 50
            });

            fakeAzureMessageQueueHelper = A.Fake<IAzureMessageQueueHelper>();
            fakeLogger = A.Fake<ILogger<AzureBlobStorageService>>();
            fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();
            fakeSmallExchangeSetInstance = A.Fake<ISmallExchangeSetInstance>();
            fakeMediumExchangeSetInstance = A.Fake<IMediumExchangeSetInstance>();
            fakeLargeExchangeSetInstance = A.Fake<ILargeExchangeSetInstance>();

            azureBlobStorageService = new AzureBlobStorageService(fakeScsStorageService, fakeStorageConfig,
                fakeAzureMessageQueueHelper, fakeLogger, fakeAzureBlobStorageClient, fakeSmallExchangeSetInstance,
                fakeMediumExchangeSetInstance, fakeLargeExchangeSetInstance);
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
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenCallStoreSaleCatalogueServiceResponseAsync_ThenReturnsTrue(ExchangeSetStandard exchangeSetStandard)
        {
            BlobClient fakeBlobClient = A.Fake<BlobClient>();

            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string containerName = "testContainer";
            string callBackUri = "https://essTest/myCallback?secret=test&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";
            string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            SalesCatalogueProductResponse salesCatalogueProductResponse = GetSalesCatalogueServiceResponse();
            var exchangeSetResponse = new ExchangeSetResponse
            {
                RequestedAioProductCount = 0,
                RequestedProductCount = 0
            };

            CancellationToken cancellationToken = CancellationToken.None;

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString(null, null)).Returns(storageAccountConnectionString);

            A.CallTo(() => fakeAzureBlobStorageClient.GetBlobClient(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(fakeBlobClient);

            A.CallTo(() => fakeSmallExchangeSetInstance.GetInstanceNumber(1)).Returns(3);

            A.CallTo(() => fakeBlobClient.Uri).Returns(new Uri("http://tempuri.org/blob"));

            A.CallTo(() => fakeBlobClient.UploadAsync(A<MemoryStream>.Ignored)).Returns(Task.FromResult(A.Dummy<Response<BlobContentInfo>>()));

            var response = await azureBlobStorageService.StoreSaleCatalogueServiceResponseAsync(containerName, batchId, salesCatalogueProductResponse, callBackUri, exchangeSetStandard.ToString(), correlationId, cancellationToken, fakeExpiryDate, fakeScsRequestDateTime, fakeIsEmptyEncExchangeSet, fakeIsEmptyAioExchangeSet, exchangeSetResponse);

            Assert.That(response, Is.True);
        }

        #endregion StoreSaleCatalogueServiceResponseAsync

        #region DownloadSalesCatalogueResponse

        [Test]
        public void WhenScsStorageAccountAccessKeyValueNotFound_ThenReturnKeyNotFoundException()
        {
            string scsResponseUri = "https://essTest/myCallback?secret=test&po=1234";
            string fakeBatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            A.CallTo(() => fakeScsStorageService.GetStorageSharedKeyCredentials())
              .Throws(new KeyNotFoundException("Storage account credentials missing from config"));

            Assert.ThrowsAsync(Is.TypeOf<KeyNotFoundException>()
                   .And.Message.EqualTo("Storage account credentials missing from config")
                    , async delegate { await azureBlobStorageService.DownloadSalesCatalogueResponse(scsResponseUri, fakeBatchId, null); });
        }

        [Test]
        public async Task WhenCallDownloadSalesCatalogueResponse_ThenReturnsSalesCatalogueProductResponse()
        {
            string scsResponseUri = "https://essTest/myCallback?secret=test&po=1234";
            string fakeBatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString("StorageAccountName", "StorageAccountKey")).Returns(storageAccountConnectionString);

            A.CallTo(() => fakeAzureBlobStorageClient.GetBlobClientByUri(A<string>.Ignored, A<StorageSharedKeyCredential>.Ignored)).Returns(new BlobClient(new System.Uri("http://tempuri.org/blob")));

            A.CallTo(() => fakeAzureBlobStorageClient.DownloadTextAsync(A<BlobClient>.Ignored)).Returns("{\"Products\":[{\"productName\":\"DE5NOBRK\",\"editionNumber\":1,\"updateNumbers\":[0,1],\"fileSize\":200}],\"ProductCounts\":{\"RequestedProductCount\":1,\"ReturnedProductCount\":1,\"RequestedProductsAlreadyUpToDateCount\":0,\"RequestedProductsNotReturned\":[]}}");

            A.CallTo(() => fakeSmallExchangeSetInstance.GetInstanceNumber(1)).Returns(3);
            var response = await azureBlobStorageService.DownloadSalesCatalogueResponse(scsResponseUri, fakeBatchId, null);

            Assert.That(response, Is.InstanceOf<SalesCatalogueProductResponse>());
            Assert.That(response.Products[0].ProductName,Is.EqualTo("DE5NOBRK"));
            Assert.That(response.Products[0].EditionNumber, Is.EqualTo(1));
            Assert.That(response.Products[0].UpdateNumbers[0].Value, Is.EqualTo(0));
        }

        #endregion DownloadSalesCatalogueResponse
    }
}
