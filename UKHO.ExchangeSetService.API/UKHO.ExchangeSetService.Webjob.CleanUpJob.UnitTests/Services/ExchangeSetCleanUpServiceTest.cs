using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.CleanUpJob.Services;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.Webjob.CleanUpJob.UnitTests.Services
{
    public class ExchangeSetCleanUpServiceTest
    {
        public IAzureBlobStorageClient fakeAzureBlobStorageClient;
        public IOptions<EssFulfilmentStorageConfiguration> fakeStorageConfig;
        public ISalesCatalogueStorageService fakeScsStorageService;
        public IConfiguration fakeConfiguration;
        public ILogger<ExchangeSetCleanUpService> fakeLogger;
        public ExchangeSetCleanUpService exchangeSetCleanUpService;
        public string fakeFilePath = @"D:\\Downloads";

        [SetUp]
        public void Setup()
        {
            fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();
            fakeStorageConfig = Options.Create(new EssFulfilmentStorageConfiguration()
                                         { StorageContainerName ="Test"  });
            fakeScsStorageService = A.Fake<ISalesCatalogueStorageService>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakeLogger = A.Fake<ILogger<ExchangeSetCleanUpService>>();

            exchangeSetCleanUpService = new ExchangeSetCleanUpService(fakeAzureBlobStorageClient, fakeStorageConfig, fakeScsStorageService, fakeConfiguration, fakeLogger);
        }

        [Test]
        public void WhenScsStorageAccountAccessKeyValueNotFound_ThenReturnKeyNotFoundException()
        {
            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString())
              .Throws(new KeyNotFoundException("Storage account accesskey not found"));

            Assert.ThrowsAsync(Is.TypeOf<KeyNotFoundException>()
                   .And.Message.EqualTo("Storage account accesskey not found")
                    , async delegate { await exchangeSetCleanUpService.DeleteHistoricFoldersAndFiles(); });
        }

        [Test]
        public async Task WhenHistoricFoldersAndFilesNotFound_ThenReturnFalseResponse()
        {
            string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            string filePath = @"D:\\Downloads";

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString()).Returns(storageAccountConnectionString);
            A.CallTo(() => fakeAzureBlobStorageClient.DeleteDirectoryAsync(storageAccountConnectionString, fakeStorageConfig.Value.StorageContainerName, filePath)).Returns(false);

            var response = await exchangeSetCleanUpService.DeleteHistoricFoldersAndFiles();

            Assert.AreEqual(false, response);
        }

        [Test]
        public async Task WhenHistoricFoldersAndFilesFound_ThenReturnTrueResponse()
        {
            string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            fakeConfiguration["HOME"] = @"D:\\Downloads";

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString()).Returns(storageAccountConnectionString);
            A.CallTo(() => fakeAzureBlobStorageClient.DeleteDirectoryAsync(storageAccountConnectionString, fakeStorageConfig.Value.StorageContainerName, fakeFilePath)).Returns(true);

            var response = await exchangeSetCleanUpService.DeleteHistoricFoldersAndFiles();

            Assert.AreEqual(true, response);
        }
    }
}
