using FakeItEasy;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.HealthCheck;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.Common.UnitTests.HealthCheck
{
    public class AzureMessageQueueHealthCheckTest
    {
        private IAzureMessageQueueHelper fakeAzureMessageQueueHelperClient;
        private ISalesCatalogueStorageService fakeSalesCatalogueStorageService;
        private IOptions<EssFulfilmentStorageConfiguration> fakeEssFulfilmentStorageConfiguration;
        private ILogger<AzureMessageQueueHelper> fakeLogger;
        private AzureMessageQueueHealthCheck azureMessageQueueHealthCheck;
        private IAzureBlobStorageService fakeAzureBlobStorageService;

        [SetUp]
        public void Setup()
        {
            this.fakeAzureMessageQueueHelperClient = A.Fake<IAzureMessageQueueHelper>();
            this.fakeLogger = A.Fake<ILogger<AzureMessageQueueHelper>>();
            this.fakeSalesCatalogueStorageService = A.Fake<ISalesCatalogueStorageService>();
            this.fakeAzureBlobStorageService = A.Fake<IAzureBlobStorageService>();

            this.fakeEssFulfilmentStorageConfiguration = Options.Create(new EssFulfilmentStorageConfiguration()
            { QueueName = "testessdevqueue", StorageAccountKey = "testaccountkey", StorageAccountName = "testessdevstorage", StorageContainerName = "testContainer", DynamicQueueName = "testDynamicQueue", ExchangeSetTypes= "test", WebAppVersion = "" });

            azureMessageQueueHealthCheck = new AzureMessageQueueHealthCheck(fakeAzureMessageQueueHelperClient, fakeSalesCatalogueStorageService, fakeEssFulfilmentStorageConfiguration, fakeLogger, fakeAzureBlobStorageService);
        }

        private (string, string) GetStorageAccountConnectionStringAndContainerName()
        {
            string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            string containerName = "testContainer";
            return (storageAccountConnectionString, containerName);
        }

        [Test]
        public async Task WhenAzureMessageQueueExists_ThenAzureMessageQueueIsHealthy()
        {
            A.CallTo(() => fakeSalesCatalogueStorageService.GetStorageAccountConnectionString(string.Empty,string.Empty)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureMessageQueueHelperClient.CheckMessageQueueHealth(A<string>.Ignored, A<string>.Ignored)).Returns(new HealthCheckResult(HealthStatus.Healthy, "Azure message queue is healthy"));
            A.CallTo(() => fakeAzureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(A<ExchangeSetType>.Ignored)).Returns(1);

            var response = await azureMessageQueueHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(HealthStatus.Healthy, Is.EqualTo(response.Status));
        }

        [Test]
        public async Task WhenAzureMessageQueueNotExists_ThenAzureMessageQueueIsUnhealthy()
        {
            A.CallTo(() => fakeSalesCatalogueStorageService.GetStorageAccountConnectionString(string.Empty, string.Empty)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureMessageQueueHelperClient.CheckMessageQueueHealth(A<string>.Ignored, A<string>.Ignored)).Returns(new HealthCheckResult(HealthStatus.Unhealthy, "Azure message queue is unhealthy", new Exception("Azure message queue is unhealthy")));
            A.CallTo(() => fakeAzureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(A<ExchangeSetType>.Ignored)).Returns(1);

            var response = await azureMessageQueueHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(HealthStatus.Unhealthy, Is.EqualTo(response.Status));
        }

        [Test]
        public async Task WhenGetInstanceCountBasedOnExchangeSetTypeThrowsException_ThenAzureMessageQueueIsUnhealthy()
        {
            A.CallTo(() => fakeAzureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(A<ExchangeSetType>.Ignored)).Throws<Exception>();

            var response = await azureMessageQueueHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(HealthStatus.Unhealthy, Is.EqualTo(response.Status));
        }

        [Test]
        public async Task WhenGetStorageAccountConnectionStringThrowsException_ThenAzureMessageQueueIsUnhealthy()
        {
            A.CallTo(() => fakeAzureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(A<ExchangeSetType>.Ignored)).Returns(1);
            A.CallTo(() => fakeSalesCatalogueStorageService.GetStorageAccountConnectionString(string.Empty, string.Empty)).Throws<Exception>();

            var response = await azureMessageQueueHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(HealthStatus.Unhealthy, Is.EqualTo(response.Status));
        }

        [Test]
        public async Task WhenCheckMessageQueueHealthThrowsException_ThenAzureMessageQueueIsUnhealthy()
        {
            A.CallTo(() => fakeAzureBlobStorageService.GetInstanceCountBasedOnExchangeSetType(A<ExchangeSetType>.Ignored)).Returns(1);
            A.CallTo(() => fakeSalesCatalogueStorageService.GetStorageAccountConnectionString(string.Empty, string.Empty)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureMessageQueueHelperClient.CheckMessageQueueHealth(A<string>.Ignored, A<string>.Ignored)).Throws<Exception>();

            var response = await azureMessageQueueHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(HealthStatus.Unhealthy, Is.EqualTo(response.Status));
        }
    }
}
