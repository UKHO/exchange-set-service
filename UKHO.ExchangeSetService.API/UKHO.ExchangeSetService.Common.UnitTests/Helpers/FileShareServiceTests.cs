using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
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
        private IFileShareServiceCache fakeFileShareServiceCache;
        private IOptions<CacheConfiguration> fakeCacheConfiguration;
        private IOptions<AioConfiguration> fakeAioConfiguration;

        public string fakeFilePath = "C:\\HOME\\test.txt";
        public string fakeLargeMediaZipFilePath = "D:\\HOME\\M01X01.zip";
        public string fakeFolderPath = "C:\\HOME";
        public string fakeZipFilepath = "D:\\UKHO\\V01X01";
        public string fakeExchangeSetPath = @"D:\UKHO";
        public string fakeBatchId = "c4af46f5-1b41-4294-93f9-dda87bf8ab96";
        public string fakeCorrelationId = Guid.NewGuid().ToString();
        public string fulfilmentExceptionMessage = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. Please contact UKHO Customer Services quoting error code : {0} and correlation ID : {1}";
        public CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        [SetUp]
        public void Setup()
        {
            fakeLogger = A.Fake<ILogger<FileShareService>>();
            fakeAuthFssTokenProvider = A.Fake<IAuthFssTokenProvider>();
            fakeFileShareConfig = Options.Create(new FileShareServiceConfiguration()
            { BaseUrl = "http://tempuri.org", PublicBaseUrl = "http://filetempuri.org", CellName = "DE260001", EditionNumber = "1", Limit = 10, Start = 0, ProductCode = "AVCS", ProductLimit = 4, UpdateNumber = "0", UpdateNumberLimit = 10 });
            fakeFileShareServiceClient = A.Fake<IFileShareServiceClient>();
            fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            fakeFileShareServiceCache = A.Fake<IFileShareServiceCache>();
            fakeMonitorHelper = A.Fake<IMonitorHelper>();
            fakeCacheConfiguration = Options.Create(new CacheConfiguration { CacheStorageAccountKey = "key", CacheStorageAccountName = "cache", FssSearchCacheTableName = "AnyName", IsFssCacheEnabled = true });
            fakeAioConfiguration = A.Fake<IOptions<AioConfiguration>>();

            fileShareService = new FileShareService(fakeFileShareServiceClient, fakeAuthFssTokenProvider, fakeFileShareConfig, fakeLogger, fakeFileShareServiceCache, fakeCacheConfiguration, fakeFileSystemHelper, fakeMonitorHelper, fakeAioConfiguration);
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
                                FileSize = 400,
                                Bundle = new List<Bundle>
                            {
                                new Bundle
                                {
                                    BundleType = "DVD",
                                    Location = "M1;B1"
                                }
                            }
                            }
                        };
        }

        private List<Products> GetAioProductdetails()
        {
            return new List<Products> {
                            new Products {
                                ProductName = "DE416050",
                                EditionNumber = 0,
                                UpdateNumbers = new List<int?> {0},
                                FileSize = 400,
                                Bundle = new List<Bundle>
                            {
                                new Bundle
                                {
                                    BundleType = "DVD",
                                    Location = "M1;B1"
                                }
                            }
                            },
                            new Products {
                                ProductName = "GB800001",
                                EditionNumber = 0,
                                UpdateNumbers = new List<int?> {0},
                                FileSize = 400,
                                Bundle = new List<Bundle>
                            {
                                new Bundle
                                {
                                    BundleType = "DVD",
                                    Location = "M1;B1"
                                }
                            }
                            }
                        };
        }
        #endregion

        #region GetSearchBatchResponse
        private SearchBatchResponse GetSearchBatchResponse(string businessUnit = "ADDS")
        {
            return new SearchBatchResponse()
            {
                Entries = new List<BatchDetail>() {
                    new() {
                        BatchId ="63d38bde-5191-4a59-82d5-aa22ca1cc6dc",
                        Files= new List<BatchFile>(){ new() { Filename = "test.txt", FileSize = 400, Links = new Links { Get = new Link { Href = "" }}}},
                        Attributes = new List<Attribute> { new() { Key= "Agency", Value= "DE" } ,
                                                           new() { Key= "CellName", Value= "DE416050" },
                                                           new() { Key= "EditionNumber", Value= "0" } ,
                                                           new() { Key= "UpdateNumber", Value= "0" },
                                                           new() { Key= "ProductCode", Value= "AVCS" }},
                        BusinessUnit = businessUnit
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

        private CustomFileInfo GetErrorFileInfo()
        {
            var customFileInfo = new CustomFileInfo()
            {
                Name = "error.txt",
                FullName = @"D:\Batch\error.txt",
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

        private List<FileDetail> GetLargeMediaFileDetails()
        {
            return new List<FileDetail>()
            {
                new FileDetail() { FileName = "M01X02.zip", Hash = "Testdata" },
                new FileDetail() { FileName = "M02X02.zip", Hash = "Testdata"}
            };
        }

        private ResponseBatchStatusModel GetBatchStatusResponse()
        {
            return new ResponseBatchStatusModel()
            {
                BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272",
                Status = "Committed"
            };
        }

        private ResponseBatchStatusModel GetBatchStatusFailedResponse()
        {
            return new ResponseBatchStatusModel()
            {
                BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272",
                Status = "Failed"
            };
        }

        private string[] GetZipFileListForBatchCommit()
        {
            return new string[] { "Test1.zip, Test2.zip" };
        }

        #endregion UploadZipFileMethods

        #region CreateBatch
        [Test]
        public async Task WhenFSSClientReturnsOtherThan201_ThenCreateBatchReturnsSameStatusAndNullInResponse()
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request"))) });

            var response = await fileShareService.CreateBatch(string.Empty, string.Empty);
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
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);

            var response = await fileShareService.CreateBatch(string.Empty, string.Empty);

            Assert.AreEqual(HttpStatusCode.Created, response.ResponseCode, $"Expected {HttpStatusCode.Created} got {response.ResponseCode}");
            Assert.AreEqual(createBatchResponse.BatchId, response.ResponseBody.BatchId);

            //assert the mocked API response returned to CreateBatch contains the internal BaseUrl
            Assert.IsTrue(createBatchResponse.BatchStatusUri.Contains(fakeFileShareConfig.Value.BaseUrl));
            Assert.IsTrue(createBatchResponse.ExchangeSetBatchDetailsUri.Contains(fakeFileShareConfig.Value.BaseUrl));
            Assert.IsTrue(createBatchResponse.ExchangeSetFileUri.Contains(fakeFileShareConfig.Value.BaseUrl));

            //assert FileShareService.CreateBatch() is correctly replacing the internal BaseUrl with PublicUrl
            Assert.AreEqual(createBatchResponse.BatchStatusUri.Replace(fakeFileShareConfig.Value.BaseUrl, fakeFileShareConfig.Value.PublicBaseUrl), response.ResponseBody.BatchStatusUri);
            Assert.AreEqual(createBatchResponse.ExchangeSetBatchDetailsUri.Replace(fakeFileShareConfig.Value.BaseUrl, fakeFileShareConfig.Value.PublicBaseUrl), response.ResponseBody.ExchangeSetBatchDetailsUri);
            Assert.AreEqual(createBatchResponse.ExchangeSetFileUri.Replace(fakeFileShareConfig.Value.BaseUrl, fakeFileShareConfig.Value.PublicBaseUrl), response.ResponseBody.ExchangeSetFileUri);
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
            string userOID = null;
            var createBatchResponse = GetCreateBatchResponse();
            var jsonString = JsonConvert.SerializeObject(createBatchResponse);

            //Mock
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(actualAccessToken);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))), RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") }, };
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationId) =>
                {
                    accessTokenParam = accessToken;
                    uriParam = uri;
                    httpMethodParam = method;
                    postBodyParam = postBody;
                    correlationIdParam = correlationId;
                })
                .Returns(httpResponse);

            //Method call
            var response = await fileShareService.CreateBatch(userOID, correlationIdParam);

            //Test
            Assert.AreEqual(HttpStatusCode.OK, response.ResponseCode);
            Assert.AreEqual(HttpMethod.Post, httpMethodParam);
            Assert.AreEqual(accessTokenParam, actualAccessToken);
        }

        #endregion CreateBatch

        #region GetBatchInfoBasedOnProducts

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public void WhenFSSClientReturnsOtherThan201_ThenGetBatchInfoBasedOnProductsReturnsFulfilmentException(string businessUnit)
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceCache.GetNonCachedProductDataForFss(A<List<Products>>.Ignored, A<SearchBatchResponse>.Ignored, A<string>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(GetProductdetails());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request"))) });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                  async delegate { await fileShareService.GetBatchInfoBasedOnProducts(GetProductdetails(), GetScsResponseQueueMessage(), cancellationTokenSource, CancellationToken.None, string.Empty, businessUnit); });
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public async Task WhenGetBatchInfoBasedOnProducts_ThenReturnsSearchBatchResponse(string businessUnit)
        {
            string postBodyParam = "This should be replace by actual value when param passed to api call";

            //Test variable
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            string correlationIdParam = null;
            var searchBatchResponse = GetSearchBatchResponse(businessUnit);
            var jsonString = JsonConvert.SerializeObject(searchBatchResponse);

            var httpResponse = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))),
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri("http://test.com")
                }
            };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceCache.GetNonCachedProductDataForFss(A<List<Products>>.Ignored, A<SearchBatchResponse>.Ignored, A<string>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(GetProductdetails());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationId) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationIdParam = correlationId;
               })
               .Returns(httpResponse);

            var response = await fileShareService.GetBatchInfoBasedOnProducts(GetProductdetails(), GetScsResponseQueueMessage(), null, CancellationToken.None, string.Empty, businessUnit);

            Assert.IsNotNull(response);
            Assert.IsInstanceOf(typeof(SearchBatchResponse), response);
            Assert.AreEqual("63d38bde-5191-4a59-82d5-aa22ca1cc6dc", response.Entries[0].BatchId);
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public void WhenFssResponseNotFoundForScsProducts_ThenReturnFulfilmentException(string businessUnit)
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

            var httpResponse = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))),
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri("http://test.com")
                }
            };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceCache.GetNonCachedProductDataForFss(A<List<Products>>.Ignored, A<SearchBatchResponse>.Ignored, A<string>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(GetProductdetails());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationId) =>
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
                async delegate { await fileShareService.GetBatchInfoBasedOnProducts(productList, GetScsResponseQueueMessage(), cancellationTokenSource, CancellationToken.None, string.Empty, businessUnit); });
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public async Task WhenGetBatchInfoBasedOnProductsWithCancellation_ThenReturnsSearchBatchResponse(string businessUnit)
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

            var httpResponse = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))),
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri("http://test.com")
                }
            };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationId) =>
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
            A.CallTo(() => fakeFileShareServiceCache.GetNonCachedProductDataForFss(A<List<Products>>.Ignored, A<SearchBatchResponse>.Ignored, A<string>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(productList);
            CommonHelper.IsPeriodicOutputService = false;

            var response = await fileShareService.GetBatchInfoBasedOnProducts(productList, GetScsResponseQueueMessage(), null, CancellationToken.None, string.Empty, businessUnit);

            Assert.IsNotNull(response);
            Assert.IsInstanceOf(typeof(SearchBatchResponse), response);
            Assert.AreEqual("63d38bde-5191-4a59-82d5-aa22ca1cc6dc", response.Entries[0].BatchId);
            Assert.AreEqual("13d38bde-5191-4a59-82d5-aa22ca1cc6de", response.Entries[1].BatchId);
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public void WhenFssResponseNotFoundForScsProductsWithCancellation_ThenReturnFulfilmentException(string businessUnit)
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

            var httpResponse = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))),
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri("http://test.com")
                }
            };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceCache.GetNonCachedProductDataForFss(A<List<Products>>.Ignored, A<SearchBatchResponse>.Ignored, A<string>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(GetProductdetails());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationId) =>
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
                async delegate { await fileShareService.GetBatchInfoBasedOnProducts(productList, GetScsResponseQueueMessage(), cancellationTokenSource, CancellationToken.None, string.Empty, businessUnit); });

        }
        #endregion GetBatchInfoBasedOnProducts

        #region DownloadBatchFile
        [Test]
        public async Task WhenGetBatchInfoBasedOnProductsReturns200_ThenDownloadBatchFiles()
        {
            var batchDetail = GetSearchBatchResponse();
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage()
                 {
                     StatusCode = HttpStatusCode.OK,
                     RequestMessage = new HttpRequestMessage()
                     {
                         RequestUri = new Uri("http://test.com")
                     },
                     Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Received Fulfilment Data Successfully!!!!")))
                 });

            var response = await fileShareService.DownloadBatchFiles(batchDetail.Entries[0], new List<string> { fakeFilePath }, fakeFolderPath, GetScsResponseQueueMessage(), null, CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.IsInstanceOf(typeof(bool), response);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FileShareServiceSearchResponseStoreToCacheStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search response insert/merge request in azure table for cache for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit} with FSS BatchId:{FssBatchId}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenGetBatchInfoBasedOnProductsReturns200_ThenDownloadBatchFilesToCache()
        {
            var batchDetail = GetSearchBatchResponse();
            batchDetail.Entries.Add(new BatchDetail
            {
                BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dj",
                Files = new List<BatchFile>() { new BatchFile { Filename = "test.txt", FileSize = 400, Links = new Links { Get = new Link { Href = "" } } } },
                Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "DE" } ,
                                                           new Attribute { Key= "CellName", Value= "DE416051" },
                                                           new Attribute { Key= "EditionNumber", Value= "0" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "AVCS" }}
            });
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage()
                 {
                     StatusCode = HttpStatusCode.OK,
                     RequestMessage = new HttpRequestMessage()
                     {
                         RequestUri = new Uri("http://test.com")
                     },
                     Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Received Fulfilment Data Successfully!!!!")))
                 });

            var response = await fileShareService.DownloadBatchFiles(batchDetail.Entries[0], new List<string> { fakeFilePath }, fakeFolderPath, GetScsResponseQueueMessage(), null, CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.IsInstanceOf(typeof(bool), response);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FileShareServiceSearchResponseStoreToCacheStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search response insert/merge request in azure table for cache for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit} with FSS BatchId:{FssBatchId}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenGetBatchInfoBasedOnProductsReturnsOtherThan200_ThenReturnFulfilmentException()
        {
            var batchDetail = GetSearchBatchResponse();
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
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
                 async delegate { await fileShareService.DownloadBatchFiles(batchDetail.Entries[0], new List<string> { fakeFilePath }, fakeFolderPath, GetScsResponseQueueMessage(), cancellationTokenSource, CancellationToken.None); });

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.DownloadENCFilesNonOkResponse.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in file share service while downloading ENC file:{fileName} with uri:{requestUri} responded with {StatusCode} and BatchId:{BatchId} and _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.CancellationTokenEvent.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request cancelled for Error in file share service while downloading ENC file:{fileName} from File Share Service with CancellationToken:{cancellationTokenSource.Token} with uri:{requestUri} responded with {StatusCode} and BatchId:{BatchId} and _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenIsCancellationRequestedinDownloadBatchFiles_ThenThrowCancelledException()
        {
            var batchDetail = GetSearchBatchResponse();
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage()
                 {
                     StatusCode = HttpStatusCode.BadRequest,
                     RequestMessage = new HttpRequestMessage()
                     {
                         RequestUri = new Uri("http://test.com")
                     },
                     Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request")))
                 });

            cancellationTokenSource.Cancel();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            Assert.ThrowsAsync<OperationCanceledException>(async () => await fileShareService.DownloadBatchFiles(batchDetail.Entries[0], new List<string> { fakeFilePath }, fakeFolderPath, GetScsResponseQueueMessage(), cancellationTokenSource, cancellationToken));
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public void WhenIsCancellationRequestedinGetBatchInfoBasedOnProducts_ThenThrowCancelledException(string businessUnit)
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceCache.GetNonCachedProductDataForFss(A<List<Products>>.Ignored, A<SearchBatchResponse>.Ignored, A<string>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(GetProductdetails());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage()
                 {
                     StatusCode = HttpStatusCode.BadRequest,
                     RequestMessage = new HttpRequestMessage()
                     {
                         RequestUri = new Uri("http://test.com")
                     },
                     Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad Request!!!!")))
                 });
            var productList = GetProductdetails();
            productList.Add(new Products
            {
                ProductName = "DE416051",
                EditionNumber = 0,
                UpdateNumbers = new List<int?> { 0 },
                FileSize = 400,
                Cancellation = new Cancellation { EditionNumber = 3, UpdateNumber = 0 }
            });

            cancellationTokenSource.Cancel();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            Assert.ThrowsAsync<OperationCanceledException>(async () => await fileShareService.GetBatchInfoBasedOnProducts(productList, GetScsResponseQueueMessage(), cancellationTokenSource, cancellationToken, string.Empty, businessUnit));
        }

        #endregion

        #region SearchReadMeFilePath
        [Test]
        public void WhenInvalidSearchReadMeFileRequest_ThenReturnFulfilmentException()
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
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
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationid) =>
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
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationid) =>
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
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))), RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") } };
            httpResponse.Headers.Add("Server", "test/10.0");

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationid) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationidParam = correlationid;
               })
               .Returns(httpResponse);
            A.CallTo(() => fakeFileSystemHelper.DownloadReadmeFile(A<string>.Ignored, A<Stream>.Ignored, A<string>.Ignored)).Returns(true);

            var response = await fileShareService.DownloadReadMeFileFromFssAsync(readMeFilePath, batchId, exchangeSetRootPath, null);

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
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationid) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationidParam = correlationid;
               })
               .Returns(httpResponse);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                 async delegate { await fileShareService.DownloadReadMeFileFromFssAsync(readMeFilePath, batchId, exchangeSetRootPath, null); });
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
            var fileDetail = GetFileDetails();
            var responseBatchStatusModel = GetBatchStatusResponse();
            responseBatchStatusModel.Status = "";

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored)).Returns(new string[] { "V01X01.zip" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored)).Returns(GetFileInfo());
            A.CallTo(() => fakeFileSystemHelper.UploadLargeMediaCommitBatch(A<List<BatchCommitMetaData>>.Ignored)).Returns(fileDetail);
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
                async delegate { await fileShareService.CommitBatchToFss(fakeBatchId, fakeCorrelationId, fakeExchangeSetPath); });
        }

        [Test]
        public void WhenInvalidGetBatchStatusAsyncRequest_ThenReturnFulfilmentException()
        {
            fakeFileShareConfig.Value.ExchangeSetFileName = "V01X01.zip";
            fakeFileShareConfig.Value.BlockSizeInMultipleOfKBs = 256;
            fakeFileShareConfig.Value.ParallelUploadThreadCount = 0;
            fakeFileShareConfig.Value.BaseUrl = null;
            var responseBatchStatusModel = GetBatchStatusResponse();
            responseBatchStatusModel.Status = "";
            var jsonString = JsonConvert.SerializeObject(responseBatchStatusModel);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.Created, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored)).Returns(new string[] { "V01X01.zip" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored)).Returns(GetFileInfo());
            A.CallTo(() => fakeFileSystemHelper.UploadLargeMediaCommitBatch(A<List<BatchCommitMetaData>>.Ignored)).Returns(GetFileDetails());
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
                   async delegate { await fileShareService.CommitBatchToFss(fakeBatchId, fakeCorrelationId, fakeExchangeSetPath); });
        }

        [Test]
        public async Task WhenValidGetBatchStatusAsyncRequest_ThenReturnTrue()
        {
            fakeFileShareConfig.Value.ErrorFileName = "error.txt";
            fakeFileShareConfig.Value.BlockSizeInMultipleOfKBs = 256;
            fakeFileShareConfig.Value.ParallelUploadThreadCount = 0;
            fakeFileShareConfig.Value.BaseUrl = null;
            fakeFileShareConfig.Value.BatchCommitCutOffTimeInMinutes = 1;
            var responseBatchStatusModel = GetBatchStatusResponse();
            var jsonString = JsonConvert.SerializeObject(responseBatchStatusModel);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.Created, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored)).Returns(GetErrorFileInfo());
            A.CallTo(() => fakeFileSystemHelper.UploadLargeMediaCommitBatch(A<List<BatchCommitMetaData>>.Ignored)).Returns(GetFileDetails());
            A.CallTo(() => fakeFileShareServiceClient.CommitBatchAsync(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<BatchCommitModel>.Ignored, A<string>.Ignored, A<string>.Ignored))
            .Returns(httpResponse);
            A.CallTo(() => fakeFileShareServiceClient.GetBatchStatusAsync(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
            .Returns(httpResponse);

            bool result = await fileShareService.CommitBatchToFss(fakeBatchId, fakeCorrelationId, fakeExchangeSetPath, fakeFileShareConfig.Value.ErrorFileName);

            Assert.True(result);
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
            fakeFileShareConfig.Value.BatchCommitCutOffTimeInMinutes = 30;
            fakeFileShareConfig.Value.BatchCommitDelayTimeInMilliseconds = 100;

            var GetFileInfoDetails = GetFileInfo();
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored)).Returns(GetFileInfoDetails);

            var badUploadResponse = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.BadRequest,
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri("http://test.com")
                }
            };

            A.CallTo(() => fakeFileShareServiceClient.AddFileInBatchAsync(A<HttpMethod>.Ignored, A<FileCreateModel>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(badUploadResponse);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                   async delegate { await fileShareService.UploadFileToFileShareService(fakeBatchId, fakeExchangeSetPath, null, fakeFileShareConfig.Value.ExchangeSetFileName); });
        }
        #endregion UploadZipFile

        #region LargeMediaExchangeSet

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public async Task WhenGetBatchInfoBasedOnProducts_ThenReturnsSearchBatchResponseForLargeMediaExchangeSet(string businessUnit)
        {
            string postBodyParam = "This should be replace by actual value when param passed to api call";

            //Test variable
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            string correlationIdParam = null;
            var searchBatchResponse = GetSearchBatchResponse();
            var jsonString = JsonConvert.SerializeObject(searchBatchResponse);

            var httpResponse = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))),
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri("http://test.com")
                }
            };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceCache.GetNonCachedProductDataForFss(A<List<Products>>.Ignored, A<SearchBatchResponse>.Ignored, A<string>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(GetProductdetails());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationId) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationIdParam = correlationId;
               })
               .Returns(httpResponse);
            CommonHelper.IsPeriodicOutputService = true;

            var response = await fileShareService.GetBatchInfoBasedOnProducts(GetProductdetails(), GetScsResponseQueueMessage(), null, CancellationToken.None, string.Empty, businessUnit);

            Assert.IsNotNull(response);
            Assert.IsInstanceOf(typeof(SearchBatchResponse), response);
            Assert.AreEqual("63d38bde-5191-4a59-82d5-aa22ca1cc6dc", response.Entries[0].BatchId);
        }

        [Test]
        [TestCase("ADDS")]
        [TestCase("ADDS-S57")]
        public async Task WhenGetBatchInfoBasedOnProductsWithCancellation_ThenReturnsSearchBatchResponseForLargeMediaExchangeSet(string businessUnit)
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

            var httpResponse = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))),
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri("http://test.com")
                }
            };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationId) =>
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
                Cancellation = new Cancellation { EditionNumber = 3, UpdateNumber = 0 },
                Bundle = new List<Bundle> { new Bundle { BundleType = "DVD", Location = "M1;B1" } }
            });
            A.CallTo(() => fakeFileShareServiceCache.GetNonCachedProductDataForFss(A<List<Products>>.Ignored, A<SearchBatchResponse>.Ignored, A<string>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(productList);
            CommonHelper.IsPeriodicOutputService = true;

            var response = await fileShareService.GetBatchInfoBasedOnProducts(productList, GetScsResponseQueueMessage(), null, CancellationToken.None, string.Empty, businessUnit);

            Assert.IsNotNull(response);
            Assert.IsInstanceOf(typeof(SearchBatchResponse), response);
            Assert.AreEqual("63d38bde-5191-4a59-82d5-aa22ca1cc6dc", response.Entries[0].BatchId);
            Assert.AreEqual("13d38bde-5191-4a59-82d5-aa22ca1cc6de", response.Entries[1].BatchId);
        }

        [Test]
        public async Task WhenValidUploadLargeMediaZipFileRequest_ThenReturnTrue()
        {
            fakeFileShareConfig.Value.BlockSizeInMultipleOfKBs = 256;
            fakeFileShareConfig.Value.ParallelUploadThreadCount = 0;
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

            var response = await fileShareService.UploadLargeMediaFileToFileShareService(fakeBatchId, fakeExchangeSetPath, null, fakeLargeMediaZipFilePath);
            Assert.AreEqual(true, response);
        }

        [Test]
        public void WhenInvalidUploadLargeMediaZipFileRequest_ThenReturnFulfilmentException()
        {
            fakeFileShareConfig.Value.BlockSizeInMultipleOfKBs = 256;
            fakeFileShareConfig.Value.ParallelUploadThreadCount = 0;

            var GetFileInfoDetails = GetFileInfo();
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored)).Returns(GetFileInfoDetails);

            var badUploadResponse = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.BadRequest,
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri("http://test.com")
                }
            };

            A.CallTo(() => fakeFileShareServiceClient.AddFileInBatchAsync(A<HttpMethod>.Ignored, A<FileCreateModel>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(badUploadResponse);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                   async delegate { await fileShareService.UploadFileToFileShareService(fakeBatchId, fakeExchangeSetPath, null, fakeLargeMediaZipFilePath); });
        }

        [Test]
        public async Task WhenValidCommitAndGetBatchStatusForLargeMediaExchangeSet_ThenReturnTrue()
        {
            fakeFileShareConfig.Value.PosBatchCommitCutOffTimeInMinutes = 30;
            fakeFileShareConfig.Value.PosBatchCommitDelayTimeInMilliseconds = 100;

            var responseBatchStatusModel = GetBatchStatusResponse();
            var jsonString = JsonConvert.SerializeObject(responseBatchStatusModel);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.Created, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };
            var GetFileInfoDetails = GetFileInfo();

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored)).Returns(GetZipFileListForBatchCommit());
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored)).Returns(GetFileInfoDetails);
            A.CallTo(() => fakeFileSystemHelper.UploadLargeMediaCommitBatch(A<List<BatchCommitMetaData>>.Ignored)).Returns(GetLargeMediaFileDetails());
            A.CallTo(() => fakeFileShareServiceClient.CommitBatchAsync(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<BatchCommitModel>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);
            A.CallTo(() => fakeFileShareServiceClient.GetBatchStatusAsync(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);

            var response = await fileShareService.CommitAndGetBatchStatusForLargeMediaExchangeSet(fakeBatchId, fakeExchangeSetPath, null);

            Assert.AreEqual(true, response);
        }

        [Test]
        public void WhenInvalidCommitAndGetBatchStatusForLargeMediaExchangeSet_ThenReturnFulfilmentException()
        {
            fakeFileShareConfig.Value.PosBatchCommitCutOffTimeInMinutes = 30;
            fakeFileShareConfig.Value.PosBatchCommitDelayTimeInMilliseconds = 100;
            var badCommitBatchResponse = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.BadRequest,
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri("http://test.com")
                }
            };
            var GetFileInfoDetails = GetFileInfo();

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored)).Returns(GetZipFileListForBatchCommit());
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored)).Returns(GetFileInfoDetails);
            A.CallTo(() => fakeFileSystemHelper.UploadLargeMediaCommitBatch(A<List<BatchCommitMetaData>>.Ignored)).Returns(GetLargeMediaFileDetails());
            A.CallTo(() => fakeFileShareServiceClient.CommitBatchAsync(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<BatchCommitModel>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(badCommitBatchResponse);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                   async delegate { await fileShareService.CommitAndGetBatchStatusForLargeMediaExchangeSet(fakeBatchId, fakeExchangeSetPath, null); });
        }

        [Test]
        public void WhenBatchFailedForLargeMediaExchangeSet_ThenReturnFulfilmentException()
        {
            fakeFileShareConfig.Value.PosBatchCommitCutOffTimeInMinutes = 30;
            fakeFileShareConfig.Value.PosBatchCommitDelayTimeInMilliseconds = 100;
            var responseBatchStatusModel = GetBatchStatusFailedResponse();
            var jsonString = JsonConvert.SerializeObject(responseBatchStatusModel);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.Created, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };

            var GetFileInfoDetails = GetFileInfo();

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored)).Returns(GetZipFileListForBatchCommit());
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored)).Returns(GetFileInfoDetails);
            A.CallTo(() => fakeFileSystemHelper.UploadLargeMediaCommitBatch(A<List<BatchCommitMetaData>>.Ignored)).Returns(GetLargeMediaFileDetails());
            A.CallTo(() => fakeFileShareServiceClient.CommitBatchAsync(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<BatchCommitModel>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);
            A.CallTo(() => fakeFileShareServiceClient.GetBatchStatusAsync(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                   async delegate { await fileShareService.CommitAndGetBatchStatusForLargeMediaExchangeSet(fakeBatchId, fakeExchangeSetPath, null); });
        }

        #region SearchFolderFiles
        [Test]
        public async Task WhenValidSearchFolderFilesRequest_ThenReturnValidFilePath()
        {
            string postBodyParam = "This should be replace by actual value when param passed to api call";
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            string batchId = "a9e518ee-25b0-42ae-96c7-49dafc553c40";
            string correlationId = "2561fa76-ae35-4bdf-996f-75a3389ab1ad";
            var searchFolderFileName = @"batch/a9e518ee-25b0-42ae-96c7-49dafc553c40/files/TPNMS Diagrams.zip";
            string correlationidParam = null;

            var searchBatchResponse = GetSearchBatchResponse();
            var jsonResponse = JsonConvert.SerializeObject(searchBatchResponse);

            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonResponse))) };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationid) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationidParam = correlationid;
               })
               .Returns(httpResponse);

            var response = await fileShareService.SearchFolderDetails(batchId, correlationId, null);
            string expectedSearchFolderFilePath = @"batch/a9e518ee-25b0-42ae-96c7-49dafc553c40/files/TPNMS Diagrams.zip";
            Assert.IsNotNull(response);
            Assert.AreEqual(expectedSearchFolderFilePath, searchFolderFileName);
        }

        [Test]
        public Task WhenSearchSearchFolderFilesNotFound_ThenReturnFulfilmentException()
        {
            string postBodyParam = "This should be replace by actual value when param passed to api call";
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            string batchId = "a9e518ee-25b0-42ae-96c7-49dafc553c40";
            string searchFolderFileName = null;
            string correlationidParam = null;

            var searchBatchResponse = GetSearchBatchEmptyResponse();
            var jsonResponse = JsonConvert.SerializeObject(searchBatchResponse);

            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonResponse))) };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationid) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationidParam = correlationid;
               })
               .Returns(httpResponse);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                 async delegate { await fileShareService.SearchFolderDetails(batchId, string.Empty, searchFolderFileName); });
            return Task.CompletedTask;
        }

        [Test]
        public void WhenInvalidSearchFolderFilesRequest_ThenReturnFulfilmentException()
        {
            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request"))) });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                  async delegate { await fileShareService.SearchFolderDetails(string.Empty, string.Empty, string.Empty); });
        }
        #endregion SearchFolderFiles 

        #region DownloadFolderFiles
        [Test]
        public async Task WhenValidDownloadFolderFileRequest_ThenReturnTrue()
        {
            var searchFolderFileName = @"batch/a9e518ee-25b0-42ae-96c7-49dafc553c40/files/TPNMS Diagrams.zip";
            var searchBatchResponse = GetSearchBatchResponse();
            var jsonResponse = JsonConvert.SerializeObject(searchBatchResponse);
            string postBodyParam = "This should be replace by actual value when param passed to api call";
            string accessTokenParam = null;
            string uriParam = null;
            string correlationidParam = null;
            HttpMethod httpMethodParam = null;
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonResponse))) };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationid) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationidParam = correlationid;
               })
               .Returns(httpResponse);

            var batchFileList = new List<BatchFile>() {
                new BatchFile{  Filename = "TPNMS Diagrams.zip", FileSize = 400, Links = new Links { Get = new Link { Href = "" } } }

            };

            var response = await fileShareService.DownloadFolderDetails(fakeBatchId, correlationidParam, batchFileList, fakeExchangeSetPath);

            var expectedFolderFilePath = @"batch/a9e518ee-25b0-42ae-96c7-49dafc553c40/files/TPNMS Diagrams.zip";
            Assert.AreEqual(true, response);
            Assert.AreEqual(expectedFolderFilePath, searchFolderFileName);
        }

        [Test]
        public void WhenInvalidDownloadAdcFolderFileRequest_ThenReturnFulfilmentException()
        {
            var searchBatchResponse = GetSearchBatchResponse();
            var jsonResponse = JsonConvert.SerializeObject(searchBatchResponse);
            string postBodyParam = "This should be replace by actual value when param passed to api call";
            string accessTokenParam = null;
            string uriParam = null;
            string correlationidParam = null;
            HttpMethod httpMethodParam = null;
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonResponse))), RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://test.com") } };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationid) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationidParam = correlationid;
               })
               .Returns(httpResponse);

            var batchFileList = new List<BatchFile>() {
                new BatchFile{  Filename = "TPNMS Diagrams.zip", FileSize = 400, Links = new Links { Get = new Link { Href = "" } } }

            };

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                 async delegate { await fileShareService.DownloadFolderDetails(fakeBatchId, correlationidParam, batchFileList, fakeExchangeSetPath); });
        }
        #endregion DownloadFolderFiles 

        #endregion

        [Test]
        public async Task WhenGetBatchInfoBasedOnProducts_ThenReturnsSearchBatchResponseForLargeMediaExchangeSetWithAioProductAndAioToggleDisabled()
        {
            string postBodyParam = "This should be replace by actual value when param passed to api call";
            string businessUnit = "ADDS";
            //Test variable
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            string correlationIdParam = null;
            var searchBatchResponse = GetSearchBatchResponse();
            var jsonString = JsonConvert.SerializeObject(searchBatchResponse);

            var httpResponse = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))),
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri("http://test.com")
                }
            };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceCache.GetNonCachedProductDataForFss(A<List<Products>>.Ignored, A<SearchBatchResponse>.Ignored,
                A<string>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored,
                A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(GetAioProductdetails());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationId) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationIdParam = correlationId;
               })
               .Returns(httpResponse);
            CommonHelper.IsPeriodicOutputService = true;
            fakeAioConfiguration.Value.AioEnabled = false;
            fakeAioConfiguration.Value.AioCells = "GB800001";

            var response = await fileShareService.GetBatchInfoBasedOnProducts(GetProductdetails(), GetScsResponseQueueMessage(), null, CancellationToken.None, string.Empty, businessUnit);

            Assert.IsNotNull(response);
            Assert.IsInstanceOf(typeof(SearchBatchResponse), response);
            Assert.AreEqual("63d38bde-5191-4a59-82d5-aa22ca1cc6dc", response.Entries[0].BatchId);
        }

        [Test]
        public async Task WhenGetBatchInfoBasedOnProducts_ThenReturnsSearchBatchResponseForLargeMediaExchangeSetWithAioProductAndAioToggleEnabled()
        {
            string postBodyParam = "This should be replace by actual value when param passed to api call";
            string businessUnit = "ADDS";
            //Test variable
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            string correlationIdParam = null;
            var searchBatchResponse = GetSearchBatchResponse();
            searchBatchResponse.Entries.Add(new BatchDetail
            {
                BatchId = "13d38bde-5191-4a59-82d5-aa22ca1cc6de",
                Files = new List<BatchFile>() { new BatchFile { Filename = "test1.txt", FileSize = 400, Links = new Links { Get = new Link { Href = "" } } } },
                Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "GB" } ,
                                                           new Attribute { Key= "CellName", Value= "GB800001" },
                                                           new Attribute { Key= "EditionNumber", Value= "0" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "AVCS" }},
            });
            var jsonString = JsonConvert.SerializeObject(searchBatchResponse);

            var httpResponse = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))),
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri("http://test.com")
                }
            };

            A.CallTo(() => fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(GetFakeToken());
            A.CallTo(() => fakeFileShareServiceCache.GetNonCachedProductDataForFss(A<List<Products>>.Ignored, A<SearchBatchResponse>.Ignored,
                A<string>.Ignored, A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<CancellationTokenSource>.Ignored,
                A<CancellationToken>.Ignored, A<string>.Ignored)).Returns(GetAioProductdetails());
            A.CallTo(() => fakeFileShareServiceClient.CallFileShareServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string accessToken, string uri, CancellationToken cancellationToken, string correlationId) =>
               {
                   accessTokenParam = accessToken;
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
                   correlationIdParam = correlationId;
               })
               .Returns(httpResponse);
            CommonHelper.IsPeriodicOutputService = true;
            fakeAioConfiguration.Value.AioEnabled = true;
            fakeAioConfiguration.Value.AioCells = "GB800001";

            var response = await fileShareService.GetBatchInfoBasedOnProducts(GetAioProductdetails(), GetScsResponseQueueMessage(), null, CancellationToken.None, string.Empty, businessUnit);

            Assert.IsNotNull(response);
            Assert.IsInstanceOf(typeof(SearchBatchResponse), response);
            Assert.AreEqual("63d38bde-5191-4a59-82d5-aa22ca1cc6dc", response.Entries[0].BatchId);
        }
    }
}