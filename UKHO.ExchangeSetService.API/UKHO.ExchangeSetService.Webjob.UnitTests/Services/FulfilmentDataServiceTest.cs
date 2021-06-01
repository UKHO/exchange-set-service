using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Storage;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentDataServiceTest
    {
        public IScsStorageService fakeScsStorageService;
        public IOptions<EssFulfilmentStorageConfiguration> fakeEssFulfilmentStorageConfiguration;
        public FulfilmentDataService fulfilmentDataService;

        [SetUp]
        public void Setup()
        {
            fakeScsStorageService = A.Fake<IScsStorageService>();
            fakeEssFulfilmentStorageConfiguration = Options.Create(new EssFulfilmentStorageConfiguration() 
                                                    { QueueName="",StorageAccountKey="",StorageAccountName="",StorageContainerName=""});

            fulfilmentDataService = new FulfilmentDataService(fakeScsStorageService,
                fakeEssFulfilmentStorageConfiguration);
        }


    }
}
