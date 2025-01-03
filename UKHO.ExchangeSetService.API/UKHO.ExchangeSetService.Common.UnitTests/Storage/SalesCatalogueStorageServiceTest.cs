using System.Collections.Generic;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.Common.UnitTests.Storage
{
    [TestFixture]
    public class SalesCatalogueStorageServiceTest
    {
        private IOptions<EssFulfilmentStorageConfiguration> _fakeStorageConfig;
        private SalesCatalogueStorageService _salesCatalogueStorageService;

        [SetUp]
        public void Setup()
        {
            _fakeStorageConfig = Options.Create(new EssFulfilmentStorageConfiguration()
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

            _salesCatalogueStorageService = new SalesCatalogueStorageService(_fakeStorageConfig);
        }

        #region GetStorageAccountConnectionString

        [Test]
        public void WhenValidGetStorageAccountConnectionStringRequest_ThenReturnValidResponse()
        {
            _fakeStorageConfig.Value.StorageAccountKey = "Test";
            var response = _salesCatalogueStorageService.GetStorageAccountConnectionString();

            Assert.That(response, Is.Not.Null);
            Assert.That(response, Is.EqualTo("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=Test;EndpointSuffix=core.windows.net"));
        }

        [Test]
        public void WhenScsStorageAccountAccessKeyValueNotfound_ThenGetStorageAccountConnectionStringReturnsKeyNotFoundException()
        {
            var expectedErrorMessage = "Storage account accesskey not found";

            var ex = Assert.Throws<KeyNotFoundException>(() => _salesCatalogueStorageService.GetStorageAccountConnectionString(null, null));

            Assert.That(expectedErrorMessage, Is.EqualTo(ex.Message));
        }

        #endregion
    }
}
