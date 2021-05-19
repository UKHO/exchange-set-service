using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Request;
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
        private IAuthTokenProvider jwtToken { get; set; }

        [SetUp]
        public void Setup()
        {
            this.fakeLogger = A.Fake<ILogger<FileShareService>>();
            this.fakeAuthTokenProvider = A.Fake<IAuthTokenProvider>();
            this.fakeFileShareConfig = Options.Create(new FileShareServiceConfiguration() { BaseUrl = "" });
            this.fakeFileShareServiceClient = A.Fake<IFileShareServiceClient>();


            fileShareService = new FileShareService(fakeFileShareServiceClient, fakeAuthTokenProvider, fakeFileShareConfig, fakeLogger);
        }

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

        #region CreateBatch
        [Test]
        public async Task WhenFSSClientReturnsOtherThan201_ThenCreateBatchReturnsInternalServerError()
        {
            string token = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Im5PbzNaRHJPRFhFSzFqS1doWHNsSFJfS1hFZyIsImtpZCI6Im5PbzNaRHJPRFhFSzFqS1doWHNsSFJfS1hFZyJ9.eyJhdWQiOiI1YzJmNmRmNC0zMmI4LTQyYzgtOWI1Yi0zZjZjMzRiM2RkNGYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjIxMjU3MjMyLCJuYmYiOjE2MjEyNTcyMzIsImV4cCI6MTYyMTI2MTEzMiwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQUE1N2lSeUJpbGtWMUc4M2I3bDg5Z3JuUUVKTjRhWHJCZXdieDU4cVlCWUt4Sm8zQzBZVU1BcTFhVEFrMU5LTHRtN1RIeVZXU0JwRXdBYzQyd2ZhN0hqWVNZWnJaRnZNbDQzNXJnUzNDQUhiNDExOStKNmZoVElod0ZIbjhVaG9KVXgvbVdydUhuejNxYW1uTWg5OUkwTEFhRUVYWUhMR0Y3ODJFc2pBS0h3Yz0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiNWMyZjZkZjQtMzJiOC00MmM4LTliNWItM2Y2YzM0YjNkZDRmIiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJnYW5wYXQuZ2F3ZGVAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiZ2F3ZGUiLCJnaXZlbl9uYW1lIjoiZ2FucGF0IiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy43NC4xOS4xODIiLCJuYW1lIjoiR2FucGF0IEdhd2RlIiwib2lkIjoiMTk5ZTI1OTUtN2ZlNC00YmM0LWI4MDctOGRmMTAxMjNiZTA2IiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFB2UnRMMXk0TXNoQ20xc19iRFN6M1U4Q0FCQS4iLCJyb2xlcyI6WyJFeGNoYW5nZVNlcnZpY2VSZWFkZXIiLCJDYXRhbG9ndWVSZWFkZXIiXSwic2NwIjoidXNlcl9pbXBlcnNvbmF0aW9uIiwic3ViIjoiWUpyY0l3TWFVVWMxd1Z3bUNrZVZocUFsdE45MFhMM3N5ckFsaWNaMmY3VSIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoiZ2FucGF0Lmdhd2RlQG1hc3Rlay5jb20iLCJ1dGkiOiJTS1V3eHRLSFlFeThLWkZ3SkVkYkFBIiwidmVyIjoiMS4wIn0.g0i5UnttfuHzrn53g28zOwKXcGi-j4ccoQ8MRFnBXdFRZHYzepVCCMJkTI2KKwpcpP_ULQspnuZVkkk1-tKZo8YWnrY2NroxQx284hxmGt07RFi_I6r8EymBnP86myygThYt-nbtLVbedVcK0Gt17shP_Saf5EWpZwrZ7YDJNvTetD5o_s0xME5PITu50gDqVS_MAW9B4VSf1qw405-z3Fm-B6NtEYNui71hTrI_uaQePuv1Fb3feef9yG6cCbCbSBtd0whv4WdipWEc6IwQ5Rk6NkwMsfLH3hTsfJwjg55tWOaY08yW4-6SPL_Qx98YpDiluP9ZuDcWXz0SNdgo1g";

            A.CallTo(() => fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(token);
            
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.InternalServerError, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Internal Server Error"))) });

            var response = await fileShareService.CreateBatch();
            
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.ResponseCode, $"Expected {HttpStatusCode.InternalServerError} got {response.ResponseCode}");
            Assert.IsNull(response.ResponseBody);
        }

        [Test]
        public async Task WhenFSSClientReturns201_ThenCreateBatchReturns201AndDataInResponse()
        {
            CreateBatchResponseModel fssResponse = GetCreateBatchResponse();
            var jsonString = JsonConvert.SerializeObject(fssResponse);
            
            string token = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Im5PbzNaRHJPRFhFSzFqS1doWHNsSFJfS1hFZyIsImtpZCI6Im5PbzNaRHJPRFhFSzFqS1doWHNsSFJfS1hFZyJ9.eyJhdWQiOiI1YzJmNmRmNC0zMmI4LTQyYzgtOWI1Yi0zZjZjMzRiM2RkNGYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjIxMjU3MjMyLCJuYmYiOjE2MjEyNTcyMzIsImV4cCI6MTYyMTI2MTEzMiwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQUE1N2lSeUJpbGtWMUc4M2I3bDg5Z3JuUUVKTjRhWHJCZXdieDU4cVlCWUt4Sm8zQzBZVU1BcTFhVEFrMU5LTHRtN1RIeVZXU0JwRXdBYzQyd2ZhN0hqWVNZWnJaRnZNbDQzNXJnUzNDQUhiNDExOStKNmZoVElod0ZIbjhVaG9KVXgvbVdydUhuejNxYW1uTWg5OUkwTEFhRUVYWUhMR0Y3ODJFc2pBS0h3Yz0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiNWMyZjZkZjQtMzJiOC00MmM4LTliNWItM2Y2YzM0YjNkZDRmIiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJnYW5wYXQuZ2F3ZGVAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiZ2F3ZGUiLCJnaXZlbl9uYW1lIjoiZ2FucGF0IiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy43NC4xOS4xODIiLCJuYW1lIjoiR2FucGF0IEdhd2RlIiwib2lkIjoiMTk5ZTI1OTUtN2ZlNC00YmM0LWI4MDctOGRmMTAxMjNiZTA2IiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFB2UnRMMXk0TXNoQ20xc19iRFN6M1U4Q0FCQS4iLCJyb2xlcyI6WyJFeGNoYW5nZVNlcnZpY2VSZWFkZXIiLCJDYXRhbG9ndWVSZWFkZXIiXSwic2NwIjoidXNlcl9pbXBlcnNvbmF0aW9uIiwic3ViIjoiWUpyY0l3TWFVVWMxd1Z3bUNrZVZocUFsdE45MFhMM3N5ckFsaWNaMmY3VSIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoiZ2FucGF0Lmdhd2RlQG1hc3Rlay5jb20iLCJ1dGkiOiJTS1V3eHRLSFlFeThLWkZ3SkVkYkFBIiwidmVyIjoiMS4wIn0.g0i5UnttfuHzrn53g28zOwKXcGi-j4ccoQ8MRFnBXdFRZHYzepVCCMJkTI2KKwpcpP_ULQspnuZVkkk1-tKZo8YWnrY2NroxQx284hxmGt07RFi_I6r8EymBnP86myygThYt-nbtLVbedVcK0Gt17shP_Saf5EWpZwrZ7YDJNvTetD5o_s0xME5PITu50gDqVS_MAW9B4VSf1qw405-z3Fm-B6NtEYNui71hTrI_uaQePuv1Fb3feef9yG6cCbCbSBtd0whv4WdipWEc6IwQ5Rk6NkwMsfLH3hTsfJwjg55tWOaY08yW4-6SPL_Qx98YpDiluP9ZuDcWXz0SNdgo1g";

            A.CallTo(() => fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(token);
            
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.Created, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };
            
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);

            var response = await fileShareService.CreateBatch();
            
            Assert.AreEqual(HttpStatusCode.Created, response.ResponseCode, $"Expected {HttpStatusCode.Created} got {response.ResponseCode}");
            Assert.AreEqual(jsonString, JsonConvert.SerializeObject(response.ResponseBody));
        }

        #endregion CreateBatch
    }
}
