using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.ExchangeSetService.CleanUpJob.Configuration;
using UKHO.ExchangeSetService.CleanUpJob.Helpers;
using UKHO.ExchangeSetService.CleanUpJob.Services;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.Webjob.CleanUpJob.UnitTests.Services
{
    public class ExchangeSetCleanUpServiceTest
    {
        private IAzureFileSystemHelper _fakeAzureFileSystemHelper;
        private IOptions<EssFulfilmentStorageConfiguration> _fakeStorageConfig;
        private ISalesCatalogueStorageService _fakeScsStorageService;
        private IConfiguration _fakeConfiguration;
        private ILogger<ExchangeSetCleanUpService> _fakeLogger;
        private ExchangeSetCleanUpService _exchangeSetCleanUpService;
        private IOptions<CleanUpConfiguration> _fakeCleanUpConfig;
        private const string FakeFilePath = @"D:\\Downloads";
        private const string FakeStorageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
        private const int FakeNumberOfDays = 1;

        [SetUp]
        public void Setup()
        {
            _fakeAzureFileSystemHelper = A.Fake<IAzureFileSystemHelper>();
            _fakeStorageConfig = Options.Create(new EssFulfilmentStorageConfiguration { StorageContainerName = "Test" });
            _fakeScsStorageService = A.Fake<ISalesCatalogueStorageService>();
            _fakeConfiguration = A.Fake<IConfiguration>();
            _fakeLogger = A.Fake<ILogger<ExchangeSetCleanUpService>>();
            _fakeCleanUpConfig = Options.Create(new CleanUpConfiguration { NumberOfDays = 1 });

            _exchangeSetCleanUpService = new ExchangeSetCleanUpService(_fakeAzureFileSystemHelper, _fakeStorageConfig, _fakeScsStorageService, _fakeConfiguration, _fakeLogger, _fakeCleanUpConfig);
        }

        [Test]
        public void WhenScsStorageAccountAccessKeyValueNotFound_ThenReturnKeyNotFoundException()
        {
            var fakeAzureFileHelper = new FakeAzureFileHelper();

            A.CallTo(() => _fakeScsStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored))
              .Throws(new KeyNotFoundException("Storage account accesskey not found"));

            Assert.ThrowsAsync(Is.TypeOf<KeyNotFoundException>()
                   .And.Message.EqualTo("Storage account accesskey not found")
                    , async delegate { await _exchangeSetCleanUpService.DeleteHistoricFoldersAndFiles(); });

            Assert.That(fakeAzureFileHelper.DeleteDirectoryAsyncIsCalled, Is.EqualTo(false));
        }

        [Test]
        public async Task WhenHistoricFoldersAndFilesNotFound_ThenReturnFalseResponse()
        {
            var fakeAzureFileHelper = new FakeAzureFileHelper();

            A.CallTo(() => _fakeScsStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(FakeStorageAccountConnectionString);
            A.CallTo(() => _fakeAzureFileSystemHelper.DeleteDirectoryAsync(_fakeCleanUpConfig.Value.NumberOfDays, FakeStorageAccountConnectionString, _fakeStorageConfig.Value.StorageContainerName, FakeFilePath)).Returns(false);

            var response = await _exchangeSetCleanUpService.DeleteHistoricFoldersAndFiles();

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.EqualTo(false));
                Assert.That(fakeAzureFileHelper.DeleteDirectoryAsyncIsCalled, Is.EqualTo(false));
            });
        }

        [Test]
        public async Task WhenHistoricFoldersAndFilesFound_ThenReturnTrueResponse()
        {
            var fakeAzureFileHelper = new FakeAzureFileHelper();
            _fakeConfiguration["HOME"] = FakeFilePath;

            await fakeAzureFileHelper.DeleteDirectoryAsync(FakeNumberOfDays, FakeStorageAccountConnectionString, _fakeStorageConfig.Value.StorageContainerName, FakeFilePath);

            A.CallTo(() => _fakeScsStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(FakeStorageAccountConnectionString);
            A.CallTo(() => _fakeAzureFileSystemHelper.DeleteDirectoryAsync(_fakeCleanUpConfig.Value.NumberOfDays, FakeStorageAccountConnectionString, _fakeStorageConfig.Value.StorageContainerName, FakeFilePath)).Returns(true);

            var response = await _exchangeSetCleanUpService.DeleteHistoricFoldersAndFiles();

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.EqualTo(true));
                Assert.That(fakeAzureFileHelper.DeleteDirectoryAsyncIsCalled, Is.EqualTo(true));
            });
        }
    }
}
