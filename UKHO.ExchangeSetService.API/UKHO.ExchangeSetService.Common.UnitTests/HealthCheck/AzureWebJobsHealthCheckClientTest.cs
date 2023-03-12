using FakeItEasy;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.HealthCheck;

namespace UKHO.ExchangeSetService.Common.UnitTests.HealthCheck
{
    public class AzureWebJobsHealthCheckClientTest
    {
        private IAzureWebJobsHealthCheckHttpClient fakeAzureWebJobsHealthCheckHttpClient;
        private AzureWebJobsHealthCheckClient azureWebJobsHealthCheckClient;

        private static readonly WebJobDetails[] OneJobOneInstance =
        {
            new() { ExchangeSetType = "small", Instance = 1 }
        };

        private static readonly WebJobDetails[] ThreeJobsTwoInstances = 
        {
            new() { ExchangeSetType = "small", Instance = 1 },
            new() { ExchangeSetType = "small", Instance = 2 },
            new() { ExchangeSetType = "medium", Instance = 1 },
            new() { ExchangeSetType = "medium", Instance = 2 },
            new() { ExchangeSetType = "large", Instance = 1 },
            new() { ExchangeSetType = "large", Instance = 2 }
        };

        private static readonly WebJobDetails[] ThreeJobsOneInstance =
        {
            new() { ExchangeSetType = "small", Instance = 1 },
            new() { ExchangeSetType = "medium", Instance = 1 },
            new() { ExchangeSetType = "large", Instance = 1 }
        };

        private static readonly WebJobDetails[] MultipleJobsMultipleInstances =
        {
            new() { ExchangeSetType = "small", Instance = 1 },
            new() { ExchangeSetType = "small", Instance = 2 },
            new() { ExchangeSetType = "medium", Instance = 1 },
            new() { ExchangeSetType = "large", Instance = 1 },
            new() { ExchangeSetType = "xlarge", Instance = 1 }
        };

        private static object[] _webJobsTestData =
        {
            new object[] { OneJobOneInstance },
            new object[] { ThreeJobsTwoInstances },
            new object[] { ThreeJobsOneInstance },
            new object[] { MultipleJobsMultipleInstances }
        };

        [SetUp]
        public void Setup()
        {
            this.fakeAzureWebJobsHealthCheckHttpClient = A.Fake<IAzureWebJobsHealthCheckHttpClient>();

            azureWebJobsHealthCheckClient =
                new AzureWebJobsHealthCheckClient(fakeAzureWebJobsHealthCheckHttpClient);
        }

        [TestCaseSource(nameof(_webJobsTestData))]
        public async Task WhenAllInstancesAreHealthy_ThenReturnHealthy(WebJobDetails[] webJobs)
        {   
            A.CallTo(() => fakeAzureWebJobsHealthCheckHttpClient.CheckHealth(A<WebJobDetails>.Ignored))
                .Returns(new HealthCheckResult(HealthStatus.Healthy, "Azure webjob is healthy"));

            var result = await azureWebJobsHealthCheckClient.CheckAllWebJobsHealth(webJobs.ToList());

            Assert.AreEqual(HealthCheckResult.Healthy().Status, result.Status);
        }

        [TestCaseSource(nameof(_webJobsTestData))]
        public async Task WhenAllInstancesAreUnHealthy_ThenReturnUnHealthy(WebJobDetails[] webJobs)
        {
            A.CallTo(() => fakeAzureWebJobsHealthCheckHttpClient.CheckHealth(A<WebJobDetails>.Ignored))
                .Returns(new HealthCheckResult(HealthStatus.Unhealthy, "Azure webjob is unhealthy"));

            var result = await azureWebJobsHealthCheckClient.CheckAllWebJobsHealth(webJobs.ToList());

            Assert.AreEqual(HealthCheckResult.Unhealthy().Status, result.Status);
        }

        [Test]
        public async Task WhenHealthCheckThrowsException_ThenReturnUnHealthy()
        {

            A.CallTo(() => fakeAzureWebJobsHealthCheckHttpClient.CheckHealth(A<WebJobDetails>.Ignored))
                .ThrowsAsync(new Exception(""));

            var result = await azureWebJobsHealthCheckClient.CheckAllWebJobsHealth(OneJobOneInstance.ToList());

            Assert.AreEqual(HealthCheckResult.Unhealthy().Status, result.Status);
        }

        [Test]
        public async Task WhenAllInstancesOfOneTypeAreUnHealthy_ThenReturnUnhealthy()
        {
            var description1 = "Azure small webjob, instance 1 is unhealthy";
            var description2 = "Azure small webjob, instance 2 is unhealthy";
            var message1 = "error 1";
            var message2 = "error 2";

            var expected = new HealthCheckResult(HealthStatus.Unhealthy, 
                string.Join(", ", description1, description2), 
                new Exception(string.Join(", ", message1, message2)));

            A.CallTo(() => fakeAzureWebJobsHealthCheckHttpClient.CheckHealth(
                    A<WebJobDetails>.That.Matches(
                        j => j.ExchangeSetType == "small" && j.Instance == 1)))
                .Returns(Task.FromResult(new  HealthCheckResult(HealthStatus.Unhealthy, description1, new Exception(message1))));

            A.CallTo(() => fakeAzureWebJobsHealthCheckHttpClient.CheckHealth(
                    A<WebJobDetails>.That.Matches(
                        j => j.ExchangeSetType == "small" && j.Instance == 2)))
                .Returns(Task.FromResult(new HealthCheckResult(HealthStatus.Unhealthy, description2, new Exception(message2))));

            A.CallTo(() => fakeAzureWebJobsHealthCheckHttpClient.CheckHealth(A<WebJobDetails>.That.Matches(j => j.ExchangeSetType != "small")))
                .Returns(Task.FromResult(new HealthCheckResult(HealthStatus.Healthy,
                    "Azure webjob is healthy")));

            var result = await azureWebJobsHealthCheckClient.CheckAllWebJobsHealth(ThreeJobsTwoInstances.ToList());

            Assert.AreEqual(expected.Status, result.Status);
            Assert.AreEqual(expected.Description, result.Description);
            Assert.AreEqual(expected.Exception?.Message, result.Exception?.Message);
        }

        [Test]
        public async Task WhenOneInstanceIsUnhealthyAndOtherIsHealthy_ThenReturnDegraded()
        {
            var description1 = "Azure small webjob, instance 1 is unhealthy";
            var message1 = "error 1";
           
            var expected = new HealthCheckResult(HealthStatus.Degraded,
                description1, new Exception(message1));
            
            // Type: small, Instance: 1, Unhealthy
            A.CallTo(() => fakeAzureWebJobsHealthCheckHttpClient.CheckHealth(
                    A<WebJobDetails>.That.Matches(
                        j => j.ExchangeSetType == "small" && j.Instance == 1)))
                .Returns(Task.FromResult(new HealthCheckResult(HealthStatus.Unhealthy, description1, new Exception(message1))));

            // Type: small, Instance: 2, Healthy
            A.CallTo(() => fakeAzureWebJobsHealthCheckHttpClient.CheckHealth(
                    A<WebJobDetails>.That.Matches(
                        j => j.ExchangeSetType == "small" && j.Instance == 2)))
                .Returns(Task.FromResult(new HealthCheckResult(HealthStatus.Healthy,
                    "Azure webjob is healthy")));

            // Rest are healthy
            A.CallTo(() => fakeAzureWebJobsHealthCheckHttpClient.CheckHealth(
                    A<WebJobDetails>.That.Matches(j => j.ExchangeSetType != "small")))
                .Returns(Task.FromResult(new HealthCheckResult(HealthStatus.Healthy,
                    "Azure webjob is healthy")));

            var result = await azureWebJobsHealthCheckClient.CheckAllWebJobsHealth(ThreeJobsTwoInstances.ToList());

            Assert.AreEqual(expected.Status, result.Status);
            Assert.AreEqual(expected.Description, result.Description);
            Assert.AreEqual(expected.Exception?.Message, result.Exception?.Message);
        }

        [Test]
        public async Task WhenOneSmallInstanceIsUnhealthySecondIsHealthyAndAllMediumAreUnhealthy_ThenReturnUnhealthy()
        {
            var descriptionS1 = "Azure small webjob, instance 1 is unhealthy";
            var messageS1 = "error 1";

            var descriptionM1 = "Azure medium webjob, instance 1 is unhealthy";
            var messageM1 = "error 1";

            var descriptionM2 = "Azure medium webjob, instance 2 is unhealthy";
            var messageM2 = "error 2";

            var expected = new HealthCheckResult(HealthStatus.Unhealthy,
                string.Join(", ", descriptionM1, descriptionM2, descriptionS1),
                new Exception(string.Join(", ", messageM1, messageM2, messageS1)));

            // Type: small, Instance: 1, Unhealthy
            A.CallTo(() => fakeAzureWebJobsHealthCheckHttpClient.CheckHealth(
                    A<WebJobDetails>.That.Matches(
                        j => j.ExchangeSetType == "small" && j.Instance == 1)))
                .Returns(Task.FromResult(new HealthCheckResult(HealthStatus.Unhealthy, descriptionS1, new Exception(messageS1))));

            // Type: small, Instance: 2, Healthy
            A.CallTo(() => fakeAzureWebJobsHealthCheckHttpClient.CheckHealth(
                    A<WebJobDetails>.That.Matches(
                        j => j.ExchangeSetType == "small" && j.Instance == 2)))
                .Returns(Task.FromResult(new HealthCheckResult(HealthStatus.Healthy,
                    "Azure webjob is healthy")));

            // Type: medium, Instance: 1, Unhealthy
            A.CallTo(() => fakeAzureWebJobsHealthCheckHttpClient.CheckHealth(
                    A<WebJobDetails>.That.Matches(j => j.ExchangeSetType == "medium" && j.Instance == 1)))
                .Returns(Task.FromResult(new HealthCheckResult(HealthStatus.Unhealthy, descriptionM1, new Exception(messageM1))));

            // Type: medium, Instance: 2, Unhealthy
            A.CallTo(() => fakeAzureWebJobsHealthCheckHttpClient.CheckHealth(
                    A<WebJobDetails>.That.Matches(j => j.ExchangeSetType == "medium" && j.Instance == 2)))
                .Returns(Task.FromResult(new HealthCheckResult(HealthStatus.Unhealthy, descriptionM2, new Exception(messageM2))));

            // All Large are healthy
            A.CallTo(() => fakeAzureWebJobsHealthCheckHttpClient.CheckHealth(
                    A<WebJobDetails>.That.Matches(j => j.ExchangeSetType == "large")))
                .Returns(Task.FromResult(new HealthCheckResult(HealthStatus.Healthy,
                    "Azure webjob is healthy")));

            var result = await azureWebJobsHealthCheckClient.CheckAllWebJobsHealth(ThreeJobsTwoInstances.ToList());

            Assert.AreEqual(expected.Status, result.Status);
            Assert.AreEqual(expected.Description, result.Description);
            Assert.AreEqual(expected.Exception?.Message, result.Exception?.Message);
        }
    }
}
