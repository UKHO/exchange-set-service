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
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.Common.UnitTests.HealthCheck
{
    public class AzureBlobStorageHealthCheckTest
    {
        private IAzureBlobStorageClient fakeAzureBlobStorageClient;
        private ISalesCatalogueStorageService fakeSalesCatalogueStorageService;
        private IOptions<EssFulfilmentStorageConfiguration> fakeEssFulfilmentStorageConfiguration;
        private ILogger<AzureBlobStorageService> fakeLogger;
        private AzureBlobStorageHealthCheck azureBlobStorageHealthCheck;

        [SetUp]
        public void Setup()
        {
            this.fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();
            this.fakeLogger = A.Fake<ILogger<AzureBlobStorageService>>();
            this.fakeSalesCatalogueStorageService = A.Fake<ISalesCatalogueStorageService>();

            this.fakeEssFulfilmentStorageConfiguration = Options.Create(new EssFulfilmentStorageConfiguration()
            { QueueName = "testessdevqueue", StorageAccountKey = "testaccountkey", StorageAccountName = "testessdevstorage", StorageContainerName = "testContainer", DynamicQueueName = "testDynamicQueue", ExchangeSetTypes = "test" });

            azureBlobStorageHealthCheck = new AzureBlobStorageHealthCheck(fakeAzureBlobStorageClient, fakeSalesCatalogueStorageService, fakeEssFulfilmentStorageConfiguration, fakeLogger);
        }

        private (string, string) GetStorageAccountConnectionStringAndContainerName()
        {
            string storageAccountConnectionString = "DefaultEndpointsProtocol = https; AccountName = testessdevstorage2; AccountKey =testaccountkey; EndpointSuffix = core.windows.net";
            string containerName = "testContainer";
            return (storageAccountConnectionString, containerName);
        }

        [Test]
        public async Task WhenAzureBlobStorageContainerExists_ThenAzureBlobStorageServiceIsHealthy()
        {
            A.CallTo(() => fakeSalesCatalogueStorageService.GetStorageAccountConnectionString(string.Empty, string.Empty)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureBlobStorageClient.CheckBlobContainerHealth(A<string>.Ignored, A<string>.Ignored)).Returns(HealthCheckResult.Healthy("Azure blob storage is healthy"));

            var response = await azureBlobStorageHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.AreEqual(HealthStatus.Healthy, response.Status);
        }

        [Test]
        public async Task WhenAzureBlobStorageContainerNotExists_ThenAzureBlobStorageServiceIsUnhealthy()
        {
            A.CallTo(() => fakeSalesCatalogueStorageService.GetStorageAccountConnectionString(string.Empty, string.Empty)).Returns(GetStorageAccountConnectionStringAndContainerName().Item1);
            A.CallTo(() => fakeAzureBlobStorageClient.CheckBlobContainerHealth(A<string>.Ignored, A<string>.Ignored)).Returns(HealthCheckResult.Unhealthy("Azure blob storage is unhealthy", new Exception("Azure blob storage connection failed or not available")));

            var response = await azureBlobStorageHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.AreEqual(HealthStatus.Unhealthy, response.Status);
        }
    }
}