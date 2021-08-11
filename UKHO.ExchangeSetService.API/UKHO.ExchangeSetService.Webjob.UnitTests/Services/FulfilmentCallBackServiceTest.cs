using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
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
        public IOptions<EssCallBackConfiguration> fakeEssCallBackConfiguration;
        public ICallBackClient fakeCallBackClient;
        public IOptions<FileShareServiceConfiguration> fakeFileShareServiceConfig;
        public FulfilmentCallBackService fulfilmentCallBackService;
        public ILogger<FulfilmentCallBackService> fakeLogger;
        public string postBodyParam;
        public string uriParam;
        public HttpMethod httpMethodParam;
        public SalesCatalogueProductResponse salesCatalogueProductResponse;
        public SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage;

        [SetUp]
        public void Setup()
        {
            postBodyParam = "This should be replace by actual value when param passed to api call";
            uriParam = null;
            httpMethodParam = null;
            salesCatalogueProductResponse = GetSalesCatalogueServiceResponse();
            scsResponseQueueMessage = GetScsResponseQueueMessage();

            fakeEssCallBackConfiguration = Options.Create(new EssCallBackConfiguration(){});
            fakeCallBackClient = A.Fake<ICallBackClient>();
            fakeFileShareServiceConfig = Options.Create(new FileShareServiceConfiguration(){ });
            fakeLogger = A.Fake<ILogger<FulfilmentCallBackService>>();

            fulfilmentCallBackService = new FulfilmentCallBackService(fakeEssCallBackConfiguration, fakeCallBackClient, fakeFileShareServiceConfig, fakeLogger);
        }

        #region GetCallBackResponse
        public CallBackResponse GetCallBackResponse()
        {
            return new CallBackResponse() 
            {
                SpecVersion="1.0",
                Type = "test",
                Source = "test",
                Subject = "test",
                Time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                DataContentType = "application/json",
                Data = new ExchangeSetResponse()
            };
        }
        #endregion

        #region GetSalesCatalogueServiceResponse
        private static SalesCatalogueProductResponse GetSalesCatalogueServiceResponse()
        {
            return new SalesCatalogueProductResponse()
            {
                ProductCounts = new ProductCounts()
                {
                    RequestedProductCount = 12,
                    RequestedProductsAlreadyUpToDateCount = 5,
                    RequestedProductsNotReturned = new List<RequestedProductsNotReturned>
                    {
                        new RequestedProductsNotReturned()
                        {
                            ProductName = "test",
                            Reason = "notfound"
                        }
                    },
                    ReturnedProductCount = 4
                },
                Products = new List<Products> {
                            new Products {
                                ProductName = "DE5NOBRK",
                                EditionNumber = 0,
                                UpdateNumbers = new List<int?> {0,1},
                                FileSize = 400
                            }
                        }
            };
        }
        #endregion

        #region GetScsResponseQueueMessage
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
        #endregion

        [Test]
        public async Task WhenIncorrectCallBackPayloadInRequest_ThenCallBackApiIsNotCalled()
        {
            salesCatalogueProductResponse.ProductCounts.RequestedProductCount = 0;

            A.CallTo(() => fakeCallBackClient.CallBackApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string uri) =>
               {
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
               });

            var response = await fulfilmentCallBackService.SendCallBackResponse(salesCatalogueProductResponse, scsResponseQueueMessage);

            Assert.IsFalse(response);
        }

        [Test]
        public async Task WhenEmptyCallBackUriInRequest_ThenSendCallBackResponseReturnsFalse()
        {
            scsResponseQueueMessage.CallbackUri = "";

            var response = await fulfilmentCallBackService.SendCallBackResponse(salesCatalogueProductResponse, scsResponseQueueMessage);

            Assert.IsFalse(response);
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
               });

            var response = await fulfilmentCallBackService.SendCallBackResponse(salesCatalogueProductResponse, scsResponseQueueMessage);

            Assert.IsTrue(response);
        }

        [Test]
        public async Task WhenIncorrectCallBackPayloadErrorInRequest_ThenCallBackApiIsNotCalled()
        {
            salesCatalogueProductResponse.ProductCounts.RequestedProductCount = 0;

            A.CallTo(() => fakeCallBackClient.CallBackApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .Invokes((HttpMethod method, string postBody, string uri) =>
               {
                   uriParam = uri;
                   httpMethodParam = method;
                   postBodyParam = postBody;
               });

            var response = await fulfilmentCallBackService.SendCallBackErrorResponse(salesCatalogueProductResponse, scsResponseQueueMessage);

            Assert.IsFalse(response);
        }

        [Test]
        public async Task WhenEmptyCallBackUriInRequest_ThenSendCallBackErrorResponseReturnsFalse()
        {
            scsResponseQueueMessage.CallbackUri = "";

            var response = await fulfilmentCallBackService.SendCallBackErrorResponse(salesCatalogueProductResponse, scsResponseQueueMessage);

            Assert.IsFalse(response);
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

            Assert.IsTrue(response);
        }
    }
}