using AutoMapper;
using FakeItEasy;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Configuration;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.API.UnitTests.Services
{
    [TestFixture]
    public class ProductDataServiceTests
    {
        private IProductIdentifierValidator fakeProductIdentifierValidator;
        private IProductDataProductVersionsValidator fakeProductVersionValidator;
        private IProductDataSinceDateTimeValidator fakeProductDataSinceDateTimeValidator;
        private ProductDataService service;
        private ISalesCatalogueService fakeSalesCatalogueService;
        private IFileShareService fakeFileShareService;
        private ILogger<ProductDataService> logger;
        private IMapper fakeMapper;
        private IExchangeSetStorageProvider fakeExchangeSetStorageProvider;
        private IOptions<EssFulfilmentStorageConfiguration> fakeEssFulfilmentStorageConfig;
        private IMonitorHelper fakeMonitorHelper;
        private UserIdentifier fakeUserIdentifier;
        private IAzureAdB2CHelper fakeAzureAdB2CHelper;
        private IOptions<AioConfiguration> fakeAioConfiguration;

        [SetUp]
        public void Setup()
        {
            fakeProductIdentifierValidator = A.Fake<IProductIdentifierValidator>();
            fakeProductVersionValidator = A.Fake<IProductDataProductVersionsValidator>();
            fakeProductDataSinceDateTimeValidator = A.Fake<IProductDataSinceDateTimeValidator>();
            fakeSalesCatalogueService = A.Fake<ISalesCatalogueService>();
            fakeMapper = A.Fake<IMapper>();
            fakeFileShareService = A.Fake<IFileShareService>();
            logger = A.Fake<ILogger<ProductDataService>>();
            fakeExchangeSetStorageProvider = A.Fake<ExchangeSetStorageProvider>();
            fakeEssFulfilmentStorageConfig = A.Fake<IOptions<EssFulfilmentStorageConfiguration>>();
            fakeMonitorHelper = A.Fake<IMonitorHelper>();
            fakeUserIdentifier = A.Fake<UserIdentifier>();
            fakeAzureAdB2CHelper = A.Fake<IAzureAdB2CHelper>();
            fakeAioConfiguration = A.Fake<IOptions<AioConfiguration>>();
            fakeEssFulfilmentStorageConfig.Value.LargeExchangeSetSizeInMB = 300;

            service = new ProductDataService(fakeProductIdentifierValidator, fakeProductVersionValidator, fakeProductDataSinceDateTimeValidator,
                fakeSalesCatalogueService, fakeMapper, fakeFileShareService, logger, fakeExchangeSetStorageProvider
            , fakeEssFulfilmentStorageConfig, fakeMonitorHelper, fakeUserIdentifier, fakeAzureAdB2CHelper, fakeAioConfiguration);
        }

        #region GetExchangeSetResponse

        private ExchangeSetResponse GetExchangeSetResponse()
        {
            bool isAioEnabled = fakeAioConfiguration.Value.IsAioEnabled;

            LinkSetBatchStatusUri linkSetBatchStatusUri = new LinkSetBatchStatusUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/status"
            };
            LinkSetBatchDetailsUri linkSetBatchDetailsUri = new LinkSetBatchDetailsUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
            };
            LinkSetFileUri linkSetFileUri = new LinkSetFileUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip",
            };
            LinkSetFileUri AiolinkSetFileUri = new LinkSetFileUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/aio123.zip",
            };
            Common.Models.Response.Links links = new Common.Models.Response.Links()
            {
                ExchangeSetBatchStatusUri = linkSetBatchStatusUri,
                ExchangeSetBatchDetailsUri = linkSetBatchDetailsUri,
                ExchangeSetFileUri = linkSetFileUri,
                AioExchangeSetFileUri = isAioEnabled ? AiolinkSetFileUri : null
            };
            List<RequestedProductsNotInExchangeSet> lstRequestedProductsNotInExchangeSet = new List<RequestedProductsNotInExchangeSet>()
            {
                new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "GB123456",
                    Reason = "productWithdrawn"
                },
                new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "GB123789",
                    Reason = "invalidProduct"
                }
            };
            ExchangeSetResponse exchangeSetResponse = new()
            {
                Links = links,
                ExchangeSetUrlExpiryDateTime = Convert.ToDateTime("2021-02-17T16:19:32.269Z").ToUniversalTime(),
                RequestedProductsAlreadyUpToDateCount = 0,
                RequestedProductsNotInExchangeSet = lstRequestedProductsNotInExchangeSet,
                ExchangeSetCellCount = 1,
                RequestedProductCount = 3
            };

            if (isAioEnabled)
            {
                exchangeSetResponse.RequestedAioProductCount = 1;
                exchangeSetResponse.AioExchangeSetCellCount = 1;
                exchangeSetResponse.RequestedAioProductsAlreadyUpToDateCount = 0;
            }

            return exchangeSetResponse;
        }

        private ExchangeSetResponse GetExchangeSetResponseAioToggleOff()
        {
            LinkSetBatchStatusUri linkSetBatchStatusUri = new LinkSetBatchStatusUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/status"
            };
            LinkSetBatchDetailsUri linkSetBatchDetailsUri = new LinkSetBatchDetailsUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
            };
            LinkSetFileUri linkSetFileUri = new LinkSetFileUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip",
            };
            Common.Models.Response.Links links = new Common.Models.Response.Links()
            {
                ExchangeSetBatchStatusUri = linkSetBatchStatusUri,
                ExchangeSetBatchDetailsUri = linkSetBatchDetailsUri,
                ExchangeSetFileUri = linkSetFileUri,
            };
            List<RequestedProductsNotInExchangeSet> lstRequestedProductsNotInExchangeSet = new()
            {   new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "GB123456",
                    Reason = "productWithdrawn"
                },
                new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "GB123789",
                    Reason = "invalidProduct"
                },
                new RequestedProductsNotInExchangeSet()
                {
                     ProductName = "US2ARCGD",
                     Reason = "invalidProduct"
                }
            };
            ExchangeSetResponse exchangeSetResponse = new()
            {
                Links = links,
                ExchangeSetUrlExpiryDateTime = Convert.ToDateTime("2021-02-17T16:19:32.269Z").ToUniversalTime(),
                RequestedProductsAlreadyUpToDateCount = 0,
                RequestedProductsNotInExchangeSet = lstRequestedProductsNotInExchangeSet,
                RequestedProductCount = 3,
                ExchangeSetCellCount = 1,
                BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
            };

            return exchangeSetResponse;
        }

        private ExchangeSetResponse GetExchangeSetResponseAioToggleON()
        {
            LinkSetBatchStatusUri linkSetBatchStatusUri = new LinkSetBatchStatusUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/status"
            };
            LinkSetBatchDetailsUri linkSetBatchDetailsUri = new LinkSetBatchDetailsUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
            };
            LinkSetFileUri linkSetFileUri = new LinkSetFileUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip",
            };
            Common.Models.Response.Links links = new Common.Models.Response.Links()
            {
                ExchangeSetBatchStatusUri = linkSetBatchStatusUri,
                ExchangeSetBatchDetailsUri = linkSetBatchDetailsUri,
                ExchangeSetFileUri = linkSetFileUri,
            };
            List<RequestedProductsNotInExchangeSet> lstRequestedProductsNotInExchangeSet = new()
            {   new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "GB123456",
                    Reason = "productWithdrawn"
                },
                new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "GB123789",
                    Reason = "invalidProduct"
                }
            };
            ExchangeSetResponse exchangeSetResponse = new()
            {
                Links = links,
                ExchangeSetUrlExpiryDateTime = Convert.ToDateTime("2021-02-17T16:19:32.269Z").ToUniversalTime(),
                RequestedProductsAlreadyUpToDateCount = 0,
                RequestedProductsNotInExchangeSet = lstRequestedProductsNotInExchangeSet,
                RequestedProductCount = 4,
                ExchangeSetCellCount = 1,
                BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272",
                RequestedAioProductCount = 1,
                AioExchangeSetCellCount = 1,
                RequestedAioProductsAlreadyUpToDateCount = 0
            };

            return exchangeSetResponse;
        }

        #endregion GetExchangeSetResponse

        #region GetSalesCatalogueResponse

        private SalesCatalogueResponse GetSalesCatalogueResponse()
        {
            return new SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody = new SalesCatalogueProductResponse
                {
                    ProductCounts = new ProductCounts
                    {
                        RequestedProductCount = 6,
                        RequestedProductsAlreadyUpToDateCount = 8,
                        ReturnedProductCount = 2,
                        RequestedProductsNotReturned = new List<RequestedProductsNotReturned> {
                            new RequestedProductsNotReturned { ProductName = "GB123456", Reason = "productWithdrawn" },
                            new RequestedProductsNotReturned { ProductName = "GB123789", Reason = "invalidProduct" }
                        }
                    },
                    Products = new List<Products> {
                        new Products {
                            ProductName = "productName",
                            EditionNumber = 2,
                            UpdateNumbers = new List<int?> { 3, 4 },
                            Cancellation = new Cancellation {
                                EditionNumber = 4,
                                UpdateNumber = 6
                            },
                            FileSize = 400
                        }
                    }
                },
                ScsRequestDateTime = DateTime.UtcNow
            };
        }
        private SalesCatalogueResponse GetSalesCatalogueFileSizeResponse()
        {
            return new SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody = new SalesCatalogueProductResponse
                {
                    ProductCounts = new ProductCounts
                    {
                        RequestedProductCount = 6,
                        RequestedProductsAlreadyUpToDateCount = 8,
                        ReturnedProductCount = 2,
                        RequestedProductsNotReturned = new List<RequestedProductsNotReturned> {
                                new RequestedProductsNotReturned { ProductName = "GB123456", Reason = "productWithdrawn" },
                                new RequestedProductsNotReturned { ProductName = "GB123789", Reason = "invalidProduct" }
                            }
                    },
                    Products = new List<Products> {
                            new Products {
                                ProductName = "productName",
                                EditionNumber = 2,
                                UpdateNumbers = new List<int?> { 3, 4 },
                                Cancellation = new Cancellation {
                                    EditionNumber = 4,
                                    UpdateNumber = 6
                                },
                                FileSize = 500000000
                            }
                        }
                },
                ScsRequestDateTime = DateTime.UtcNow
            };
        }
        #endregion

        #region AzureADB2CToken
        private AzureAdB2C GetAzureADToken()
        {
            return new AzureAdB2C()
            {
                AudToken = string.Empty,
                IssToken = string.Empty
            };
        }
        private AzureAdB2C GetAzureB2CToken()
        {
            return new AzureAdB2C()
            {
                AudToken = "9bca10f0-20d9-4b38-88eb-c7aff6b5f571",
                IssToken = "https://gk.microsoft.com/9b29766b-896f-46df-8f1a-122d7c822d91/v2.0/"
            };
        }

        private AzureAdB2C GetAzureAdB2CToken()
        {
            return new AzureAdB2C()
            {
                AudToken = "9bca10f0-20d9-4b38-88eb-c7aff6b5f571",
                IssToken = "https://www.microsoft.com/9b29766b-896f-46df-8f1a-122d7c822d91/v2.0"
            };
        }
        #endregion AzureB2CToken

        #region CreateBatchResponse
        private static CreateBatchResponse CreateBatchResponse()
        {
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            return new CreateBatchResponse()
            {
                ResponseBody = new CreateBatchResponseModel()
                {
                    BatchId = batchId,
                    BatchStatusUri = $"http://fss.ukho.gov.uk/batch/{batchId}/status",
                    ExchangeSetBatchDetailsUri = $"http://fss.ukho.gov.uk/batch/{batchId}",
                    ExchangeSetFileUri = $"http://fss.ukho.gov.uk/batch/{batchId}/files/exchangeset123.zip",
                    AioExchangeSetFileUri = $"http://fss.ukho.gov.uk/batch/{batchId}/files/aio123.zip",
                    BatchExpiryDateTime = "2021-02-17T16:19:32.269Z"
                },
                ResponseCode = HttpStatusCode.Created
            };
        }
        #endregion CreateBatchResponse

        #region ProductIdentifiers

        [Test]
        public async Task WhenInvalidProductIdentifierRequest_ThenValidateProductDataByProductIdentifiersReturnsBadrequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("ProductIdentifiers", "Product Identifiers cannot be blank or null.")}));

            var result = await service.ValidateProductDataByProductIdentifiers(new ProductIdentifierRequest());

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Product Identifiers cannot be blank or null.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenInvalidNullProductIdentifierRequest_ThenValidateProductDataByProductIdentifiersReturnsBadrequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("RequestBody", "Either body is null or malformed.")}));

            var result = await service.ValidateProductDataByProductIdentifiers(null);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Either body is null or malformed.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenValidProductIdentifierRequest_ThenValidateProductDataByProductIdentifierReturnsOkrequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;

            var result = await service.ValidateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri
                });

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task WhenLargeExchangeSetRequestedWithB2CToken_ThenCreateProductDataByProductIdentifierReturnsBadRequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;
            var salesCatalogueResponse = GetSalesCatalogueFileSizeResponse();
            var azureB2CToken = GetAzureB2CToken();

            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri
                }, azureB2CToken); //B2C Token with file Size less than 300 mb

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.HttpStatusCode);

        }

        [Test]
        public async Task WhenLargeExchangeSetRequestedWithADB2CToken_ThenCreateProductDataByProductIdentifierReturnsBadRequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;
            var salesCatalogueResponse = GetSalesCatalogueFileSizeResponse();
            var azureAdB2CToken = GetAzureAdB2CToken();

            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri
                }, azureAdB2CToken); //AdB2C token with file size large than 300 mb

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.HttpStatusCode);
        }

        [Test]
        public async Task WhenValidProductIdentifierRequest_ThenCreateProductDataByProductIdentifierReturnsOkrequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            var azureAdToken = GetAzureADToken();

            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);

            var exchangeSetResponse = GetExchangeSetResponse();
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;
            string callBackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";

            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);
            A.CallTo(() => fakeExchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, CreateBatchResponseModel.ResponseBody.BatchId, callBackUri, correlationId, A<string>.Ignored, salesCatalogueResponse.ScsRequestDateTime)).Returns(true);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri
                }, azureAdToken);

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();

            Assert.AreEqual(HttpStatusCode.Created, result.HttpStatusCode);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, result.ExchangeSetResponse.ExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductCount, result.ExchangeSetResponse.RequestedProductCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchStatusUri.Href, result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchDetailsUri.Href, result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.Links.ExchangeSetFileUri.Href, result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime, result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.BatchId, result.BatchId);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS API : AIO toggle is OFF for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidProductIdentifierRequest_ThenCreateProductDataByProductIdentifierReturnsOkrequestWithLastModified()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            var azureB2CToken = GetAzureB2CToken();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            salesCatalogueResponse.LastModified = DateTime.Now.AddDays(-4);
            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            var exchangeSetResponse = GetExchangeSetResponse();
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;

            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri
                }, azureB2CToken); // AzureB2C Token but file size is less than 300 Mb

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();

            Assert.AreEqual(HttpStatusCode.Created, result.HttpStatusCode);
            Assert.NotNull(result.LastModified);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, result.ExchangeSetResponse.ExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductCount, result.ExchangeSetResponse.RequestedProductCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.BatchId, result.BatchId);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS API : AIO toggle is OFF for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidProductIdentifierRequest_ThenCreateProductDataByProductIdentifierReturnsInternalServerError()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            var azureAdB2CToken = GetAzureAdB2CToken();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.BadRequest;
            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            var exchangeSetResponse = GetExchangeSetResponse();
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri
                }, azureAdB2CToken);//azure Ad B2C token when file size is less than 300 Mb

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.InternalServerError, result.HttpStatusCode);
        }

        [Test]
        public async Task WhenInvalidFSSCreateBatchProductIdentifierRequest_ThenCreateProductDataByProductIdentifierReturnsInternalServerError()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;

            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.InternalServerError;

            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var exchangeSetResponse = GetExchangeSetResponse();
            var azureAdToken = GetAzureADToken();
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);

            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.InternalServerError;

            A.CallTo(() => fakeFileShareService.CreateBatch(string.Empty, string.Empty)).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri
                }, azureAdToken);

            Assert.IsNull(result.ExchangeSetResponse);
            Assert.AreEqual(HttpStatusCode.InternalServerError, result.HttpStatusCode);
        }

        [Test]
        public async Task WhenValidProductIdentifierRequest_And_AIOToggleIsOff_ThenCreateProductDataByProductIdentifierReturnsOkrequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550", "US2ARCGD" };
            fakeAioConfiguration.Value.IsAioEnabled = false;
            fakeAioConfiguration.Value.AioCells = "US2ARCGD";
            string callbackUri = string.Empty;
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            var azureAdToken = GetAzureADToken();

            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);

            var exchangeSetResponse = GetExchangeSetResponse();
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;
            string callBackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";

            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);
            A.CallTo(() => fakeExchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, CreateBatchResponseModel.ResponseBody.BatchId, callBackUri, correlationId, A<string>.Ignored, salesCatalogueResponse.ScsRequestDateTime)).Returns(true);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri
                }, azureAdToken);

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();
            exchangeSetResponseAioToggleOff.RequestedProductCount += 1; //one aio cell passed

            Assert.AreEqual(HttpStatusCode.Created, result.HttpStatusCode);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, result.ExchangeSetResponse.ExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductCount, result.ExchangeSetResponse.RequestedProductCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchStatusUri.Href, result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchDetailsUri.Href, result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.Links.ExchangeSetFileUri.Href, result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime, result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.BatchId, result.BatchId);
            //Aio cell details
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductsNotInExchangeSet.Count, result.ExchangeSetResponse.RequestedProductsNotInExchangeSet.Count);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS API : AIO toggle is OFF for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidProductIdentifierRequest_And_AIOToggleIsOn_ThenCreateProductDataByProductIdentifierReturnsOkrequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550", "US2ARCGD" };
            fakeAioConfiguration.Value.IsAioEnabled = true;
            fakeAioConfiguration.Value.AioCells = "US2ARCGD";
            string callbackUri = string.Empty;
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            var azureAdToken = GetAzureADToken();

            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);

            var exchangeSetResponse = GetExchangeSetResponse();
            exchangeSetResponse.RequestedProductCount += 1; //one aio cell passed

            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;
            string callBackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";

            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);
            A.CallTo(() => fakeExchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, CreateBatchResponseModel.ResponseBody.BatchId, callBackUri, correlationId, A<string>.Ignored, salesCatalogueResponse.ScsRequestDateTime)).Returns(true);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri
                }, azureAdToken);

            var exchangeSetResponseAioToggleOn = GetExchangeSetResponseAioToggleON();

            Assert.AreEqual(HttpStatusCode.Created, result.HttpStatusCode);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.ExchangeSetCellCount, result.ExchangeSetResponse.ExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.RequestedProductCount, result.ExchangeSetResponse.RequestedProductCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.RequestedProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.Links.ExchangeSetBatchStatusUri.Href, result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.Links.ExchangeSetBatchDetailsUri.Href, result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.Links.ExchangeSetFileUri.Href, result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.ExchangeSetUrlExpiryDateTime, result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.BatchId, result.BatchId);
            //Aio cell details
            Assert.AreEqual(exchangeSetResponseAioToggleOn.AioExchangeSetCellCount, result.ExchangeSetResponse.AioExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.RequestedAioProductCount, result.ExchangeSetResponse.RequestedAioProductCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.RequestedAioProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedAioProductsAlreadyUpToDateCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.RequestedProductsNotInExchangeSet.Count, result.ExchangeSetResponse.RequestedProductsNotInExchangeSet.Count);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOn.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS API : AIO toggle is ON for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
        }

        #endregion

        #region ProductVersions

        [Test]
        public async Task WhenInvalidProductVersionRequest_ThenValidateProductDataByProductVersionsReturnsBadrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("productName", "productName cannot be blank or null.")}));

            var result = await service.ValidateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            { ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest() { ProductName = null } } });

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("productName cannot be blank or null.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenInvalidNullProductVersionRequest_ThenValidateProductDataByProductVersionsReturnsBadrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("RequestBody", "Either body is null or malformed.")}));

            var result = await service.ValidateProductDataByProductVersions(null);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Either body is null or malformed.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenValidProductVersionRequest_ThenValidateProductDataByProductVersionsReturnsOkrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            var result = await service.ValidateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            { ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest() { ProductName = "Demo", EditionNumber = 5, UpdateNumber = 0 } } });

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task WhenLargeExchangeSetRequestedWithB2CToken_ThenCreateProductDataByProductVersionsReturnsBadRequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
               .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueFileSizeResponse();
            var azureB2CToken = GetAzureB2CToken();
            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);

            var result = await service.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            {
                ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest {
                ProductName = "GB123789", EditionNumber = 6, UpdateNumber = 3 } },
                CallbackUri = ""
            }, azureB2CToken);//valid AzureAdB2c Token , but filesize is large than 300 mb

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.HttpStatusCode);
        }

        [Test]
        public async Task WhenLargeExchangeSetRequestedWithADB2CToken_ThenCreateProductDataByProductVersionsReturnsBadRequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueFileSizeResponse();
            var azureAdB2CToken = GetAzureAdB2CToken();
            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);

            var result = await service.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            {
                ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest {
                ProductName = "GB123789", EditionNumber = 6, UpdateNumber = 3 } },
                CallbackUri = ""
            }, azureAdB2CToken);//valid AzureAdB2c Token , but filesize is large than 300 mb

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.HttpStatusCode);
        }

        [Test]
        public async Task WhenValidProductVersionRequest_ThenCreateProductDataByProductVersionsReturnsOkrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            var azureAdToken = GetAzureADToken();

            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var exchangeSetResponse = GetExchangeSetResponse();
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;

            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            {
                ProductVersions = new List<ProductVersionRequest>() {
                    new ProductVersionRequest {
                            ProductName = "GB123456", EditionNumber = 6, UpdateNumber = 3
                    },new ProductVersionRequest {
                            ProductName = "GB160060", EditionNumber = 2, UpdateNumber = 4
                    },new ProductVersionRequest {
                            ProductName = "AU334550", EditionNumber = 8, UpdateNumber = 1
                    }
                },
                CallbackUri = ""
            }, azureAdToken);

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();

            Assert.AreEqual(HttpStatusCode.Created, result.HttpStatusCode);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, result.ExchangeSetResponse.ExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductCount, result.ExchangeSetResponse.RequestedProductCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.BatchId, result.BatchId);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS API : AIO toggle is OFF for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidProductVersionRequest_ThenCreateProductDataByProductVersionsReturnsNotModifiedToOkrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            var azureB2CToken = GetAzureB2CToken();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.NotModified;
            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;

            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);

            var result = await service.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            {
                ProductVersions = new List<ProductVersionRequest>() {
                    new ProductVersionRequest {
                            ProductName = "GB123456", EditionNumber = 6, UpdateNumber = 3
                    },new ProductVersionRequest {
                            ProductName = "GB160060", EditionNumber = 2, UpdateNumber = 4
                    },new ProductVersionRequest {
                            ProductName = "AU334550", EditionNumber = 8, UpdateNumber = 1
                    }
                },
                CallbackUri = ""
            }, azureB2CToken);// azureB2C Token with file size less than 300 Mb

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();
            exchangeSetResponseAioToggleOff.ExchangeSetCellCount = 0; //RequestedProductsAlreadyUpToDateCount
            exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount = 3;//RequestedProductsAlreadyUpToDateCount

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.Created, result.HttpStatusCode);
            Assert.Null(result.LastModified);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, result.ExchangeSetResponse.ExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductCount, result.ExchangeSetResponse.RequestedProductCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime, result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.BatchId, result.BatchId);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS API : AIO toggle is OFF for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidProductVersionRequest_ThenCreateProductDataByProductVersionsReturnsNotModifiedToOkrequestWithLastModified()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            var salesCatalogueResponse = GetSalesCatalogueResponse();
            var azureAdB2CToken = GetAzureAdB2CToken();

            salesCatalogueResponse.ResponseCode = HttpStatusCode.NotModified;
            salesCatalogueResponse.LastModified = DateTime.Now.AddDays(-2);
            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;

            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);

            var result = await service.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            {
                ProductVersions = new List<ProductVersionRequest>() {
                    new ProductVersionRequest {
                            ProductName = "GB123456", EditionNumber = 6, UpdateNumber = 3
                    },new ProductVersionRequest {
                            ProductName = "GB160060", EditionNumber = 2, UpdateNumber = 4
                    },new ProductVersionRequest {
                            ProductName = "AU334550", EditionNumber = 8, UpdateNumber = 1
                    }
                },
                CallbackUri = ""
            }, azureAdB2CToken); // Azure Ad B2C Token but file size less than 300 mB

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();
            exchangeSetResponseAioToggleOff.ExchangeSetCellCount = 0; //RequestedProductsAlreadyUpToDateCount
            exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount = 3;//RequestedProductsAlreadyUpToDateCount

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.Created, result.HttpStatusCode);
            Assert.NotNull(result.LastModified);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, result.ExchangeSetResponse.ExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductCount, result.ExchangeSetResponse.RequestedProductCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime, result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.BatchId, result.BatchId);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS API : AIO toggle is OFF for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidProductVersionRequest_ThenCreateProductDataByProductVersionsReturnsNotModifiedToBadRequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            var azureADToken = GetAzureADToken();

            salesCatalogueResponse.ResponseCode = HttpStatusCode.BadRequest;
            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);

            var result = await service.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            {
                ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest {
                ProductName = "GB123789", EditionNumber = 6, UpdateNumber = 3 } },
                CallbackUri = ""
            }, azureADToken);

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.InternalServerError, result.HttpStatusCode);
            Assert.Null(result.LastModified);
        }

        [Test]
        public async Task WhenInvalidFSSCreateBatchProductVersionRequest_ThenCreateProductDataByProductVersionsReturnsInternalServerError()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.InternalServerError;

            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var exchangeSetResponse = GetExchangeSetResponse();
            var azureAdToken = GetAzureADToken();

            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.InternalServerError;

            A.CallTo(() => fakeFileShareService.CreateBatch(string.Empty, string.Empty)).Returns(CreateBatchResponseModel);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);

            var result = await service.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            {
                ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest {
                ProductName = "GB123789", EditionNumber = 6, UpdateNumber = 3 } },
                CallbackUri = ""
            }, azureAdToken);

            Assert.IsNull(result.ExchangeSetResponse);
            Assert.AreEqual(HttpStatusCode.InternalServerError, result.HttpStatusCode);
        }

        [Test]
        public async Task WhenValidProductVersionRequest_And_AIOToggleIsOff_ThenCreateProductDataByProductVersionsReturnsOkrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            var azureAdToken = GetAzureADToken();
            fakeAioConfiguration.Value.IsAioEnabled = false;
            fakeAioConfiguration.Value.AioCells = "US2ARCGD";
            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var exchangeSetResponse = GetExchangeSetResponse();
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;

            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            {
                ProductVersions = new List<ProductVersionRequest>() {
                    new ProductVersionRequest {
                            ProductName = "GB123456", EditionNumber = 6, UpdateNumber = 3
                    },new ProductVersionRequest {
                            ProductName = "GB160060", EditionNumber = 2, UpdateNumber = 4
                    },new ProductVersionRequest {
                            ProductName = "AU334550", EditionNumber = 8, UpdateNumber = 1
                    },
                    new ProductVersionRequest {
                            ProductName = "US2ARCGD", EditionNumber = 4, UpdateNumber = 6
                    }
                },
                CallbackUri = ""
            }, azureAdToken);

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();
            exchangeSetResponseAioToggleOff.RequestedProductCount += 1; //one aio cell passed

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.Created, result.HttpStatusCode);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, result.ExchangeSetResponse.ExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductCount, result.ExchangeSetResponse.RequestedProductCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchStatusUri.Href, result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchDetailsUri.Href, result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.Links.ExchangeSetFileUri.Href, result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime, result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.BatchId, result.BatchId);
            //Aio cell details
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductsNotInExchangeSet.Count, result.ExchangeSetResponse.RequestedProductsNotInExchangeSet.Count);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS API : AIO toggle is OFF for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidProductVersionRequest_And_AIOToggleIsON_ThenCreateProductDataByProductVersionsReturnsOkrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            var azureAdToken = GetAzureADToken();
            fakeAioConfiguration.Value.IsAioEnabled = true;
            fakeAioConfiguration.Value.AioCells = "US2ARCGD";
            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var exchangeSetResponse = GetExchangeSetResponse();
            exchangeSetResponse.RequestedProductCount += 1; //one aio cell passed

            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;

            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            {
                ProductVersions = new List<ProductVersionRequest>() {
                    new ProductVersionRequest {
                            ProductName = "GB123456", EditionNumber = 6, UpdateNumber = 3
                    },new ProductVersionRequest {
                            ProductName = "GB160060", EditionNumber = 2, UpdateNumber = 4
                    },new ProductVersionRequest {
                            ProductName = "AU334550", EditionNumber = 8, UpdateNumber = 1
                    },
                    new ProductVersionRequest {
                            ProductName = "US2ARCGD", EditionNumber = 4, UpdateNumber = 6
                    }
                },
                CallbackUri = ""
            }, azureAdToken);

            var exchangeSetResponseAioToggleOn = GetExchangeSetResponseAioToggleON();

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.Created, result.HttpStatusCode);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.ExchangeSetCellCount, result.ExchangeSetResponse.ExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.RequestedProductCount, result.ExchangeSetResponse.RequestedProductCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.RequestedProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.Links.ExchangeSetBatchStatusUri.Href, result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.Links.ExchangeSetBatchDetailsUri.Href, result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.Links.ExchangeSetFileUri.Href, result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.ExchangeSetUrlExpiryDateTime, result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.BatchId, result.BatchId);
            //Aio cell details
            Assert.AreEqual(exchangeSetResponseAioToggleOn.AioExchangeSetCellCount, result.ExchangeSetResponse.AioExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.RequestedAioProductCount, result.ExchangeSetResponse.RequestedAioProductCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.RequestedAioProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedAioProductsAlreadyUpToDateCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.RequestedProductsNotInExchangeSet.Count, result.ExchangeSetResponse.RequestedProductsNotInExchangeSet.Count);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOn.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS API : AIO toggle is ON for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
        }

        #endregion

        #region ProductDataSinceDateTime

        [Test]
        public async Task WhenInvalidSinceDateTimeInRequest_ThenValidateProductDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                            { new ValidationFailure("SinceDateTime", "Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format'.")}));

            var result = await service.ValidateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format'.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenSinceDateTimeFormatIsGreaterThanCurrrentDateTimeInRequest_ThenValidateProductDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                            { new ValidationFailure("SinceDateTime", "Provided sinceDateTime cannot be a future date.")}));

            var result = await service.ValidateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Provided sinceDateTime cannot be a future date.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenCallbackUrlParameterNotValidInRequest_ThenValidateProductDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                            { new ValidationFailure("CallbackUri", "Invalid callbackUri format.")}));

            var result = await service.ValidateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Invalid callbackUri format.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenValidateProductDataSinceDateTimeInRequest_ThenCreateProductDataSinceDateTimeReturnSuccess()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest(), GetAzureADToken());

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
        }

        [Test]
        public async Task WhenValidateProductDataSinceDateTimeInRequest_ThenCreateProductDataSinceDateTimeReturnNotModified()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.NotModified;
            salesCatalogueResponse.LastModified = DateTime.UtcNow;
            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest(), GetAzureADToken());

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
        }

        [Test]
        public async Task WhenLargeExchangeSetRequestedWithB2CToken_ThenCreateProductDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueFileSizeResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            salesCatalogueResponse.LastModified = DateTime.UtcNow;

            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest(), GetAzureB2CToken());// B2C token passed and file size large than 300 mb

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.HttpStatusCode);
        }

        [Test]
        public async Task WhenLargeExchangeSetRequestedWithADB2CToken_ThenCreateProductDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
               .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueFileSizeResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            salesCatalogueResponse.LastModified = DateTime.UtcNow;

            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest(), GetAzureAdB2CToken());//AdB2C token passed and file size large than 300 mb

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.HttpStatusCode);
        }

        [Test]
        public async Task WhenValidateProductDataSinceDateTimeInRequest_ThenCreateProductDataSinceDateTimeReturnOk()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            salesCatalogueResponse.LastModified = DateTime.UtcNow;
            var exchangeSetResponse = GetExchangeSetResponse();
            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;

            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);
            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest(), GetAzureB2CToken());//B2C token passed and file size less than 300 mb

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.Created, result.HttpStatusCode);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, result.ExchangeSetResponse.ExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductCount, result.ExchangeSetResponse.RequestedProductCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchStatusUri.Href, result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchDetailsUri.Href, result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.Links.ExchangeSetFileUri.Href, result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime, result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.BatchId, result.BatchId);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS API : AIO toggle is OFF for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenInvalidFSSCreateBatchProductDataSinceDateTimeInRequest_ThenCreateProductDataSinceDateTimeReturnInternalServerError()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.InternalServerError;
            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.InternalServerError;

            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest(), GetAzureAdB2CToken());// ADB2C Token with File size less than 300 mb

            Assert.IsNull(result.ExchangeSetResponse);
            Assert.AreEqual(HttpStatusCode.InternalServerError, result.HttpStatusCode);
        }

        [Test]
        public async Task WhenValidateProductDataSinceDateTimeInRequest_And_AIOToggleIsOff_ThenCreateProductDataSinceDateTimeReturnOk()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            fakeAioConfiguration.Value.IsAioEnabled = false;
            fakeAioConfiguration.Value.AioCells = "US2ARCGD";
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            salesCatalogueResponse.LastModified = DateTime.UtcNow;
            var exchangeSetResponse = GetExchangeSetResponse();
            exchangeSetResponse.RequestedProductsNotInExchangeSet.Add(new RequestedProductsNotInExchangeSet
            {
                ProductName = "US2ARCGD",
                Reason = "InvalidProduct"
            });
            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;

            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);
            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest(), GetAzureB2CToken());//B2C token passed and file size less than 300 mb

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();
            exchangeSetResponseAioToggleOff.RequestedProductCount = 3;

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.Created, result.HttpStatusCode);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, result.ExchangeSetResponse.ExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductCount, result.ExchangeSetResponse.RequestedProductCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchStatusUri.Href, result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchDetailsUri.Href, result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.Links.ExchangeSetFileUri.Href, result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime, result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.BatchId, result.BatchId);
            Assert.AreEqual(exchangeSetResponseAioToggleOff.RequestedProductsNotInExchangeSet.Count, result.ExchangeSetResponse.RequestedProductsNotInExchangeSet.Count);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS API : AIO toggle is OFF for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidateProductDataSinceDateTimeInRequest_And_AIOToggleIsON_ThenCreateProductDataSinceDateTimeReturnOk()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            fakeAioConfiguration.Value.IsAioEnabled = true;
            fakeAioConfiguration.Value.AioCells = "US2ARCGD";
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            salesCatalogueResponse.LastModified = DateTime.UtcNow;
            salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductsNotReturned = new List<RequestedProductsNotReturned>();
            salesCatalogueResponse.ResponseBody.Products.Add(new Products
            {
                ProductName = "US2ARCGD",
                EditionNumber = 2,
                UpdateNumbers = new List<int?> { 3, 4 },
                Cancellation = new Cancellation
                {
                    EditionNumber = 4,
                    UpdateNumber = 6
                },
                FileSize = 400
            });
            var exchangeSetResponse = GetExchangeSetResponse();
            exchangeSetResponse.RequestedProductCount = 0;
            exchangeSetResponse.RequestedProductsNotInExchangeSet = new List<RequestedProductsNotInExchangeSet>();
            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;

            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);
            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest(), GetAzureB2CToken());//B2C token passed and file size less than 300 mb

            var exchangeSetResponseAioToggleOn = GetExchangeSetResponseAioToggleON();
            exchangeSetResponseAioToggleOn.RequestedProductCount = 0;
            exchangeSetResponseAioToggleOn.ExchangeSetCellCount = 0;
            exchangeSetResponseAioToggleOn.RequestedProductsNotInExchangeSet = new List<RequestedProductsNotInExchangeSet>();

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.Created, result.HttpStatusCode);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.ExchangeSetCellCount, result.ExchangeSetResponse.ExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.RequestedProductCount, result.ExchangeSetResponse.RequestedProductCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.RequestedProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.Links.ExchangeSetBatchStatusUri.Href, result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.Links.ExchangeSetBatchDetailsUri.Href, result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.Links.ExchangeSetFileUri.Href, result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.ExchangeSetUrlExpiryDateTime, result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.BatchId, result.BatchId);
            //Aio cell details
            Assert.AreEqual(exchangeSetResponseAioToggleOn.AioExchangeSetCellCount, result.ExchangeSetResponse.AioExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.RequestedAioProductCount, result.ExchangeSetResponse.RequestedAioProductCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.RequestedAioProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedAioProductsAlreadyUpToDateCount);
            Assert.AreEqual(exchangeSetResponseAioToggleOn.RequestedProductsNotInExchangeSet.Count, result.ExchangeSetResponse.RequestedProductsNotInExchangeSet.Count);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOn.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS API : AIO toggle is ON for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
        }

        #endregion ProductDataSinceDateTime       
    }
}