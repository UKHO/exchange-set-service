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
        private IAuthFssTokenProvider fakeAuthFssTokenProvider;
        private IFileShareServiceClient fakeFileShareServiceClient;
        private IFileShareService fileShareService;
        private IFileSystemHelper fakeFileSystemHelper;
        private IMonitorHelper fakeMonitorHelper;

        public string fakeFilePath = "C:\\HOME\\test.txt";
        public string fakeFolderPath = "C:\\HOME";
        public string fakeZipFilepath = "D:\\UKHO\\V01X01";
        public string fakeExchangeSetPath = @"D:\UKHO";
        public string fakeBatchId = "c4af46f5-1b41-4294-93f9-dda87bf8ab96";
        public string fulfilmentExceptionMessage = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. Please contact UKHO Customer Services quoting error code : {0} and correlation ID : {1}";

        [SetUp]
        public void Setup()
        {
            this.fakeLogger = A.Fake<ILogger<FileShareService>>();
            this.fakeAuthFssTokenProvider = A.Fake<IAuthFssTokenProvider>();
            this.fakeFileShareConfig = Options.Create(new FileShareServiceConfiguration()
            { BaseUrl = "http://tempuri.org", PublicBaseUrl = "http://tempuri.org", CellName = "DE260001", EditionNumber = "1", Limit = 10, Start = 0, ProductCode = "AVCS", ProductLimit = 4, UpdateNumber = "0", UpdateNumberLimit = 10 });
            this.fakeFileShareServiceClient = A.Fake<IFileShareServiceClient>();
            this.fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            this.fakeMonitorHelper = A.Fake<IMonitorHelper>();

            fileShareService = new FileShareService(fakeFileShareServiceClient, fakeAuthFssTokenProvider, fakeFileShareConfig, fakeLogger, fakeFileSystemHelper, fakeMonitorHelper);
        }

        private SalesCatalogueServiceResponseQueueMessage GetScsResponseQueueMessage()
        {
            return new SalesCatalogueServiceResponseQueueMessage
            {
                BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272",
                FileSize = 4000,
                ScsResponseUri = "https://test/ess-test/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272.json",
                CallbackUri = "https://test-callbackuri.com",
                CorrelationId = "727c5230-2c25-4244-9580-13d90004584a"
            };
        }

        #region GetCreateBatchResponse
        private static CreateBatchResponseModel GetCreateBatchResponse()
        {
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            return new CreateBatchResponseModel()
            {
                BatchId = batchId,
                BatchStatusUri = $"http://tempuri.org/batch/{batchId}/status",
                ExchangeSetBatchDetailsUri = $"http://tempuri.org/batch/{batchId}",
                ExchangeSetFileUri = $"http://tempuri.org/batch/{batchId}/files/",
                BatchExpiryDateTime = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)
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

        #region GetSearchBatchEmptyResponse
        private SearchBatchResponse GetSearchBatchEmptyResponse()
        {
            return new SearchBatchResponse()
            {
                Entries = new List<BatchDetail>(),
                Count = 0
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

        #region UploadZipFileData
        private CustomFileInfo GetFileInfo()
        {
            var customFileInfo = new CustomFileInfo()
            {
                Name = "V01X01.zip",
                FullName = @"D:\Downloads",
                Length = 21833
            };
            return customFileInfo;
        }        

        private List<FileDetail> GetFileDetails()
        {
            List<FileDetail> lstFileDetails = new List<FileDetail>()
            { new FileDetail() { FileName = "V01X01.zip", Hash = "Testdata" } };
            return lstFileDetails;
        }
        private ResponseBatchStatusModel GetBatchStatusResponse()
        {
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            return new ResponseBatchStatusModel()
            {
                BatchId = batchId,
                Status = "Committed"
            };
        }
        #endregion UploadZipFileMethods

        #region CreateBatch
        [Test]
        public async Task WhenFSSClientReturnsOtherThan201_ThenCreateBatchReturnsSameStatusAndNullInResponse()
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request"))) });

            var response = await fileShareService.CreateBatch(string.Empty);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.ResponseCode, $"Expected {HttpStatusCode.BadRequest} got {response.ResponseCode}");
            Assert.IsNull(response.ResponseBody);
        }

        [Test]
        public async Task WhenFSSClientReturns201_ThenCreateBatchReturns201AndDataInResponse()
        {
            var createBatchResponse = GetCreateBatchResponse();
            var jsonString = JsonConvert.SerializeObject(createBatchResponse);
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());

            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.Created, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);

            var response = await fileShareService.CreateBatch(string.Empty);

            Assert.AreEqual(HttpStatusCode.Created, response.ResponseCode, $"Expected {HttpStatusCode.Created} got {response.ResponseCode}");
            Assert.AreEqual(createBatchResponse.BatchId, response.ResponseBody.BatchId);
            Assert.AreEqual(createBatchResponse.BatchStatusUri, response.ResponseBody.BatchStatusUri);
            Assert.AreEqual(createBatchResponse.ExchangeSetBatchDetailsUri, response.ResponseBody.ExchangeSetBatchDetailsUri);
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
            string correlationIdParam = null;
            var createBatchResponse = GetCreateBatchResponse();
            var jsonString = JsonConvert.SerializeObject(createBatchResponse);

            //Mock
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(actualAccessToken);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))), RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") }, };
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Invokes((HttpMethod method, string postBody, string accessToken, string uri, string correlationId) =>
                {
                    accessTokenParam = accessToken;
                    uriParam = uri;
                    httpMethodParam = method;
                    postBodyParam = postBody;
                    correlationIdParam = correlationId;
                })
                .Returns(httpResponse);

            //Method call
            var response = await fileShareService.CreateBatch(correlationIdParam);

            //Test
            Assert.AreEqual(response.ResponseCode, HttpStatusCode.OK);
            Assert.AreEqual(HttpMethod.Post, httpMethodParam);
            Assert.AreEqual(actualAccessToken, accessTokenParam);
        }

        #endregion CreateBatch

        #region GetBatchInfoBasedOnProducts
        [Test]
        public void WhenFSSClientReturnsOtherThan201_ThenGetBatchInfoBasedOnProductsReturnsFulfilmentException()
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request"))) });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                  async delegate { await fileShareService.GetBatchInfoBasedOnProducts(GetProductdetails(), null, null); });
        }

        [Test]
        public async Task WhenGetBatchInfoBasedOnProducts_ThenReturnsSearchBatchResponse()
        {
            string postBodyParam = "This should be replace by actual value when param passed to api call";

            //Test variable
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            string correlationIdParam = null;
            var searchBatchResponse = GetSearchBatchResponse();
            var jsonString = JsonConvert.SerializeObject(searchBatchResponse);

            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, string correlationId) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationIdParam = correlationId;
               })
               .Returns(httpResponse);

            var response = await fileShareService.GetBatchInfoBasedOnProducts(GetProductdetails(), null, null);

            Assert.IsNotNull(response);
            Assert.IsInstanceOf(typeof(SearchBatchResponse), response);
            Assert.AreEqual("63d38bde-5191-4a59-82d5-aa22ca1cc6dc", response.Entries[0].BatchId);
        }

        [Test]
        public void WhenFssResponseNotFoundForScsProducts_ThenReturnFulfilmentException()
        {
            string postBodyParam = "This should be replace by actual value when param passed to api call";

            //Test variable
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            string correlationIdParam = null;
            var searchBatchResponse = GetSearchBatchResponse();
            searchBatchResponse.Entries.Add(new BatchDetail
            {
                BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6de",
                Files = new List<BatchFile>() { new BatchFile { Filename = "test.txt", FileSize = 400, Links = new Links { Get = new Link { Href = "" } } } },
                Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "DE" } ,
                                                           new Attribute { Key= "CellName", Value= "DE416050" },
                                                           new Attribute { Key= "EditionNumber", Value= "0" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "AVCS" }}
            });
            var jsonString = JsonConvert.SerializeObject(searchBatchResponse);

            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, string correlationId) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationIdParam = correlationId;
               })
               .Returns(httpResponse);
            var productList = GetProductdetails();
            productList.Add(new Products
            {
                ProductName = "DE416051",
                EditionNumber = 0,
                UpdateNumbers = new List<int?> { 1 },
                FileSize = 400
            });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                async delegate { await fileShareService.GetBatchInfoBasedOnProducts(productList, null, null); });

        }

        [Test]
        public async Task WhenGetBatchInfoBasedOnProductsWithCancellation_ThenReturnsSearchBatchResponse()
        {
            string postBodyParam = "This should be replace by actual value when param passed to api call";

            //Test variable
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            string correlationIdParam = null;
            var searchBatchResponse = GetSearchBatchResponse();
            searchBatchResponse.Entries.Add(new BatchDetail
            {
                BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6de",
                Files = new List<BatchFile>() { new BatchFile { Filename = "test.txt", FileSize = 400, Links = new Links { Get = new Link { Href = "" } } } },
                Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "DE" } ,
                                                           new Attribute { Key= "CellName", Value= "DE416050" },
                                                           new Attribute { Key= "EditionNumber", Value= "0" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "AVCS" }},
            });
            searchBatchResponse.Entries.Add(new BatchDetail
            {
                BatchId = "13d38bde-5191-4a59-82d5-aa22ca1cc6de",
                Files = new List<BatchFile>() { new BatchFile { Filename = "test1.txt", FileSize = 400, Links = new Links { Get = new Link { Href = "" } } } },
                Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "DE" } ,
                                                           new Attribute { Key= "CellName", Value= "DE416051" },
                                                           new Attribute { Key= "EditionNumber", Value= "3" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "AVCS" }},
            });
            var jsonString = JsonConvert.SerializeObject(searchBatchResponse);

            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, string correlationId) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationIdParam = correlationId;
               })
               .Returns(httpResponse);
            var productList = GetProductdetails();
            productList.Add(new Products {
                                ProductName = "DE416051",
                                EditionNumber = 0,
                                UpdateNumbers = new List<int?> {0},
                                FileSize = 400,
                                Cancellation = new Cancellation { EditionNumber = 3, UpdateNumber = 0 }
                            });
            var response = await fileShareService.GetBatchInfoBasedOnProducts(productList, null, null);

            Assert.IsNotNull(response);
            Assert.IsInstanceOf(typeof(SearchBatchResponse), response);
            Assert.AreEqual("63d38bde-5191-4a59-82d5-aa22ca1cc6dc", response.Entries[0].BatchId);
            Assert.AreEqual("13d38bde-5191-4a59-82d5-aa22ca1cc6de", response.Entries[1].BatchId);
        }

        [Test]
        public void WhenFssResponseNotFoundForScsProductsWithCancellation_ThenReturnFulfilmentException()
        {
            string postBodyParam = "This should be replace by actual value when param passed to api call";

            //Test variable
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            string correlationIdParam = null;
            var searchBatchResponse = GetSearchBatchResponse();
            searchBatchResponse.Entries.Add(new BatchDetail
            {
                BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6de",
                Files = new List<BatchFile>() { new BatchFile { Filename = "test.txt", FileSize = 400, Links = new Links { Get = new Link { Href = "" } } } },
                Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "DE" } ,
                                                           new Attribute { Key= "CellName", Value= "DE416050" },
                                                           new Attribute { Key= "EditionNumber", Value= "0" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "AVCS" }}
            });
            searchBatchResponse.Entries.Add(new BatchDetail
            {
                BatchId = "13d38bde-5191-4a59-82d5-aa22ca1cc6de",
                Files = new List<BatchFile>() { new BatchFile { Filename = "test1.txt", FileSize = 400, Links = new Links { Get = new Link { Href = "" } } } },
                Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "DE" } ,
                                                           new Attribute { Key= "CellName", Value= "DE416051" },
                                                           new Attribute { Key= "EditionNumber", Value= "0" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "AVCS" }},
            });
            var jsonString = JsonConvert.SerializeObject(searchBatchResponse);

            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, string correlationId) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationIdParam = correlationId;
               })
               .Returns(httpResponse);
            var productList = GetProductdetails();
            productList.Add(new Products
            {
                ProductName = "DE416051",
                EditionNumber = 0,
                UpdateNumbers = new List<int?> { 0 },
                FileSize = 400,
                Cancellation = new Cancellation { EditionNumber = 3, UpdateNumber = 0 }
            });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                async delegate { await fileShareService.GetBatchInfoBasedOnProducts(productList, null, null); });

        }
        #endregion GetBatchInfoBasedOnProducts

        #region DownloadBatchFile
        [Test]
        public async Task WhenGetBatchInfoBasedOnProductsReturns200_ThenDownloadBatchFiles()
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage()
                 {
                     StatusCode = HttpStatusCode.OK,
                     RequestMessage = new HttpRequestMessage()
                     {
                         RequestUri = new Uri("http://test.com")
                     },
                     Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Received Fulfilment Data Successfully!!!!")))
                 });

            var response = await fileShareService.DownloadBatchFiles(new List<string> { fakeFilePath }, fakeFolderPath, GetScsResponseQueueMessage());

            Assert.IsNotNull(response);
            Assert.IsInstanceOf(typeof(bool), response);
        }

        [Test]
        public void WhenGetBatchInfoBasedOnProductsReturnsOtherThan200_ThenReturnFulfilmentException()
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage()
                 {
                     StatusCode = HttpStatusCode.BadRequest,
                     RequestMessage = new HttpRequestMessage()
                     {
                         RequestUri = new Uri("http://test.com")
                     },
                     Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request")))
                 });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                 async delegate { await fileShareService.DownloadBatchFiles(new List<string> { fakeFilePath }, fakeFolderPath, GetScsResponseQueueMessage()); });
        }
        #endregion

        #region SearchReadMeFilePath
        [Test]
        public void WhenInvalidSearchReadMeFileRequest_ThenReturnFulfilmentException()
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request"))) });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                  async delegate { await fileShareService.SearchReadMeFilePath(string.Empty, string.Empty); });
        }

        [Test]
        public void WhenReadMeFileNotFound_ThenReturnFulfilmentException()
        {
            string postBodyParam = "This should be replace by actual value when param passed to api call";
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            string batchId = "a07537ff-ffa2-4565-8f0e-96e61e70a9fc";
            string correlationidParam = null;
            var searchBatchResponse = GetSearchBatchEmptyResponse();
            var jsonString = JsonConvert.SerializeObject(searchBatchResponse);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, string correlationid) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationidParam = correlationid;
               })
               .Returns(httpResponse);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                  async delegate { await fileShareService.SearchReadMeFilePath(batchId, string.Empty); });
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
            string correlationidParam = null;

            var searchBatchResponse = GetSearchBatchResponse();
            var jsonString = JsonConvert.SerializeObject(searchBatchResponse);

            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, string correlationid) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationidParam = correlationid;
               })
               .Returns(httpResponse);

            var response = await fileShareService.SearchReadMeFilePath(batchId, null);
            string expectedReadMeFilePath = @"batch/a07537ff-ffa2-4565-8f0e-96e61e70a9fc/files/README.TXT";
            Assert.IsNotNull(response);
            Assert.AreEqual(expectedReadMeFilePath, searchReadMeFileName);
        }
        #endregion SearchReadMeFilePath

        #region DownloadReadMeFile

        [Test]
        public async Task WhenValidDownloadReadMeFileRequest_ThenReturnTrue()
        {
            string postBodyParam = "This should be replace by actual value when param passed to api call";
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            string batchId = "c4af46f5-1b41-4294-93f9-dda87bf8ab96";
            string correlationidParam = null;
            fakeFileShareConfig.Value.ReadMeFileName = "ReadMe.txt";
            string readMeFilePath = @"batch/c4af46f5-1b41-4294-93f9-dda87bf8ab96/files/README.TXT";
            string exchangeSetRootPath = @"C:\\HOME";
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            var searchBatchResponse = GetReadMeFileDetails();
            var jsonString = JsonConvert.SerializeObject(searchBatchResponse);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, string correlationid) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationidParam = correlationid;
               })
               .Returns(httpResponse);
            A.CallTo(() => fakeFileSystemHelper.DownloadReadmeFile(A<string>.Ignored, A<Stream>.Ignored, A<string>.Ignored)).Returns(true);

            var response = await fileShareService.DownloadReadMeFile(readMeFilePath, batchId, exchangeSetRootPath, null);

            Assert.AreEqual(true, response);
        }

        [Test]
        public void WhenInvalidDownloadReadMeFileRequest_ThenReturnFulfilmentException()
        {
            string postBodyParam = "This should be replace by actual value when param passed to api call";
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            string batchId = "c4af46f5-1b41-4294-93f9-dda87bf8ab96";
            string correlationidParam = null;

            fakeFileShareConfig.Value.ReadMeFileName = "ReadMe.txt";

            string readMeFilePath = @"batch/c4af46f5-1b41-4294-93f9-dda87bf8ab96/files/README.TXT";
            string exchangeSetRootPath = @"C:\\HOME";

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            var searchBatchResponse = GetReadMeFileDetails();
            var jsonString = JsonConvert.SerializeObject(searchBatchResponse);

            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))), RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") } };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, string correlationid) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationidParam = correlationid;
               })
               .Returns(httpResponse);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                 async delegate { await fileShareService.DownloadReadMeFile(readMeFilePath, batchId, exchangeSetRootPath, null); });
        }
        #endregion

        #region CreateZipFile
        [Test]
        public void WhenInvalidCreateZipFileRequest_ThenReturnFulfilmentException()
        {
            A.CallTo(() => fakeFileSystemHelper.CreateZipFile(A<string>.Ignored, A<string>.Ignored));
            A.CallTo(() => fakeFileSystemHelper.CheckDirectoryAndFileExists(A<string>.Ignored, A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                  async delegate { await fileShareService.CreateZipFileForExchangeSet(fakeBatchId, string.Empty, string.Empty); });
        }

        [Test]
        public async Task WhenValidCreateZipFileRequest_ThenReturnTrue()
        {
            A.CallTo(() => fakeFileSystemHelper.CreateZipFile(A<string>.Ignored, A<string>.Ignored));
            A.CallTo(() => fakeFileSystemHelper.CheckDirectoryAndFileExists(A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(true);

            bool response = await fileShareService.CreateZipFileForExchangeSet(fakeBatchId, fakeZipFilepath, null);
            Assert.IsNotNull(response);
            Assert.AreEqual(true, response);
        }
        #endregion CreateZipFile

        #region UploadZipFile

        [Test]
        public void WhenInvalidAddFileInBatchAsyncRequest_ThenReturnFulfilmentException()
        {
            fakeFileShareConfig.Value.ExchangeSetFileName = "V01X01.zip";
            fakeFileShareConfig.Value.BlockSizeInMultipleOfKBs = 256;
            fakeFileShareConfig.Value.ParallelUploadThreadCount = 0;
            fakeFileShareConfig.Value.BaseUrl = null;

            var GetFileInfoDetails = GetFileInfo();

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored)).Returns(GetFileInfoDetails);
            A.CallTo(() => fakeFileShareServiceClient.AddFileInBatchAsync(A<HttpMethod>.Ignored, A<FileCreateModel>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(new HttpResponseMessage()
             {
                 StatusCode = HttpStatusCode.BadRequest,
                 RequestMessage = new HttpRequestMessage()
                 {
                     RequestUri = new Uri("http://test.com")
                 },
                 Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request")))
             });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                   async delegate { await fileShareService.UploadFileToFileShareService(fakeBatchId, fakeExchangeSetPath, null, fakeFileShareConfig.Value.ExchangeSetFileName); });
        }

        [Test]
        public void WhenInvalidWriteBlockInFileAsyncRequest_ThenReturnFulfilmentException()
        {
            fakeFileShareConfig.Value.ExchangeSetFileName = "V01X01.zip";
            fakeFileShareConfig.Value.BlockSizeInMultipleOfKBs = 256;
            fakeFileShareConfig.Value.ParallelUploadThreadCount = 0;
            fakeFileShareConfig.Value.BaseUrl = null;
            byte[] byteData = new byte[1024];
            var responseBatchStatusModel = GetBatchStatusResponse();
            responseBatchStatusModel.Status = "";
            var jsonString = JsonConvert.SerializeObject(responseBatchStatusModel);
            var GetFileInfoDetails = GetFileInfo();
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.Created, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored)).Returns(GetFileInfoDetails);
            A.CallTo(() => fakeFileSystemHelper.UploadFileBlockMetaData(A<UploadBlockMetaData>.Ignored)).Returns(byteData);
            A.CallTo(() => fakeFileShareServiceClient.AddFileInBatchAsync(A<HttpMethod>.Ignored, A<FileCreateModel>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
            .Returns(httpResponse);
            A.CallTo(() => fakeFileShareServiceClient.WriteBlockInFileAsync(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<WriteBlockFileModel>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(new HttpResponseMessage()
             {
                 StatusCode = HttpStatusCode.BadRequest,
                 RequestMessage = new HttpRequestMessage()
                 {
                     RequestUri = new Uri("http://test.com")
                 },
                 Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request")))
             });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                  async delegate { await fileShareService.UploadFileToFileShareService(fakeBatchId, fakeExchangeSetPath, null, fakeFileShareConfig.Value.ExchangeSetFileName); });
        }

        [Test]
        public void WhenInvalidCommitBatchAsyncRequest_ThenReturnFulfilmentException()
        {
            fakeFileShareConfig.Value.ExchangeSetFileName = "V01X01.zip";
            fakeFileShareConfig.Value.BlockSizeInMultipleOfKBs = 256;
            fakeFileShareConfig.Value.ParallelUploadThreadCount = 0;
            fakeFileShareConfig.Value.BaseUrl = null;
            byte[] byteData = new byte[1024];
            var fileDetail = GetFileDetails();
            var responseBatchStatusModel = GetBatchStatusResponse();
            responseBatchStatusModel.Status = "";
            var jsonString = JsonConvert.SerializeObject(responseBatchStatusModel);
            var GetFileInfoDetails = GetFileInfo();
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.Created, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored)).Returns(GetFileInfoDetails);
            A.CallTo(() => fakeFileSystemHelper.UploadFileBlockMetaData(A<UploadBlockMetaData>.Ignored)).Returns(byteData);
            A.CallTo(() => fakeFileSystemHelper.UploadCommitBatch(A<BatchCommitMetaData>.Ignored)).Returns(fileDetail); 
            A.CallTo(() => fakeFileShareServiceClient.AddFileInBatchAsync(A<HttpMethod>.Ignored, A<FileCreateModel>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
            .Returns(httpResponse);
            A.CallTo(() => fakeFileShareServiceClient.WriteBlockInFileAsync(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<WriteBlockFileModel>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
            .Returns(httpResponse);
            A.CallTo(() => fakeFileShareServiceClient.CommitBatchAsync(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<BatchCommitModel>.Ignored, A<string>.Ignored, A<string>.Ignored))
            .Returns(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.BadRequest,
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri("http://test.com")
                },
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request")))
            });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                async delegate { await fileShareService.UploadFileToFileShareService(fakeBatchId, fakeExchangeSetPath, null, fakeFileShareConfig.Value.ExchangeSetFileName); });
        }

        [Test]
        public void WhenInvalidGetBatchStatusAsyncRequest_ThenReturnFulfilmentException()
        {
            fakeFileShareConfig.Value.ExchangeSetFileName = "V01X01.zip";
            fakeFileShareConfig.Value.BlockSizeInMultipleOfKBs = 256;
            fakeFileShareConfig.Value.ParallelUploadThreadCount = 0;
            fakeFileShareConfig.Value.BaseUrl = null;
            byte[] byteData = new byte[1024];
            var responseBatchStatusModel = GetBatchStatusResponse();
            responseBatchStatusModel.Status = "";
            var jsonString = JsonConvert.SerializeObject(responseBatchStatusModel);
            var GetFileInfoDetails = GetFileInfo();
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.Created, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored)).Returns(GetFileInfoDetails);
            A.CallTo(() => fakeFileSystemHelper.UploadFileBlockMetaData(A<UploadBlockMetaData>.Ignored)).Returns(byteData);
            A.CallTo(() => fakeFileShareServiceClient.AddFileInBatchAsync(A<HttpMethod>.Ignored, A<FileCreateModel>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
            .Returns(httpResponse);
            A.CallTo(() => fakeFileShareServiceClient.WriteBlockInFileAsync(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<WriteBlockFileModel>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
            .Returns(httpResponse);
            A.CallTo(() => fakeFileShareServiceClient.CommitBatchAsync(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<BatchCommitModel>.Ignored, A<string>.Ignored, A<string>.Ignored))
            .Returns(httpResponse);
            A.CallTo(() => fakeFileShareServiceClient.GetBatchStatusAsync(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
            .Returns(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.BadRequest,
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri("http://test.com")
                },
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request")))
            });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                   async delegate { await fileShareService.UploadFileToFileShareService(fakeBatchId, fakeExchangeSetPath, null, fakeFileShareConfig.Value.ExchangeSetFileName); });
        }

        [Test]
        public async Task WhenValidUploadZipFileRequest_ThenReturnTrue()
        {
            fakeFileShareConfig.Value.ExchangeSetFileName = "V01X01.zip";
            fakeFileShareConfig.Value.BlockSizeInMultipleOfKBs = 256;
            fakeFileShareConfig.Value.ParallelUploadThreadCount = 0;
            fakeFileShareConfig.Value.BaseUrl = null;
            fakeFileShareConfig.Value.BatchCommitCutOffTimeInMinutes = 30;
            fakeFileShareConfig.Value.BatchCommitDelayTimeInMilliseconds = 100;
            byte[] byteData = new byte[1024];

            var responseBatchStatusModel = GetBatchStatusResponse();
            var jsonString = JsonConvert.SerializeObject(responseBatchStatusModel);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.Created, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };
            var GetFileInfoDetails = GetFileInfo();
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored)).Returns(GetFileInfoDetails);
            A.CallTo(() => fakeFileShareServiceClient.AddFileInBatchAsync(A<HttpMethod>.Ignored, A<FileCreateModel>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
            .Returns(httpResponse);
            A.CallTo(() => fakeFileSystemHelper.UploadFileBlockMetaData(A<UploadBlockMetaData>.Ignored)).Returns(byteData);
            A.CallTo(() => fakeFileShareServiceClient.GetBatchStatusAsync(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(httpResponse);

            var response = await fileShareService.UploadFileToFileShareService(fakeBatchId, fakeExchangeSetPath, null, fakeFileShareConfig.Value.ExchangeSetFileName);
            Assert.AreEqual(true, response);
        }

        [Test]
        public void WhenInvalidUploadZipFileRequest_ThenReturnFulfilmentException()
        {
            fakeFileShareConfig.Value.ExchangeSetFileName = "V01X01.zip";
            fakeFileShareConfig.Value.BlockSizeInMultipleOfKBs = 256;
            fakeFileShareConfig.Value.ParallelUploadThreadCount = 0;
            fakeFileShareConfig.Value.BaseUrl = null;
            byte[] byteData = new byte[1024];
            var responseBatchStatusModel = GetBatchStatusResponse();
            responseBatchStatusModel.Status = "";
            var jsonString = JsonConvert.SerializeObject(responseBatchStatusModel);

            var GetFileInfoDetails = GetFileInfo();
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored)).Returns(GetFileInfoDetails);
            A.CallTo(() => fakeFileSystemHelper.UploadFileBlockMetaData(A<UploadBlockMetaData>.Ignored)).Returns(byteData);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.Created, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };
            A.CallTo(() => fakeFileShareServiceClient.AddFileInBatchAsync(A<HttpMethod>.Ignored, A<FileCreateModel>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
            .Returns(httpResponse);
            A.CallTo(() => fakeFileShareServiceClient.GetBatchStatusAsync(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
            .Returns(httpResponse);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                   async delegate { await fileShareService.UploadFileToFileShareService(fakeBatchId, fakeExchangeSetPath, null, fakeFileShareConfig.Value.ExchangeSetFileName); });
        }
        #endregion UploadZipFile
    }
}