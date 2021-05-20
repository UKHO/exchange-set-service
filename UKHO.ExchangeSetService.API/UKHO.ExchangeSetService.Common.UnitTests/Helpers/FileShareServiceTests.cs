using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers
{
    public class FileShareServiceTests
    {
        private ILogger<FileShareService> fakeLogger;
        private IOptions<FileShareServiceConfiguration> fakeFileShareConfig;
        private IAuthTokenProvider fakeAuthTokenProvider;
        private IFileShareServiceClient fakeFileShareServiceClient;
        private IFileShareService fileShareService;

        [SetUp]
        public void Setup()
        {
            this.fakeLogger = A.Fake<ILogger<FileShareService>>();
            this.fakeAuthTokenProvider = A.Fake<IAuthTokenProvider>();
            this.fakeFileShareConfig = Options.Create(new FileShareServiceConfiguration() { BaseUrl = "" });
            this.fakeFileShareServiceClient = A.Fake<IFileShareServiceClient>();


            fileShareService = new FileShareService(fakeFileShareServiceClient, fakeAuthTokenProvider, fakeFileShareConfig, fakeLogger);
        }

        #region GetCreateBatchResponse
        private static CreateBatchResponseModel GetCreateBatchResponse()
        {
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            return new CreateBatchResponseModel()
            {
                BatchId = batchId,
                BatchStatusUri = $"/batch/{batchId}",
                ExchangeSetFileUri = $"/batch/{batchId}/files/",
                BatchExpiryDateTime = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
            };
        }
        #endregion GetCreateBatchResponse

        #region GetFakeToken
        private static string GetFakeToken()
        {
            return "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJ0ZXN0IHNlcnZlciIsImlhdCI6MTU1ODMyOTg2MCwiZXhwIjoxNTg5OTUyMjYwLCJhdWQiOiJ3d3cudGVzdC5jb20iLCJzdWIiOiJ0ZXN0dXNlckB0ZXN0LmNvbSIsIm9pZCI6IjE0Y2I3N2RjLTFiYTUtNDcxZC1hY2Y1LWEwNDBkMTM4YmFhOSJ9.uOPTbf2Tg6M2OIC6bPHsBAOUuFIuCIzQL_MV3qV6agc";
        }
        #endregion

        #region CreateBatch
        [Test]
        public async Task WhenFSSClientReturnsOtherThan201_ThenCreateBatchReturnsSameStatusAndNullInResponse()
        {
            A.CallTo(() => fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request"))) });

            var response = await fileShareService.CreateBatch();
            Assert.AreEqual(HttpStatusCode.BadRequest, response.ResponseCode, $"Expected {HttpStatusCode.BadRequest} got {response.ResponseCode}");
            Assert.IsNull(response.ResponseBody);
        }

        [Test]
        public async Task WhenFSSClientReturns201_ThenCreateBatchReturns201AndDataInResponse()
        {
            var createBatchResponse = GetCreateBatchResponse();
            var jsonString = JsonConvert.SerializeObject(createBatchResponse);
            A.CallTo(() => fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());

            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.Created, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);

            var response = await fileShareService.CreateBatch();
 
            Assert.AreEqual(HttpStatusCode.Created, response.ResponseCode, $"Expected {HttpStatusCode.Created} got {response.ResponseCode}");
            Assert.AreEqual(createBatchResponse.BatchId, response.ResponseBody.BatchId);
            Assert.AreEqual(createBatchResponse.BatchStatusUri, response.ResponseBody.BatchStatusUri);
            Assert.AreEqual(createBatchResponse.ExchangeSetFileUri, response.ResponseBody.ExchangeSetFileUri);
        }

        #endregion CreateBatch
    }
}
