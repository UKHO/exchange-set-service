using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.CleanUpJob.Configuration;
using UKHO.ExchangeSetService.CleanUpJob.Helpers;
using UKHO.ExchangeSetService.CleanUpJob.Services;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.Webjob.CleanUpJob.UnitTests.Services
{
    public class ExchangeSetCleanUpServiceTest
    {
        public IAzureFileSystemHelper fakeAzureFileSystemHelper;
        public IOptions<EssFulfilmentStorageConfiguration> fakeStorageConfig;
        public ISalesCatalogueStorageService fakeScsStorageService;
        public IConfiguration fakeConfiguration;
        public ILogger<ExchangeSetCleanUpService> fakeLogger;
        public ExchangeSetCleanUpService exchangeSetCleanUpService;
        public IOptions<CleanUpConfig> fakeCleanUpConfig;
        public string fakeFilePath = @"D:\\Downloads";
        public string fakeStorageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";

        [SetUp]
        public void Setup()
        {
            fakeAzureFileSystemHelper = A.Fake<IAzureFileSystemHelper>();
            fakeStorageConfig = Options.Create(new EssFulfilmentStorageConfiguration()
                                         { StorageContainerName ="Test"  });
            fakeScsStorageService = A.Fake<ISalesCatalogueStorageService>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakeLogger = A.Fake<ILogger<ExchangeSetCleanUpService>>();
            fakeCleanUpConfig = Options.Create(new CleanUpConfig()
            { NumberOfDays = 1 });

            exchangeSetCleanUpService = new ExchangeSetCleanUpService(fakeAzureFileSystemHelper, fakeStorageConfig, fakeScsStorageService, fakeConfiguration, fakeLogger, fakeCleanUpConfig);
        }

        [Test]
        public void WhenScsStorageAccountAccessKeyValueNotFound_ThenReturnKeyNotFoundException()
        {
            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored))
              .Throws(new KeyNotFoundException("Storage account accesskey not found"));

            Assert.ThrowsAsync(Is.TypeOf<KeyNotFoundException>()
                   .And.Message.EqualTo("Storage account accesskey not found")
                    , async delegate { await exchangeSetCleanUpService.DeleteHistoricFoldersAndFiles(); });
        }

        [Test]
        public async Task WhenHistoricFoldersAndFilesNotFound_ThenReturnFalseResponse()
        {
            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(fakeStorageAccountConnectionString);
            A.CallTo(() => fakeAzureFileSystemHelper.DeleteDirectoryAsync(fakeCleanUpConfig.Value.NumberOfDays, fakeStorageAccountConnectionString, fakeStorageConfig.Value.StorageContainerName, fakeFilePath)).Returns(false);

            var response = await exchangeSetCleanUpService.DeleteHistoricFoldersAndFiles();

            Assert.AreEqual(false, response);
        }

        [Test]
        public async Task WhenHistoricFoldersAndFilesFound_ThenReturnTrueResponse()
        {
            fakeConfiguration["HOME"] = @"D:\\Downloads";

            A.CallTo(() => fakeScsStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(fakeStorageAccountConnectionString);
            A.CallTo(() => fakeAzureFileSystemHelper.DeleteDirectoryAsync(fakeCleanUpConfig.Value.NumberOfDays, fakeStorageAccountConnectionString, fakeStorageConfig.Value.StorageContainerName, fakeFilePath)).Returns(true);

            var response = await exchangeSetCleanUpService.DeleteHistoricFoldersAndFiles();

            Assert.AreEqual(true, response);
        }
    }
}
