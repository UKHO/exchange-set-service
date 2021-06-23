using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using Attribute = UKHO.ExchangeSetService.Common.Models.FileShareService.Response.Attribute;


namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers
{
    public class FileShareServiceTests
    {
        private ILogger<FileShareService> fakeLogger;
        private IOptions<FileShareServiceConfiguration> fakeFileShareConfig;
        private IAuthTokenProvider fakeAuthTokenProvider;
        private IFileShareServiceClient fakeFileShareServiceClient;
        private IFileShareService fileShareService;
        public string fakeFilePath = "C:\\HOME\\test.txt";
        public string fakeFolderPath = "C:\\HOME";

        [SetUp]
        public void Setup()
        {
            this.fakeLogger = A.Fake<ILogger<FileShareService>>();
            this.fakeAuthTokenProvider = A.Fake<IAuthTokenProvider>();
            this.fakeFileShareConfig = Options.Create(new FileShareServiceConfiguration()
                                       { BaseUrl = "http://tempuri.org", CellName = "DE260001", EditionNumber = "1", Limit = 10, Start = 0, ProductCode = "AVCS", ProductLimit = 4, UpdateNumber = "0", UpdateNumberLimit = 10 });
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
                BatchStatusUri = $"http://tempuri.org/batch/{batchId}",
                ExchangeSetFileUri = $"http://tempuri.org/batch/{batchId}/files/",
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

        #region GetProductdetails
        private List<Products> GetProductdetails()
        {
            return new List<Products> {
                            new Products {
                                ProductName = "DE416050",
                                EditionNumber = 0,
                                UpdateNumbers = new List<int?> {0},
                                FileSize = 400
                            }
                        };
        }
        #endregion

        #region GetSearchBatchResponse
        private SearchBatchResponse GetSearchBatchResponse()
        {
            return new SearchBatchResponse()
            {
                Entries = new List<BatchDetail>() {
                    new BatchDetail {
                        BatchId ="63d38bde-5191-4a59-82d5-aa22ca1cc6dc",
                        Files= new List<BatchFile>(){ new BatchFile { Filename = "test.txt", FileSize = 400, Links = new Links { Get = new Link { Href = "" }}}},
                        Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "DE" } ,
                                                           new Attribute { Key= "CellName", Value= "DE416050" },
                                                           new Attribute { Key= "EditionNumber", Value= "0" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "AVCS" }}
                    } },
                Links = new PagingLinks(),
                Count = 0,
                Total = 0
            };
        }
        #endregion

        #region GetReadMeFileDetails
        private String GetReadMeFileDetails()
        {
            StringBuilder sb = new StringBuilder();            
            string lineTwo = "Version: Published Week 22 / 21 dated 03 - 06 - 2021";
            string lineThree = "This file was last updated 3 - Jun - 2021";            
            sb.AppendLine("AVCS README");
            sb.AppendLine(lineTwo);
            sb.AppendLine(lineThree);
            return sb.ToString();
        }
        #endregion

        #region CreateBatch
        [Test]
        public async Task WhenFSSClientReturnsOtherThan201_ThenCreateBatchReturnsSameStatusAndNullInResponse()
        {
            A.CallTo(() => fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request"))) });

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

        [Test]
        public async Task WhenFSSApiIsCalledForCreateBatch_ThenValidateCorrectParametersArePassed()
        {
            //Data
            string actualAccessToken = GetFakeToken();
            string postBodyParam = "This should be replace by actual value when param passed to api call";

            //Test variable
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            var createBatchResponse = GetCreateBatchResponse();
            var jsonString = JsonConvert.SerializeObject(createBatchResponse);

            //Mock
            A.CallTo(() => fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(actualAccessToken);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Invokes((HttpMethod method, string postBody, string accessToken, string uri) =>
                {
                    accessTokenParam = accessToken;
                    uriParam = uri;
                    httpMethodParam = method;
                    postBodyParam = postBody;
                })
                .Returns(httpResponse);

            //Method call
            var response = await fileShareService.CreateBatch();

            //Test
            Assert.AreEqual(response.ResponseCode, HttpStatusCode.OK);
            Assert.AreEqual(HttpMethod.Post, httpMethodParam);
            Assert.AreEqual(actualAccessToken, accessTokenParam);
        }

        #endregion CreateBatch

        #region GetBatchInfoBasedOnProducts
        [Test]
        public async Task WhenFSSClientReturnsOtherThan201_ThenGetBatchInfoBasedOnProductsReturnsNullResponse()
        {
            A.CallTo(() => fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request"))) });

            var response = await fileShareService.GetBatchInfoBasedOnProducts(GetProductdetails());
            Assert.AreEqual(0, response.Entries.Count);
        }

        [Test]
        public async Task WhenGetBatchInfoBasedOnProducts_ThenReturnsSearchBatchResponse()
        {
            string postBodyParam = "This should be replace by actual value when param passed to api call";

            //Test variable
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            var searchBatchResponse = GetSearchBatchResponse();
            var jsonString = JsonConvert.SerializeObject(searchBatchResponse);

            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };

            A.CallTo(() => fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
               })
               .Returns(httpResponse);

            var response = await fileShareService.GetBatchInfoBasedOnProducts(GetProductdetails());

            Assert.IsNotNull(response);
            Assert.IsInstanceOf(typeof(SearchBatchResponse), response);
            Assert.AreEqual("63d38bde-5191-4a59-82d5-aa22ca1cc6dc", response.Entries[0].BatchId);
        }

        #endregion GetBatchInfoBasedOnProducts

        #region DownloadBatchFile
        [Test]
        public async Task WhenGetBatchInfoBasedOnProductsReturns200_ThenDownloadBatchFiles()
        {
            A.CallTo(() => fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, RequestMessage = new HttpRequestMessage() { 
                     RequestUri = new Uri("http://test.com") 
                 }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Received Fulfilment Data Successfully!!!!"))) 
                 });

            var response = await fileShareService.DownloadBatchFiles(new List<string> { fakeFilePath }, fakeFolderPath);

            Assert.IsNotNull(response);
            Assert.IsInstanceOf(typeof(bool), response);
        }

        [Test]
        public async Task WhenGetBatchInfoBasedOnProductsReturnsOtherThan200_ThenDonotDownloadBatchFiles()
        {
            A.CallTo(() => fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage()
                 {
                     StatusCode = HttpStatusCode.BadRequest,
                     RequestMessage = new HttpRequestMessage()
                     {
                         RequestUri = new Uri("http://test.com")
                     },
                     Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request")))
                 });

            var response = await fileShareService.DownloadBatchFiles(new List<string> { fakeFilePath }, fakeFolderPath);

            Assert.IsNotNull(response);
            Assert.IsInstanceOf(typeof(bool), response);
            Assert.IsFalse(response);
        }
        #endregion

        #region SearchReadMeFilePath
        [Test]
        public async Task WhenInvalidSearchReadMeFileRequest_ThenReturnEmptyFilePath()
        {
            A.CallTo(() => fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request"))) });

            var response = await fileShareService.SearchReadMeFilePath(string.Empty);
            Assert.AreEqual(string.Empty, response);
        
        }
        [Test]
        public async Task WhenValidSearchReadMeFileRequest_ThenReturnValidFilePath()
        {
            string postBodyParam = "This should be replace by actual value when param passed to api call";
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            string batchId = "a07537ff-ffa2-4565-8f0e-96e61e70a9fc";
            var searchReadMeFileName = @"batch/a07537ff-ffa2-4565-8f0e-96e61e70a9fc/files/README.TXT";

            var searchBatchResponse = GetSearchBatchResponse();
            var jsonString = JsonConvert.SerializeObject(searchBatchResponse);
           
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };

            A.CallTo(() => fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
               })
               .Returns(httpResponse);

            var response = await fileShareService.SearchReadMeFilePath(batchId);
            string expectedReadMeFilePath = @"batch/a07537ff-ffa2-4565-8f0e-96e61e70a9fc/files/README.TXT";
            Assert.IsNotNull(response);
            Assert.AreEqual(expectedReadMeFilePath, searchReadMeFileName);
        }
        #endregion SearchReadMeFilePath

        #region DownloadReadMeFile

        [Test]
        public async Task WhenValidDownloadReadMeFileRequest_ThenReturnTrueResponse()
        {
            string postBodyParam = "This should be replace by actual value when param passed to api call";
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            string batchId = "c4af46f5-1b41-4294-93f9-dda87bf8ab96";

            fakeFileShareConfig.Value.ReadMeFileName = "ReadMe.txt";

            string readMeFilePath = @"batch/c4af46f5-1b41-4294-93f9-dda87bf8ab96/files/README.TXT";
            string exchangeSetRootPath = @"C:\\HOME";

            A.CallTo(() => fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            var searchBatchResponse = GetReadMeFileDetails();
            var jsonString = JsonConvert.SerializeObject(searchBatchResponse);            
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };

            A.CallTo(() => fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
               })
               .Returns(httpResponse);
            var response = await fileShareService.DownloadReadMeFile(readMeFilePath, batchId, exchangeSetRootPath);
            Assert.AreEqual(true,response);
        }
        #endregion 
    }
}
