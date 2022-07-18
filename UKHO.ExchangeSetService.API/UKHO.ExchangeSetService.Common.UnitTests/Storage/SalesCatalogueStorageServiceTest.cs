using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Collections.Generic;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.Common.UnitTests.Storage
{
    [TestFixture]
    public class SalesCatalogueStorageServiceTest
    {
        private IOptions<EssFulfilmentStorageConfiguration> fakeStorageConfig;
        private SalesCatalogueStorageService salesCatalogueStorageService;

        [SetUp]
        public void Setup()
        {
            fakeStorageConfig = Options.Create(new EssFulfilmentStorageConfiguration()
            {
                QueueName = "",
                StorageAccountKey = "",
                StorageAccountName = "test",
                StorageContainerName = "test",
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

            salesCatalogueStorageService = new SalesCatalogueStorageService(fakeStorageConfig);
        }

        #region GetStorageAccountConnectionString

        [Test]
        public void WhenValidGetStorageAccountConnectionStringRequest_ThenReturnValidResponse()
        {
            fakeStorageConfig.Value.StorageAccountKey = "Test";
            var response = salesCatalogueStorageService.GetStorageAccountConnectionString();

            Assert.NotNull(response);
            Assert.AreEqual("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=Test;EndpointSuffix=core.windows.net", response);
        }

        [Test]
        public void WhenScsStorageAccountAccessKeyValueNotfound_ThenGetStorageAccountConnectionStringReturnsKeyNotFoundException()
        {
            string expectedErrorMessage = "Storage account accesskey not found";

            var ex = Assert.Throws<KeyNotFoundException>(() => salesCatalogueStorageService.GetStorageAccountConnectionString(null, null));

            Assert.AreEqual(expectedErrorMessage, ex.Message);
        }

        #endregion
    }
}
