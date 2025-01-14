using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Configuration;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentCallBackServiceTest
    {
        private IOptions<EssCallBackConfiguration> fakeEssCallBackConfiguration;
        private ICallBackClient fakeCallBackClient;
        private IOptions<FileShareServiceConfiguration> fakeFileShareServiceConfig;
        private FulfilmentCallBackService fulfilmentCallBackService;
        private ILogger<FulfilmentCallBackService> fakeLogger;
        private string postBodyParam;
        private string uriParam;
        private HttpMethod httpMethodParam;
        private SalesCatalogueProductResponse salesCatalogueProductResponse;
        private SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage;
        private IOptions<AioConfiguration> fakeAioConfiguration;
        private readonly List<string> aioCells = ["GB800001"];

        [SetUp]
        public void Setup()
        {
            postBodyParam = "This should be replace by actual value when param passed to api call";
            uriParam = null;
            httpMethodParam = null;
            salesCatalogueProductResponse = GetSalesCatalogueServiceResponse();
            scsResponseQueueMessage = GetScsResponseQueueMessage();

            fakeEssCallBackConfiguration = Options.Create(new EssCallBackConfiguration() { });
            fakeCallBackClient = A.Fake<ICallBackClient>();
            fakeFileShareServiceConfig = Options.Create(new FileShareServiceConfiguration() { });
            fakeLogger = A.Fake<ILogger<FulfilmentCallBackService>>();
            fakeAioConfiguration = Options.Create(new AioConfiguration() { AioCells = string.Join<string>(",", aioCells) });

            fulfilmentCallBackService = new FulfilmentCallBackService(fakeEssCallBackConfiguration, fakeCallBackClient, fakeFileShareServiceConfig, fakeLogger, fakeAioConfiguration);
        }

        #region GetSalesCatalogueServiceResponse

        private static SalesCatalogueProductResponse GetSalesCatalogueServiceResponse()
        {
            return new SalesCatalogueProductResponse
            {
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 12,
                    RequestedProductsAlreadyUpToDateCount = 5,
                    RequestedProductsNotReturned =
                    [
                        new RequestedProductsNotReturned
                        {
                            ProductName = "test",
                            Reason = "notfound"
                        }
                    ],
                    ReturnedProductCount = 4
                },
                Products =
                [
                    new Products
                    {
                        ProductName = "DE5NOBRK",
                        EditionNumber = 0,
                        UpdateNumbers = [0, 1],
                        FileSize = 400
                    }
                ]
            };
        }

        #endregion

        #region GetScsResponseQueueMessage

        private static SalesCatalogueServiceResponseQueueMessage GetScsResponseQueueMessage()
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

        #endregion

        [Test]
        public async Task WhenEmptyCallBackUriInRequest_ThenSendCallBackResponseReturnsFalse()
        {
            scsResponseQueueMessage.CallbackUri = "";

            var response = await fulfilmentCallBackService.SendCallBackResponse(salesCatalogueProductResponse, scsResponseQueueMessage);

            Assert.That(response, Is.False);
        }

        [Test]
        public async Task WhenCallBackApiSocketExceptionFound_ThenSendCallBackResponseReturnsFalse()
        {
            A.CallTo(() => fakeCallBackClient.CallBackApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Throws(new SocketException());

            var response = await fulfilmentCallBackService.SendCallBackResponse(salesCatalogueProductResponse, scsResponseQueueMessage);

            Assert.That(response, Is.False);
        }

        [Test]
        public async Task WhenCallBackUriInRequest_ThenSendCallBackResponseReturnsTrue()
        {
            A.CallTo(() => fakeCallBackClient.CallBackApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Invokes((HttpMethod method, string postBody, string uri) =>
                {
                    uriParam = uri;
                    httpMethodParam = method;
                    postBodyParam = postBody;
                    var callBackResponse = JsonConvert.DeserializeObject<CallBackResponse>(postBody);
                    Assert.That(callBackResponse, Is.Not.Null, "PostBody can not be null");
                    Assert.That(callBackResponse.Data.BatchId, Is.EqualTo(scsResponseQueueMessage.BatchId));
                });

            var response = await fulfilmentCallBackService.SendCallBackResponse(salesCatalogueProductResponse, scsResponseQueueMessage);

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.True);
                Assert.That(uriParam, Is.EqualTo(scsResponseQueueMessage.CallbackUri));
                Assert.That(httpMethodParam, Is.EqualTo(HttpMethod.Post));
                Assert.That(postBodyParam, Is.Not.Null);
            });
        }


        [Test]
        public async Task WhenEmptyCallBackUriInRequest_ThenSendCallBackErrorResponseReturnsFalse()
        {
            scsResponseQueueMessage.CallbackUri = "";

            var response = await fulfilmentCallBackService.SendCallBackErrorResponse(salesCatalogueProductResponse, scsResponseQueueMessage);

            Assert.That(response, Is.False);
        }

        [Test]
        public async Task WhenCallBackApiSocketExceptionFound_ThenSendCallBackErrorResponseReturnsFalse()
        {
            A.CallTo(() => fakeCallBackClient.CallBackApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .Throws(new SocketException());

            var response = await fulfilmentCallBackService.SendCallBackErrorResponse(salesCatalogueProductResponse, scsResponseQueueMessage);

            Assert.That(response, Is.False);
        }

        [Test]
        public async Task WhenCallBackUriInRequest_ThenSendCallBackErrorResponseReturnsTrue()
        {
            A.CallTo(() => fakeCallBackClient.CallBackApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Invokes((HttpMethod method, string postBody, string uri) =>
                {
                    uriParam = uri;
                    httpMethodParam = method;
                    postBodyParam = postBody;
                });

            var response = await fulfilmentCallBackService.SendCallBackErrorResponse(salesCatalogueProductResponse, scsResponseQueueMessage);

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.True);
                Assert.That(uriParam, Is.EqualTo(scsResponseQueueMessage.CallbackUri));
                Assert.That(httpMethodParam, Is.EqualTo(HttpMethod.Post));
                Assert.That(postBodyParam, Is.Not.Null);
            });
        }

        #region ValidateCallbackRequestPayload

        [Test, TestCaseSource(nameof(GetValidateCallbackRequestPayloadWithProductCountTestData))]
        public void WhenValidateCallbackRequestPayloadWithProductCount_ThenReturnResult(int requestedProductCount, int? requestedAioProductCount, bool expectedResult)

        {
            var exchangeSetResponse = GetExchangeSetResponse();
            exchangeSetResponse.RequestedProductCount = requestedProductCount;
            exchangeSetResponse.RequestedAioProductCount = requestedAioProductCount;

            var callBackResponse = new CallBackResponse
            {
                Id = "response id",
                Data = exchangeSetResponse
            };

            var result = fulfilmentCallBackService.ValidateCallbackRequestPayload(callBackResponse);

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("batch status uri", true, TestName = "When ExchangeSetBatchStatusUri is not null")]
        [TestCase(null, false, TestName = "When ExchangeSetBatchStatusUri is null")]
        public void WhenValidateCallbackRequestPayloadWithExchangeSetBatchStatusUri_ThenReturnResult(string exchangeSetBatchStatusUri, bool expectedResult)
        {
            var exchangeSetResponse = GetExchangeSetResponse((e) => e.Links.ExchangeSetBatchStatusUri.Href = exchangeSetBatchStatusUri);

            var result = fulfilmentCallBackService.ValidateCallbackRequestPayload(GetCallBackResponse(exchangeSetResponse));

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("batch detail uri", true, TestName = "When ExchangeSetBatchDetailsUri is not null")]
        [TestCase(null, false, TestName = "When ExchangeSetBatchDetailsUri is null")]
        public void WhenValidateCallbackRequestPayloadWithExchangeSetBatchDetailsUri_ThenReturnResult(string exchangeSetBatchDetailsUri, bool expectedResult)
        {
            var exchangeSetResponse = GetExchangeSetResponse((e) => e.Links.ExchangeSetBatchDetailsUri.Href = exchangeSetBatchDetailsUri);

            var result = fulfilmentCallBackService.ValidateCallbackRequestPayload(GetCallBackResponse(exchangeSetResponse));

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test, TestCaseSource(nameof(GetValidateCallbackRequestPayloadWithFileUriTestData))]
        public void WhenValidateCallbackRequestPayloadWithFileUri_ThenReturnResult(bool isExchangeSetFileUriNull, string exchangeSetFileUri, bool isAioExchangeSetFileUri, string aioExchangeSetFileUri, bool expectedResult)
        {
            var exchangeSetResponse = GetExchangeSetResponse((e) =>
            {
                if (isExchangeSetFileUriNull)
                {
                    e.Links.ExchangeSetFileUri = null;
                }
                else
                {
                    e.Links.ExchangeSetFileUri.Href = exchangeSetFileUri;
                }

                if (isAioExchangeSetFileUri)
                {
                    e.Links.AioExchangeSetFileUri = null;
                }
                else
                {
                    e.Links.AioExchangeSetFileUri.Href = aioExchangeSetFileUri;
                }
            });

            var result = fulfilmentCallBackService.ValidateCallbackRequestPayload(GetCallBackResponse(exchangeSetResponse));

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("response Id", true, TestName = "When CallBackResponse Id is not null")]
        [TestCase(null, false, TestName = "When CallBackResponse Id is null")]
        public void WhenValidateCallbackRequestPayloadWithId_ThenReturnResult(string callBackResponseId, bool expectedResult)
        {
            var callBackResponse = new CallBackResponse
            {
                Data = GetExchangeSetResponse(),
                Id = callBackResponseId
            };

            var result = fulfilmentCallBackService.ValidateCallbackRequestPayload(callBackResponse);

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        #endregion

        #region ValidateCallbackErrorRequestPayload

        [Test, TestCaseSource(nameof(GetValidateCallbackRequestPayloadWithProductCountTestData))]
        public void WhenValidateCallbackErrorRequestPayloadWithProductCount_ThenReturnResult(int requestedProductCount, int? requestedAioProductCount, bool expectedResult)
        {
            var exchangeSetResponse = GetExchangeSetResponseForErrorCallback(a =>
            {
                a.RequestedProductCount = requestedProductCount;
                a.RequestedAioProductCount = requestedAioProductCount;
            });

            var callBackResponse = new CallBackResponse
            {
                Id = "response id",
                Data = exchangeSetResponse
            };

            var result = fulfilmentCallBackService.ValidateCallbackErrorRequestPayload(callBackResponse);

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("batch status uri", true, TestName = "When ExchangeSetBatchStatusUri is not null")]
        [TestCase(null, false, TestName = "When ExchangeSetBatchStatusUri is null")]
        public void WhenValidateCallbackErrorRequestPayloadWithExchangeSetBatchStatusUri_ThenReturnResult(string exchangeSetBatchStatusUri, bool expectedResult)
        {
            var exchangeSetResponse = GetExchangeSetResponseForErrorCallback((e) => e.Links.ExchangeSetBatchStatusUri.Href = exchangeSetBatchStatusUri);

            var result = fulfilmentCallBackService.ValidateCallbackErrorRequestPayload(GetCallBackResponse(exchangeSetResponse));

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("batch detail uri", true, TestName = "When ExchangeSetBatchDetailsUri is not null")]
        [TestCase(null, false, TestName = "When ExchangeSetBatchDetailsUri is null")]
        public void WhenValidateCallbackErrorRequestPayloadWithExchangeSetBatchDetailsUri_ThenReturnResult(string exchangeSetBatchDetailsUri, bool expectedResult)
        {
            var exchangeSetResponse = GetExchangeSetResponseForErrorCallback((e) => e.Links.ExchangeSetBatchDetailsUri.Href = exchangeSetBatchDetailsUri);

            var result = fulfilmentCallBackService.ValidateCallbackErrorRequestPayload(GetCallBackResponse(exchangeSetResponse));

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("exchange set file uri", false, TestName = "When ExchangeSetFileUri is not null")]
        [TestCase(null, true, TestName = "When ExchangeSetFileUri is null")]
        public void WhenValidateCallbackErrorRequestPayloadWithExchangeSetFileUri_ThenReturnResult(string exchangeSetFileUri, bool expectedResult)
        {
            var exchangeSetResponse = GetExchangeSetResponseForErrorCallback((e) =>
            {
                e.Links.ExchangeSetFileUri = exchangeSetFileUri != null ? new LinkSetFileUri() : null;
            });

            var result = fulfilmentCallBackService.ValidateCallbackErrorRequestPayload(GetCallBackResponse(exchangeSetResponse));

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("aio exchange set file uri", false, TestName = "When AioExchangeSetFileUri is not null")]
        [TestCase(null, true, TestName = "When AioExchangeSetFileUri is null")]
        public void WhenValidateCallbackErrorRequestPayloadWithAioExchangeSetFileUri_ThenReturnResult(string aioExchangeSetFileUri, bool expectedResult)
        {
            var exchangeSetResponse = GetExchangeSetResponseForErrorCallback((e) =>
            {
                e.Links.AioExchangeSetFileUri = aioExchangeSetFileUri != null ? new LinkSetFileUri() : null;
            });

            var result = fulfilmentCallBackService.ValidateCallbackErrorRequestPayload(GetCallBackResponse(exchangeSetResponse));

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("response Id", true, TestName = "When CallBackResponse Id is not null")]
        [TestCase(null, false, TestName = "When CallBackResponse Id is null")]
        public void WhenValidateCallbackErrorRequestPayloadWithId_ThenReturnResult(string callBackResponseId, bool expectedResult)
        {
            var callBackResponse = new CallBackResponse
            {
                Data = GetExchangeSetResponseForErrorCallback(),
                Id = callBackResponseId
            };

            var result = fulfilmentCallBackService.ValidateCallbackErrorRequestPayload(callBackResponse);

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase(1, false, TestName = "When ExchangeSetCellCount is not 0")]
        [TestCase(0, true, TestName = "When ExchangeSetCellCount is 0")]
        public void WhenValidateCallbackErrorRequestPayloadWithExchangeSetCellCount_ThenReturnResult(int exchangeSetCellCount, bool expectedResult)
        {
            var exchangeSetResponse = GetExchangeSetResponseForErrorCallback((e) => e.ExchangeSetCellCount = exchangeSetCellCount);

            var result = fulfilmentCallBackService.ValidateCallbackErrorRequestPayload(GetCallBackResponse(exchangeSetResponse));

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase(null, true, TestName = "When AioExchangeSetCellCount is null")]
        [TestCase(0, true, TestName = "When AioExchangeSetCellCount is 0")]
        [TestCase(1, false, TestName = "When AioExchangeSetCellCount is not 0")]
        public void WhenValidateCallbackErrorRequestPayloadWithExchangeSetCellCount_ThenReturnResult(int? aioExchangeSetCellCount, bool expectedResult)
        {
            var exchangeSetResponse = GetExchangeSetResponseForErrorCallback((e) => e.AioExchangeSetCellCount = aioExchangeSetCellCount);

            var result = fulfilmentCallBackService.ValidateCallbackErrorRequestPayload(GetCallBackResponse(exchangeSetResponse));

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        #endregion

        #region SetExchangeSetResponse

        [Test, TestCaseSource(nameof(GetExchangeSetResponseTestData), new object[] { true })]
        public void WhenSetExchangeSetResponseWithAioEnabled_ThenReturnValidExchangeSetResponse(bool isAioReturned, bool isEncReturned, bool isEmptyEncExchangeSet, bool isEmptyAioExchangeSet)
        {
            var scProductResponse = GetSalesCatalogueProductResponse(a =>
            {
                if (!isEncReturned)
                {
                    a.Products.Clear();
                    a.ProductCounts.RequestedProductCount = 0;
                    a.ProductCounts.ReturnedProductCount = 0;
                    a.ProductCounts.RequestedProductsAlreadyUpToDateCount = 0;
                }

                if (isAioReturned)
                {
                    a.Products.Add(new Products { ProductName = aioCells.First() });
                    a.ProductCounts.RequestedProductCount += 1;
                    a.ProductCounts.ReturnedProductCount += 1;
                    a.ProductCounts.RequestedProductsAlreadyUpToDateCount += 1;
                }
            });

            var queueMessage = GetSalesCatalogueServiceResponseQueueMessage(a =>
            {
                a.IsEmptyEncExchangeSet = isEmptyEncExchangeSet;
                a.IsEmptyAioExchangeSet = isEmptyAioExchangeSet;
            });

            var result = fulfilmentCallBackService.SetExchangeSetResponse(scProductResponse, queueMessage);

            Assert.That(result, Is.Not.Null);

            if (isAioReturned || isEmptyAioExchangeSet)
            {
                Assert.That(result.Links.AioExchangeSetFileUri, Is.Not.Null, "AioExchangeSetFileUri can not be null");
            }
            else
            {
                Assert.That(result.Links.AioExchangeSetFileUri, Is.Null, "AioExchangeSetFileUri should be null");
            }

            Assert.Multiple(() =>
            {
                Assert.That(result.AioExchangeSetCellCount, Is.Not.Null);
                Assert.That(result.RequestedAioProductsAlreadyUpToDateCount, Is.Not.Null);
            });

            if (isEncReturned || isEmptyEncExchangeSet || result.RequestedProductsNotInExchangeSet.Count != 0)
            {
                Assert.That(result.Links.ExchangeSetFileUri, Is.Not.Null);
            }
            else
            {
                Assert.That(result.Links.ExchangeSetFileUri, Is.Null);
            }

            Assert.Multiple(() =>
            {
                Assert.That(result.ExchangeSetCellCount, Is.GreaterThanOrEqualTo(0));
                Assert.That(result.RequestedProductsAlreadyUpToDateCount, Is.GreaterThanOrEqualTo(0));
            });
        }

        #endregion

        private static CallBackResponse GetCallBackResponse(ExchangeSetResponse exchangeSetResponse)
        {
            return new CallBackResponse
            {
                Id = "response id",
                Data = exchangeSetResponse
            };
        }

        private static ExchangeSetResponse GetExchangeSetResponse(Action<ExchangeSetResponse> action = null)
        {
            var linkSetBatchStatusUri = new LinkSetBatchStatusUri
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/status"
            };

            var linkSetBatchDetailsUri = new LinkSetBatchDetailsUri
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
            };

            var linkSetEncFileUri = new LinkSetFileUri
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip",
            };

            var linkSetAioFileUri = new LinkSetFileUri
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/aio123.zip",
            };

            var links = new Links
            {
                ExchangeSetBatchStatusUri = linkSetBatchStatusUri,
                ExchangeSetBatchDetailsUri = linkSetBatchDetailsUri,
                ExchangeSetFileUri = linkSetEncFileUri,
                AioExchangeSetFileUri = linkSetAioFileUri
            };

            var exchangeSetResponse = new ExchangeSetResponse
            {
                Links = links,
                RequestedProductCount = 10,
                RequestedAioProductCount = 1
            };

            action?.Invoke(exchangeSetResponse);

            return exchangeSetResponse;
        }

        private static ExchangeSetResponse GetExchangeSetResponseForErrorCallback(Action<ExchangeSetResponse> action = null)
        {
            var linkSetBatchStatusUri = new LinkSetBatchStatusUri
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/status"
            };

            var linkSetBatchDetailsUri = new LinkSetBatchDetailsUri
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
            };

            var links = new Links
            {
                ExchangeSetBatchStatusUri = linkSetBatchStatusUri,
                ExchangeSetBatchDetailsUri = linkSetBatchDetailsUri

            };

            var exchangeSetResponse = new ExchangeSetResponse
            {
                Links = links,
                RequestedProductCount = 10,
                RequestedAioProductCount = 0,
                ExchangeSetCellCount = 0
            };

            action?.Invoke(exchangeSetResponse);

            return exchangeSetResponse;
        }

        private static SalesCatalogueProductResponse GetSalesCatalogueProductResponse(Action<SalesCatalogueProductResponse> action = null)
        {
            var response = new SalesCatalogueProductResponse
            {
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 3,
                    RequestedProductsAlreadyUpToDateCount = 2,
                    ReturnedProductCount = 2,
                    RequestedProductsNotReturned =
                    [
                        new RequestedProductsNotReturned { ProductName = "AU000004", Reason = "productWithdrawn" }
                    ]
                },
                Products =
                [
                    new Products { ProductName = "AU000001" },
                    new Products { ProductName = "AU000002"}
                ]
            };

            action?.Invoke(response);

            return response;
        }

        private static SalesCatalogueServiceResponseQueueMessage GetSalesCatalogueServiceResponseQueueMessage(Action<SalesCatalogueServiceResponseQueueMessage> action = null)
        {
            var response = new SalesCatalogueServiceResponseQueueMessage
            {
                BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272",
                FileSize = 4000,
                ScsResponseUri = "https://test/ess-test/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272.json",
                CallbackUri = "https://test-callbackuri.com",
                CorrelationId = "727c5230-2c25-4244-9580-13d90004584a",
                ExchangeSetUrlExpiryDate = DateTime.Today.AddDays(1).ToUniversalTime().ToString(CultureInfo.CurrentCulture),
                RequestedAioProductCount = 1,
                RequestedProductCount = 2,
                RequestedProductsAlreadyUpToDateCount = 2,
                RequestedAioProductsAlreadyUpToDateCount = 1,
                IsEmptyAioExchangeSet = true,
                IsEmptyEncExchangeSet = true
            };

            action?.Invoke(response);

            return response;
        }

        private static IEnumerable<TestCaseData> GetExchangeSetResponseTestData(bool isAioEnabled)
        {
            if (!isAioEnabled)
            {
                yield return new TestCaseData(true, true, false)
                    .SetName("When sales catalogue contains both AIO and ENCs");

                yield return new TestCaseData(false, true, false)
                    .SetName("When sales catalogue contains only ENCs");

                yield return new TestCaseData(false, false, true)
                    .SetName("When sales catalogue does not contains AIO or ENCs and empty exchange set is true");

                yield return new TestCaseData(false, false, false)
                    .SetName("When sales catalogue does not contains AIO or ENCs and empty exchange set is false");
            }
            else
            {
                yield return new TestCaseData(true, true, false, false)
                    .SetName("When sales catalogue contains both AIO and ENCs");

                yield return new TestCaseData(false, true, false, false)
                    .SetName("When sales catalogue contains only ENCs");

                yield return new TestCaseData(false, false, true, true)
                    .SetName("When sales catalogue does not contains AIO or ENCs and empty exchange set and empty AIO exchange set are true");

                yield return new TestCaseData(false, false, false, false)
                    .SetName("When sales catalogue does not contains AIO or ENCs and empty exchange set and empty AIO exchange set are false");

                yield return new TestCaseData(false, false, true, false)
                    .SetName("When sales catalogue does not contains AIO or ENCs and empty exchange set is true and empty AIO exchange set is false");
            }
        }

        private static IEnumerable<TestCaseData> GetValidateCallbackRequestPayloadWithFileUriTestData()
        {
            yield return new TestCaseData(false, "enc file uri", false, "aio file uri", true)
                .SetName("When ExchangeSetFileUri and AioExchangeSetFileUri are not null");

            yield return new TestCaseData(false, null, false, "aio file uri", true)
                .SetName("When ExchangeSetFileUri Href is null and AioExchangeSetFileUri is not null");

            yield return new TestCaseData(true, null, false, "aio file uri", true)
                .SetName("When ExchangeSetFileUri is null and AioExchangeSetFileUri is not null");

            yield return new TestCaseData(false, "enc file uri", false, null, true)
                .SetName("When ExchangeSetFileUri is not null and AioExchangeSetFileUri Href is null");

            yield return new TestCaseData(false, "enc file uri", true, null, true)
                .SetName("When ExchangeSetFileUri is not null and AioExchangeSetFileUri is null");

            yield return new TestCaseData(true, null, true, null, false)
                .SetName("When ExchangeSetFileUri and AioExchangeSetFileUri are null");

            yield return new TestCaseData(false, null, false, null, false)
                .SetName("When ExchangeSetFileUri Href and AioExchangeSetFileUri Href are null");
        }

        private static IEnumerable<TestCaseData> GetValidateCallbackRequestPayloadWithProductCountTestData()
        {
            yield return new TestCaseData(0, 0, true).SetName(
                "When RequestedProductCount is 0 and RequestedAioProductCount is 0");

            yield return new TestCaseData(0, 2, true).SetName(
                "When RequestedProductCount is 0 and RequestedAioProductCount is more than 0");

            yield return new TestCaseData(2, 0, true).SetName(
                "When RequestedProductCount is more than 0 and RequestedAioProductCount is 0");
            yield return new TestCaseData(2, 2, true).SetName(
                "When RequestedProductCount and RequestedAioProductCount are more than 0");
            yield return new TestCaseData(0, 0, true).SetName(
                "When RequestedProductCount and RequestedAioProductCount are 0");
            yield return new TestCaseData(-1, 0, true).SetName(
                "When RequestedProductCount is less than 0 and RequestedAioProductCount is 0");
            yield return new TestCaseData(0, -1, true).SetName(
                "When RequestedProductCount is 0 and RequestedAioProductCount is less than 0");
            yield return new TestCaseData(-1, -1, false).SetName(
                "When RequestedProductCount and RequestedAioProductCount are less than 0");
            yield return new TestCaseData(-1, null, false).SetName(
                "When RequestedProductCount is less than 0 and RequestedAioProductCount is null");
            yield return new TestCaseData(0, null, true).SetName(
                "When RequestedProductCount is 0 and RequestedAioProductCount is null");
        }
    }
}
