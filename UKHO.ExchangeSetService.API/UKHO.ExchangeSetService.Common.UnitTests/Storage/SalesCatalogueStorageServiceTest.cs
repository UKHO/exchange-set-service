using Microsoft.Extensions.Options;
using NUnit.Framework;
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
            { QueueName = "", StorageAccountKey = "", StorageAccountName = "test", StorageContainerName = "test" });

            salesCatalogueStorageService = new SalesCatalogueStorageService(fakeStorageConfig);
        }

        #region GetStorageAccountConnectionString

        [Test]
        public void WhenValidGetStorageAccountConnectionStringRequest_ThenReturnKeyNotFoundExceptionResponse()
        {
            fakeStorageConfig.Value.StorageAccountKey = "Test";
            var response = salesCatalogueStorageService.GetStorageAccountConnectionString();

            Assert.NotNull(response);
            Assert.AreEqual("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=Test;EndpointSuffix=core.windows.net", response);
        }
        #endregion
    }
}
