using FakeItEasy;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.HealthCheck;
using UKHO.ExchangeSetService.Common.Helpers;

namespace UKHO.ExchangeSetService.Common.UnitTests.HealthCheck
{
    public class FileShareServiceHealthCheckTest
    {
        private ILogger<FileShareService> fakeLogger;
        private IOptions<FileShareServiceConfiguration> fakeFileShareConfig;
        private IAuthFssTokenProvider fakeAuthFssTokenProvider;
        private IFileShareServiceClient fakeFileShareServiceClient;
        private FileShareServiceHealthCheck fileShareServiceHealthCheck;

        [SetUp]
        public void Setup()
        {
            this.fakeLogger = A.Fake<ILogger<FileShareService>>();
            this.fakeAuthFssTokenProvider = A.Fake<IAuthFssTokenProvider>();
            this.fakeFileShareConfig = Options.Create(new FileShareServiceConfiguration()
            { BaseUrl = "http://tempuri.org", CellName = "DE260001", EditionNumber = "1", Limit = 1, Start = 0, ProductCode = "AVCS", ProductLimit = 4, UpdateNumber = "0", UpdateNumberLimit = 10 });
            this.fakeFileShareServiceClient = A.Fake<IFileShareServiceClient>();

            fileShareServiceHealthCheck = new FileShareServiceHealthCheck(fakeFileShareServiceClient, fakeAuthFssTokenProvider, fakeFileShareConfig, fakeLogger);
        }

        #region GetFakeToken
        private static string GetFakeToken()
        {
            return "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJ0ZXN0IHNlcnZlciIsImlhdCI6MTU1ODMyOTg2MCwiZXhwIjoxNTg5OTUyMjYwLCJhdWQiOiJ3d3cudGVzdC5jb20iLCJzdWIiOiJ0ZXN0dXNlckB0ZXN0LmNvbSIsIm9pZCI6IjE0Y2I3N2RjLTFiYTUtNDcxZC1hY2Y1LWEwNDBkMTM4YmFhOSJ9.uOPTbf2Tg6M2OIC6bPHsBAOUuFIuCIzQL_MV3qV6agc";
        }
        #endregion

        [Test]
        public async Task WhenFSSClientReturnsOtherThan200_ThenFileShareServiceIsUnhealthy()
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("BadRequest"))) });

            var response = await fileShareServiceHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.AreEqual(HealthStatus.Unhealthy, response.Status);
        }

        [Test]
        public async Task WhenFSSClientReturns200_ThenFileShareServiceIsHealthy()
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("OK"))) });

            var response = await fileShareServiceHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.AreEqual(HealthStatus.Healthy, response.Status);
        }

        [Test]
        public async Task WhenFSSClientReturns503_ThenFileShareServiceIsUnhealthy()
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.ServiceUnavailable, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("ServiceUnavailable"))) });

            var response = await fileShareServiceHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.AreEqual(HealthStatus.Unhealthy, response.Status);
        }
    }
}
