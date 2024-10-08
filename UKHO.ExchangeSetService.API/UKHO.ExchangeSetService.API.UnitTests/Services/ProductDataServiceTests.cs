using AutoMapper;
using FakeItEasy;
using FluentAssertions;
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
using UKHO.ExchangeSetService.Common.Models.Enums;
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
        private IScsProductIdentifierValidator fakeScsProductIdentifierValidator;
        private IScsDataSinceDateTimeValidator fakeScsDataSinceDateTimeValidator;

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
            fakeEssFulfilmentStorageConfig.Value.S57ExchangeSetSizeInMB = 700;
            fakeScsProductIdentifierValidator = A.Fake<IScsProductIdentifierValidator>();
            fakeScsDataSinceDateTimeValidator = A.Fake<IScsDataSinceDateTimeValidator>();

            service = new ProductDataService(fakeProductIdentifierValidator, fakeProductVersionValidator, fakeScsProductIdentifierValidator, fakeProductDataSinceDateTimeValidator,
                fakeSalesCatalogueService, fakeMapper, fakeFileShareService, logger, fakeExchangeSetStorageProvider
            , fakeEssFulfilmentStorageConfig, fakeMonitorHelper, fakeUserIdentifier, fakeAzureAdB2CHelper, fakeAioConfiguration, fakeScsDataSinceDateTimeValidator);
        }

        #region GetExchangeSetResponse

        private ExchangeSetResponse GetExchangeSetResponse()
        {
            bool isAioEnabled = fakeAioConfiguration.Value.IsAioEnabled;

            LinkSetBatchStatusUri linkSetBatchStatusUri = new()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/status"
            };
            LinkSetBatchDetailsUri linkSetBatchDetailsUri = new()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
            };
            LinkSetFileUri linkSetFileUri = new()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip",
            };
            LinkSetFileUri AiolinkSetFileUri = new()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/aio123.zip",
            };
            Common.Models.Response.Links links = new()
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
                    ProductName = "GB160060",
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

        private static ExchangeSetResponse GetExchangeSetResponseAioToggleOff()
        {
            LinkSetBatchStatusUri linkSetBatchStatusUri = new()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/status"
            };
            LinkSetBatchDetailsUri linkSetBatchDetailsUri = new()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
            };
            LinkSetFileUri linkSetFileUri = new()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip",
            };
            Common.Models.Response.Links links = new()
            {
                ExchangeSetBatchStatusUri = linkSetBatchStatusUri,
                ExchangeSetBatchDetailsUri = linkSetBatchDetailsUri,
                ExchangeSetFileUri = linkSetFileUri,
            };
            List<RequestedProductsNotInExchangeSet> lstRequestedProductsNotInExchangeSet = new()
            {
                new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "GB123456",
                    Reason = "productWithdrawn"
                },
                new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "GB160060",
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

        private static ExchangeSetResponse GetExchangeSetResponseAioToggleON()
        {
            LinkSetBatchStatusUri linkSetBatchStatusUri = new()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/status"
            };
            LinkSetBatchDetailsUri linkSetBatchDetailsUri = new()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
            };
            LinkSetFileUri linkSetFileUri = new()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip",
            };
            LinkSetFileUri AiolinkSetFileUri = new()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/aio123.zip",
            };
            Common.Models.Response.Links links = new()
            {
                ExchangeSetBatchStatusUri = linkSetBatchStatusUri,
                ExchangeSetBatchDetailsUri = linkSetBatchDetailsUri,
                ExchangeSetFileUri = linkSetFileUri,
                AioExchangeSetFileUri = AiolinkSetFileUri
            };
            List<RequestedProductsNotInExchangeSet> lstRequestedProductsNotInExchangeSet = new()
            {   new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "GB123456",
                    Reason = "productWithdrawn"
                },
                new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "GB160060",
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
                            new RequestedProductsNotReturned { ProductName = "GB160060", Reason = "invalidProduct" }
                        }
                    },
                    Products = new List<Products> {
                        new Products {
                            ProductName = "AU334550",
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

        private static SalesCatalogueResponse GetSalesCatalogueFileSizeResponse()
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
                                FileSize = 900000000
                            }
                        }
                },
                ScsRequestDateTime = DateTime.UtcNow
            };
        }

        #endregion GetSalesCatalogueResponse

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

        #endregion AzureADB2CToken

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

            Assert.That(result.IsValid,Is.False);
            Assert.That("Product Identifiers cannot be blank or null.", Is.EqualTo(result.Errors.Single().ErrorMessage));
        }

        [Test]
        public async Task WhenInvalidNullProductIdentifierRequest_ThenValidateProductDataByProductIdentifiersReturnsBadrequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("RequestBody", "Either body is null or malformed.")}));

            var result = await service.ValidateProductDataByProductIdentifiers(null);
            Assert.That(result.IsValid, Is.False);
            Assert.That("Either body is null or malformed.", Is.EqualTo(result.Errors.Single().ErrorMessage));
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenValidProductIdentifierRequest_ThenValidateProductDataByProductIdentifierReturnsOkrequest(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;

            var result = await service.ValidateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri,
                    ExchangeSetStandard = exchangeSetStandard.ToString()

                });

            Assert.That(result.IsValid,Is.True);
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

            Assert.That(result, Is.InstanceOf<ExchangeSetServiceResponse>());
            Assert.That(HttpStatusCode.BadRequest, Is.EqualTo(result.HttpStatusCode));
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

            Assert.That(result, Is.InstanceOf<ExchangeSetServiceResponse>());
            Assert.That(HttpStatusCode.BadRequest, Is.EqualTo(result.HttpStatusCode));
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenValidProductIdentifierRequest_AndFileSizeIsLessThan700Mb_ThenCreateProductDataByProductIdentifierReturnsOkAndCreatesExchangeSet(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            var azureAdToken = GetAzureADToken();
            

            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);

            var exchangeSetResponse = GetExchangeSetResponse();
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);

            var CreateBatchResponseModel = CreateBatchResponse();
            string callBackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";

            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);
            A.CallTo(() => fakeExchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, CreateBatchResponseModel.ResponseBody.BatchId, callBackUri, A<string>.Ignored, correlationId, A<string>.Ignored, salesCatalogueResponse.ScsRequestDateTime, A<bool>.Ignored, A<bool>.Ignored, A<ExchangeSetResponse>.Ignored)).Returns(true);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri,
                    ExchangeSetStandard = exchangeSetStandard.ToString()
                }, azureAdToken);

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();

            Assert.That(HttpStatusCode.Created, Is.EqualTo(result.HttpStatusCode));
            Assert.That(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetCellCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount));
            Assert.That(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchStatusUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href));
            Assert.That(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchDetailsUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href));
            Assert.That(exchangeSetResponseAioToggleOff.Links.ExchangeSetFileUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href));
            Assert.That(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime));
            Assert.That(exchangeSetResponseAioToggleOff.BatchId, Is.EqualTo(result.BatchId));

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is Off, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
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

            Assert.That(HttpStatusCode.Created, Is.EqualTo(result.HttpStatusCode));
            Assert.That(result.LastModified, Is.Not.Null);
            Assert.That(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetCellCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount));
            Assert.That(exchangeSetResponseAioToggleOff.BatchId, Is.EqualTo(result.BatchId));

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is Off, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
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

            Assert.That(result, Is.InstanceOf<ExchangeSetServiceResponse>());
            Assert.That(HttpStatusCode.InternalServerError, Is.EqualTo(result.HttpStatusCode));
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenInvalidFSSCreateBatchProductIdentifierRequest_ThenCreateProductDataByProductIdentifierReturnsInternalServerError(ExchangeSetStandard exchangeSetStandard)
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
                    CallbackUri = callbackUri,
                    ExchangeSetStandard = exchangeSetStandard.ToString()
                }, azureAdToken);

            Assert.That(result.ExchangeSetResponse, Is.Null);
            Assert.That(HttpStatusCode.InternalServerError, Is.EqualTo(result.HttpStatusCode));
        }

        [Test]
        public async Task WhenBatchNotCreatedProductIdentifierRequest_ThenCreateProductDataByProductIdentifierReturnsOkWithoutStoringData()
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
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.NotModified;
            string callBackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";

            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);

            await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri,
                    ExchangeSetStandard = ExchangeSetStandard.s63.ToString()
                }, azureAdToken);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is Off, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustNotHaveHappened();

            A.CallTo(() => fakeExchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, CreateBatchResponseModel.ResponseBody.BatchId, callBackUri, A<string>.Ignored, correlationId, A<string>.Ignored, salesCatalogueResponse.ScsRequestDateTime, A<bool>.Ignored, A<bool>.Ignored, A<ExchangeSetResponse>.Ignored)).MustNotHaveHappened();
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
            A.CallTo(() => fakeExchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, CreateBatchResponseModel.ResponseBody.BatchId, callBackUri, A<string>.Ignored, correlationId, A<string>.Ignored, salesCatalogueResponse.ScsRequestDateTime, A<bool>.Ignored, A<bool>.Ignored, A<ExchangeSetResponse>.Ignored)).Returns(true);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri,
                    ExchangeSetStandard = ExchangeSetStandard.s63.ToString()
                }, azureAdToken);

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();
            exchangeSetResponseAioToggleOff.RequestedProductCount += 1; //one aio cell passed

            Assert.That(HttpStatusCode.Created, Is.EqualTo(result.HttpStatusCode));
            Assert.That(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetCellCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount));
            Assert.That(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchStatusUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href));
            Assert.That(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchDetailsUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href));
            Assert.That(exchangeSetResponseAioToggleOff.Links.ExchangeSetFileUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href));
            Assert.That(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime));
            Assert.That(exchangeSetResponseAioToggleOff.BatchId, Is.EqualTo(result.BatchId));
            //Aio cell details
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductsNotInExchangeSet.Count, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsNotInExchangeSet.Count));

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is Off, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
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
            A.CallTo(() => fakeExchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, CreateBatchResponseModel.ResponseBody.BatchId, callBackUri, A<string>.Ignored, correlationId, A<string>.Ignored, salesCatalogueResponse.ScsRequestDateTime, A<bool>.Ignored, A<bool>.Ignored, A<ExchangeSetResponse>.Ignored)).Returns(true);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri,
                    ExchangeSetStandard = ExchangeSetStandard.s63.ToString()
                }, azureAdToken);

            var exchangeSetResponseAioToggleOn = GetExchangeSetResponseAioToggleON();

            Assert.That(HttpStatusCode.Created, Is.EqualTo(result.HttpStatusCode));
            Assert.That(exchangeSetResponseAioToggleOn.ExchangeSetCellCount, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetCellCount));
            Assert.That(exchangeSetResponseAioToggleOn.RequestedProductCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductCount));
            Assert.That(exchangeSetResponseAioToggleOn.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount));
            Assert.That(exchangeSetResponseAioToggleOn.Links.ExchangeSetBatchStatusUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href));
            Assert.That(exchangeSetResponseAioToggleOn.Links.ExchangeSetBatchDetailsUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href));
            Assert.That(exchangeSetResponseAioToggleOn.Links.ExchangeSetFileUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href));
            Assert.That(exchangeSetResponseAioToggleOn.ExchangeSetUrlExpiryDateTime, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime));
            Assert.That(exchangeSetResponseAioToggleOn.BatchId, Is.EqualTo(result.BatchId));
            //Aio cell details
            Assert.That(exchangeSetResponseAioToggleOn.AioExchangeSetCellCount, Is.EqualTo(result.ExchangeSetResponse.AioExchangeSetCellCount));
            Assert.That(exchangeSetResponseAioToggleOn.RequestedAioProductCount, Is.EqualTo(result.ExchangeSetResponse.RequestedAioProductCount));
            Assert.That(exchangeSetResponseAioToggleOn.RequestedAioProductsAlreadyUpToDateCount, Is.EqualTo(result.ExchangeSetResponse.RequestedAioProductsAlreadyUpToDateCount));
            Assert.That(exchangeSetResponseAioToggleOn.RequestedProductsNotInExchangeSet.Count, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsNotInExchangeSet.Count));
            Assert.That(exchangeSetResponseAioToggleOn.Links.AioExchangeSetFileUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.AioExchangeSetFileUri.Href));

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOn.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is ON, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidProductIdentifierRequestWithExchangeSetStandardS57_AndFileSizeIsMoreThan700Mb_ThenCreateProductDataByProductIdentifierReturnsBadRequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            var salesCatalogueResponse = GetSalesCatalogueFileSizeResponse();

            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = string.Empty,
                    ExchangeSetStandard = ExchangeSetStandard.s57.ToString()
                }, GetAzureADToken());

            result.Should().BeOfType<ExchangeSetServiceResponse>();
            result.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task WhenValidProductIdentifierRequestWithExchangeSetStandardS63_AndFileSizeIsMoreThan700Mb_ThenCreateProductDataByProductIdentifierReturnsOkAndCreatesExchangeSet()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            var salesCatalogueResponse = GetSalesCatalogueFileSizeResponse();

            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);

            var exchangeSetResponse = GetExchangeSetResponse();
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);

            var CreateBatchResponseModel = CreateBatchResponse();
            string callBackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";

            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);
            A.CallTo(() => fakeExchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, CreateBatchResponseModel.ResponseBody.BatchId, callBackUri, A<string>.Ignored, correlationId, A<string>.Ignored, salesCatalogueResponse.ScsRequestDateTime, A<bool>.Ignored, A<bool>.Ignored, A<ExchangeSetResponse>.Ignored)).Returns(true);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callBackUri,
                    ExchangeSetStandard = ExchangeSetStandard.s63.ToString()
                }, GetAzureADToken());

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();

            result.Should().BeOfType<ExchangeSetServiceResponse>();
            result.HttpStatusCode.Should().Be(HttpStatusCode.Created);
            result.ExchangeSetResponse.ExchangeSetCellCount.Should().Be(exchangeSetResponseAioToggleOff.ExchangeSetCellCount);
            result.ExchangeSetResponse.RequestedProductCount.Should().Be(exchangeSetResponseAioToggleOff.RequestedProductCount);
            result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount.Should().Be(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount);
            result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href.Should().Be(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchStatusUri.Href);
            result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href.Should().Be(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchDetailsUri.Href);
            result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href.Should().Be(exchangeSetResponseAioToggleOff.Links.ExchangeSetFileUri.Href);
            result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime.Should().Be(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime);
            result.BatchId.Should().Be(exchangeSetResponseAioToggleOff.BatchId);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is Off, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        #endregion ProductIdentifiers

        #region ProductVersions

        [Test]
        public async Task WhenInvalidProductVersionRequest_ThenValidateProductDataByProductVersionsReturnsBadrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("productName", "productName cannot be blank or null.")}));

            var result = await service.ValidateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            { ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest() { ProductName = null } } });

            Assert.That(result.IsValid, Is.False);
            Assert.That("productName cannot be blank or null.", Is.EqualTo(result.Errors.Single().ErrorMessage));
        }

        [Test]
        public async Task WhenInvalidNullProductVersionRequest_ThenValidateProductDataByProductVersionsReturnsBadrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("RequestBody", "Either body is null or malformed.")}));

            var result = await service.ValidateProductDataByProductVersions(null);

            Assert.That(result.IsValid, Is.False);
            Assert.That("Either body is null or malformed.", Is.EqualTo(result.Errors.Single().ErrorMessage));
        }

        [Test]
        public async Task WhenValidProductVersionRequest_ThenValidateProductDataByProductVersionsReturnsOkrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            var result = await service.ValidateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            { ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest() { ProductName = "Demo", EditionNumber = 5, UpdateNumber = 0 } } });

            Assert.That(result.IsValid,Is.True);
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

            Assert.That(result, Is.InstanceOf<ExchangeSetServiceResponse>());
            Assert.That(HttpStatusCode.BadRequest, Is.EqualTo(result.HttpStatusCode));
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

            Assert.That(result, Is.InstanceOf<ExchangeSetServiceResponse>());
            Assert.That(HttpStatusCode.BadRequest, Is.EqualTo(result.HttpStatusCode));
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenValidProductVersionRequest_AndFileSizeIsLessThan700Mb_ThenCreateProductDataByProductVersionsReturnsOkRequest(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            var azureAdToken = GetAzureADToken();           

            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var exchangeSetResponse = GetExchangeSetResponse();
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);

            var CreateBatchResponseModel = CreateBatchResponse();
            string callBackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";

            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);
            A.CallTo(() => fakeExchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, CreateBatchResponseModel.ResponseBody.BatchId, callBackUri, A<string>.Ignored, correlationId, A<string>.Ignored, salesCatalogueResponse.ScsRequestDateTime, A<bool>.Ignored, A<bool>.Ignored, A<ExchangeSetResponse>.Ignored)).Returns(true);

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
                CallbackUri = string.Empty,
                ExchangeSetStandard = exchangeSetStandard.ToString()
            }, azureAdToken);

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();

            Assert.That(HttpStatusCode.Created, Is.EqualTo(result.HttpStatusCode));
            Assert.That(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetCellCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount));
            Assert.That(exchangeSetResponseAioToggleOff.BatchId, Is.EqualTo(result.BatchId));

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is Off, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
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
            string callBackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";

            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);
            A.CallTo(() => fakeExchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, CreateBatchResponseModel.ResponseBody.BatchId, callBackUri, A<string>.Ignored, correlationId, A<string>.Ignored, salesCatalogueResponse.ScsRequestDateTime, A<bool>.Ignored, A<bool>.Ignored, A<ExchangeSetResponse>.Ignored)).Returns(true); A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);

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

            Assert.That(result, Is.InstanceOf<ExchangeSetServiceResponse>());
            Assert.That(HttpStatusCode.Created, Is.EqualTo(result.HttpStatusCode));
            Assert.That(result.LastModified,Is.Null);
            Assert.That(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetCellCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount));
            Assert.That(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime));
            Assert.That(exchangeSetResponseAioToggleOff.BatchId, Is.EqualTo(result.BatchId));

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
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
            string callBackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";

            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);
            A.CallTo(() => fakeExchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, CreateBatchResponseModel.ResponseBody.BatchId, callBackUri, A<string>.Ignored, correlationId, A<string>.Ignored, salesCatalogueResponse.ScsRequestDateTime, A<bool>.Ignored, A<bool>.Ignored, A<ExchangeSetResponse>.Ignored)).Returns(true); A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);

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

            Assert.That(result, Is.InstanceOf<ExchangeSetServiceResponse>());
            Assert.That(HttpStatusCode.Created, Is.EqualTo(result.HttpStatusCode));
            Assert.That(result.LastModified,Is.Not.Null);
            Assert.That(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetCellCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount));
            Assert.That(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime));
            Assert.That(exchangeSetResponseAioToggleOff.BatchId, Is.EqualTo(result.BatchId));

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenValidProductVersionRequest_ThenCreateProductDataByProductVersionsReturnsNotModifiedToBadRequest(ExchangeSetStandard exchangeSetStandard)
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
                CallbackUri = "",
                ExchangeSetStandard = exchangeSetStandard.ToString()

            }, azureADToken);

            Assert.That(result, Is.InstanceOf<ExchangeSetServiceResponse>());
            Assert.That(HttpStatusCode.InternalServerError, Is.EqualTo(result.HttpStatusCode));
            Assert.That(result.LastModified,Is.Null);
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenInvalidFSSCreateBatchProductVersionRequest_ThenCreateProductDataByProductVersionsReturnsInternalServerError(ExchangeSetStandard exchangeSetStandard)
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
                CallbackUri = "",
                ExchangeSetStandard = exchangeSetStandard.ToString()
            }, azureAdToken);

            Assert.That(result.ExchangeSetResponse,Is.Null);
            Assert.That(HttpStatusCode.InternalServerError, Is.EqualTo(result.HttpStatusCode));
        }

        [Test]
        public async Task WhenBatchNotCreatedProductVersionRequest_ThenCreateProductDataByProductVersionsReturnsOkWithoutStoringData()
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
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.NotModified;
            string callBackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";

            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);

            await service.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
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
                CallbackUri = "",
                ExchangeSetStandard = ExchangeSetStandard.s63.ToString()
            }, azureAdToken);

            A.CallTo(() => fakeExchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, CreateBatchResponseModel.ResponseBody.BatchId, callBackUri, A<string>.Ignored, correlationId, A<string>.Ignored, salesCatalogueResponse.ScsRequestDateTime, A<bool>.Ignored, A<bool>.Ignored, A<ExchangeSetResponse>.Ignored)).MustNotHaveHappened();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustNotHaveHappened();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is Off, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
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
                CallbackUri = "",
                ExchangeSetStandard = ExchangeSetStandard.s63.ToString()
            }, azureAdToken);

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();
            exchangeSetResponseAioToggleOff.RequestedProductCount += 1; //one aio cell passed

            Assert.That(result, Is.InstanceOf<ExchangeSetServiceResponse>());
            Assert.That(HttpStatusCode.Created, Is.EqualTo(result.HttpStatusCode));
            Assert.That(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetCellCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount));
            Assert.That(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchStatusUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href));
            Assert.That(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchDetailsUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href));
            Assert.That(exchangeSetResponseAioToggleOff.Links.ExchangeSetFileUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href));
            Assert.That(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime));
            Assert.That(exchangeSetResponseAioToggleOff.BatchId, Is.EqualTo(result.BatchId));
            //Aio cell details
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductsNotInExchangeSet.Count, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsNotInExchangeSet.Count));

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is Off, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidProductVersionRequestWithExchangeSetStandardS57_AndFileSizeIsMoreThan700Mb_ThenCreateProductDataByProductVersionsReturnsBadRequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueFileSizeResponse();
            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);

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
                CallbackUri = string.Empty,
                ExchangeSetStandard = ExchangeSetStandard.s57.ToString()
            }, GetAzureADToken());

            result.Should().BeOfType<ExchangeSetServiceResponse>();
            result.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task WhenValidProductVersionRequestWithExchangeSetStandardS63_AndFileSizeIsMoreThan700Mb_ThenCreateProductDataByProductVersionsReturnsOkAndCreatesExchangeSet()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueFileSizeResponse();
            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var exchangeSetResponse = GetExchangeSetResponse();
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);

            var CreateBatchResponseModel = CreateBatchResponse();
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
                CallbackUri = string.Empty,
                ExchangeSetStandard = ExchangeSetStandard.s63.ToString()
            }, GetAzureADToken());

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();

            result.Should().BeOfType<ExchangeSetServiceResponse>();
            result.HttpStatusCode.Should().Be(HttpStatusCode.Created);
            result.ExchangeSetResponse.ExchangeSetCellCount.Should().Be(exchangeSetResponseAioToggleOff.ExchangeSetCellCount);
            result.ExchangeSetResponse.RequestedProductCount.Should().Be(exchangeSetResponseAioToggleOff.RequestedProductCount);
            result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount.Should().Be(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount);
            result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href.Should().Be(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchStatusUri.Href);
            result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href.Should().Be(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchDetailsUri.Href);
            result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href.Should().Be(exchangeSetResponseAioToggleOff.Links.ExchangeSetFileUri.Href);
            result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime.Should().Be(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime);
            result.BatchId.Should().Be(exchangeSetResponseAioToggleOff.BatchId);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is Off, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidProductVersionRequest_And_AIOToggleIsON_ThenCreateProductDataByProductVersionsReturnsOkrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
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
                CallbackUri = "",
                ExchangeSetStandard = ExchangeSetStandard.s63.ToString()
            }, azureAdToken);

            var exchangeSetResponseAioToggleOn = GetExchangeSetResponseAioToggleON();

            Assert.That(result, Is.InstanceOf<ExchangeSetServiceResponse>());
            Assert.That(HttpStatusCode.Created, Is.EqualTo(result.HttpStatusCode));
            Assert.That(exchangeSetResponseAioToggleOn.ExchangeSetCellCount, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetCellCount));
            Assert.That(exchangeSetResponseAioToggleOn.RequestedProductCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductCount));
            Assert.That(exchangeSetResponseAioToggleOn.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount));
            Assert.That(exchangeSetResponseAioToggleOn.Links.ExchangeSetBatchStatusUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href));
            Assert.That(exchangeSetResponseAioToggleOn.Links.ExchangeSetBatchDetailsUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href));
            Assert.That(exchangeSetResponseAioToggleOn.Links.ExchangeSetFileUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href));
            Assert.That(exchangeSetResponseAioToggleOn.ExchangeSetUrlExpiryDateTime, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime));
            Assert.That(exchangeSetResponseAioToggleOn.BatchId, Is.EqualTo(result.BatchId));
            //Aio cell details
            Assert.That(exchangeSetResponseAioToggleOn.AioExchangeSetCellCount, Is.EqualTo(result.ExchangeSetResponse.AioExchangeSetCellCount));
            Assert.That(exchangeSetResponseAioToggleOn.RequestedAioProductCount, Is.EqualTo(result.ExchangeSetResponse.RequestedAioProductCount));
            Assert.That(exchangeSetResponseAioToggleOn.RequestedAioProductsAlreadyUpToDateCount, Is.EqualTo(result.ExchangeSetResponse.RequestedAioProductsAlreadyUpToDateCount));
            Assert.That(exchangeSetResponseAioToggleOn.RequestedProductsNotInExchangeSet.Count, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsNotInExchangeSet.Count));
            Assert.That(exchangeSetResponseAioToggleOn.Links.AioExchangeSetFileUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.AioExchangeSetFileUri.Href));

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOn.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is ON, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
        }

        #endregion ProductVersions

        #region ScsValidateProductIndentifier

        [Test]
        public async Task WhenInvalidScsProductIdentifierRequest_ThenValidateProductDataByScsProductIdentifiersReturnsBadrequest()
        {
            A.CallTo(() => fakeScsProductIdentifierValidator.Validate(A<ScsProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("ProductIdentifiers", "Product Identifiers cannot be blank or null.")}));

            var result = await service.ValidateScsProductDataByProductIdentifiers(new ScsProductIdentifierRequest());

            Assert.That(result.IsValid, Is.False);
            Assert.That("Product Identifiers cannot be blank or null.", Is.EqualTo(result.Errors.Single().ErrorMessage));
        }

        [Test]
        public async Task WhenInvalidNullScsProductIdentifierRequest_ThenValidateProductDataByScsProductIdentifiersReturnsBadrequest()
        {
            A.CallTo(() => fakeScsProductIdentifierValidator.Validate(A<ScsProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("RequestBody", "Either body is null or malformed.")}));

            var result = await service.ValidateScsProductDataByProductIdentifiers(null);
            Assert.That(result.IsValid, Is.False);
            Assert.That("Either body is null or malformed.", Is.EqualTo(result.Errors.Single().ErrorMessage));
        }

        [Test]
        public async Task WhenValidScsProductIdentifierRequest_ThenValidateProductDataByScsProductIdentifierReturnsOkrequest()
        {
            A.CallTo(() => fakeScsProductIdentifierValidator.Validate(A<ScsProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] scsProductIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };

            var result = await service.ValidateScsProductDataByProductIdentifiers(
                new ScsProductIdentifierRequest()
                {
                    ProductIdentifier = scsProductIdentifiers,
                });

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public async Task WhenValidScsProductIdentifierRequest_ThenCreateProductDataByProductIdentifierReturnsOkrequest()
        {
            A.CallTo(() => fakeScsProductIdentifierValidator.Validate(A<ScsProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            var salesCatalogueResponse = GetSalesCatalogueResponse();

            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ScsProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers
                });

            Assert.That(HttpStatusCode.OK, Is.EqualTo(result.ResponseCode));
        }

        [Test]
        public async Task WhenEmptyScsProductIdentifierRequest_ThenCreateProductDataByProductIdentifierReturnsBadrequest()
        {
            A.CallTo(() => fakeScsProductIdentifierValidator.Validate(A<ScsProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { };
            var salesCatalogueResponse = GetSalesCatalogueResponse();

            salesCatalogueResponse.ResponseCode = HttpStatusCode.BadRequest;
            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ScsProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers
                });

            Assert.That(HttpStatusCode.BadRequest, Is.EqualTo(result.ResponseCode));
        }

        #endregion ScsValidateProductIndentifier

        #region ProductDataSinceDateTime

        [Test]
        public async Task WhenInvalidSinceDateTimeInRequest_ThenValidateProductDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                            { new ValidationFailure("SinceDateTime", "Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format'.")}));

            var result = await service.ValidateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.That(result.IsValid, Is.False);
            Assert.That("Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format'.", Is.EqualTo(result.Errors.Single().ErrorMessage));
        }

        [Test]
        public async Task WhenSinceDateTimeFormatIsGreaterThanCurrrentDateTimeInRequest_ThenValidateProductDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                            { new ValidationFailure("SinceDateTime", "Provided sinceDateTime cannot be a future date.")}));

            var result = await service.ValidateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.That(result.IsValid, Is.False);
            Assert.That("Provided sinceDateTime cannot be a future date.", Is.EqualTo(result.Errors.Single().ErrorMessage));
        }

        [Test]
        public async Task WhenCallbackUrlParameterNotValidInRequest_ThenValidateProductDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                            { new ValidationFailure("CallbackUri", "Invalid callbackUri format.")}));

            var result = await service.ValidateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.That(result.IsValid, Is.False);
            Assert.That("Invalid callbackUri format.", Is.EqualTo(result.Errors.Single().ErrorMessage));
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenValidProductDataSinceDateTimeInRequest_AndFileSizeIsLessThan700Mb_ThenCreateProductDataSinceDateTimeReturnsOkAndCreatesExchangeSet(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.LastModified = DateTime.UtcNow;
            var exchangeSetResponse = GetExchangeSetResponse();
            var CreateBatchResponseModel = CreateBatchResponse();           

            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);
            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest()
            {
                ExchangeSetStandard = exchangeSetStandard.ToString(),
                CallbackUri = string.Empty
            }, GetAzureADToken());

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();

            result.Should().BeOfType<ExchangeSetServiceResponse>();
            result.HttpStatusCode.Should().Be(HttpStatusCode.Created);
            result.ExchangeSetResponse.ExchangeSetCellCount.Should().Be(exchangeSetResponseAioToggleOff.ExchangeSetCellCount);
            result.ExchangeSetResponse.RequestedProductCount.Should().Be(exchangeSetResponseAioToggleOff.RequestedProductCount);
            result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount.Should().Be(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount);
            result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href.Should().Be(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchStatusUri.Href);
            result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href.Should().Be(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchDetailsUri.Href);
            result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href.Should().Be(exchangeSetResponseAioToggleOff.Links.ExchangeSetFileUri.Href);
            result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime.Should().Be(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime);
            result.BatchId.Should().Be(exchangeSetResponseAioToggleOff.BatchId);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is Off, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenValidateProductDataSinceDateTimeInRequest_ThenCreateProductDataSinceDateTimeReturnNotModified(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.NotModified;
            salesCatalogueResponse.LastModified = DateTime.UtcNow;
            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest()
            {
                CallbackUri = string.Empty,
                ExchangeSetStandard = exchangeSetStandard.ToString()
            }, GetAzureADToken());

            Assert.That(result, Is.InstanceOf<ExchangeSetServiceResponse>());
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

            Assert.That(result, Is.InstanceOf<ExchangeSetServiceResponse>());
            Assert.That(HttpStatusCode.BadRequest, Is.EqualTo(result.HttpStatusCode));
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

            Assert.That(result, Is.InstanceOf<ExchangeSetServiceResponse>());
            Assert.That(HttpStatusCode.BadRequest, Is.EqualTo(result.HttpStatusCode));
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

            Assert.That(result, Is.InstanceOf<ExchangeSetServiceResponse>());
            Assert.That(HttpStatusCode.Created, Is.EqualTo(result.HttpStatusCode));
            Assert.That(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetCellCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount));
            Assert.That(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchStatusUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href));
            Assert.That(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchDetailsUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href));
            Assert.That(exchangeSetResponseAioToggleOff.Links.ExchangeSetFileUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href));
            Assert.That(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime));
            Assert.That(exchangeSetResponseAioToggleOff.BatchId, Is.EqualTo(result.BatchId));

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is Off, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
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

            Assert.That(result.ExchangeSetResponse, Is.Null);
            Assert.That(HttpStatusCode.InternalServerError, Is.EqualTo(result.HttpStatusCode));
        }

        [Test]
        public async Task WhenBatchNotCreatedProductDataSinceDateTimeInRequest_ThenCreateProductDataSinceDateTimeReturnsOkWithoutStoringData()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            salesCatalogueResponse.LastModified = DateTime.UtcNow;
            var exchangeSetResponse = GetExchangeSetResponse();
            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.NotModified;
            string callBackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";

            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);
            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);

            await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest(), GetAzureB2CToken());//B2C token passed and file size less than 300 mb

            A.CallTo(() => fakeExchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, CreateBatchResponseModel.ResponseBody.BatchId, callBackUri, A<string>.Ignored, correlationId, A<string>.Ignored, salesCatalogueResponse.ScsRequestDateTime, A<bool>.Ignored, A<bool>.Ignored, A<ExchangeSetResponse>.Ignored)).MustNotHaveHappened();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustNotHaveHappened();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is Off, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
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

            Assert.That(result, Is.InstanceOf<ExchangeSetServiceResponse>());
            Assert.That(HttpStatusCode.Created, Is.EqualTo(result.HttpStatusCode));
            Assert.That(exchangeSetResponseAioToggleOff.ExchangeSetCellCount, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetCellCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductCount));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount));
            Assert.That(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchStatusUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href));
            Assert.That(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchDetailsUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href));
            Assert.That(exchangeSetResponseAioToggleOff.Links.ExchangeSetFileUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href));
            Assert.That(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime));
            Assert.That(exchangeSetResponseAioToggleOff.BatchId, Is.EqualTo(result.BatchId));
            Assert.That(exchangeSetResponseAioToggleOff.RequestedProductsNotInExchangeSet.Count, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsNotInExchangeSet.Count));

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is Off, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
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
            exchangeSetResponseAioToggleOn.RequestedAioProductCount = 0;

            Assert.That(result, Is.InstanceOf<ExchangeSetServiceResponse>());
            Assert.That(HttpStatusCode.Created, Is.EqualTo(result.HttpStatusCode));
            Assert.That(exchangeSetResponseAioToggleOn.ExchangeSetCellCount, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetCellCount));
            Assert.That(exchangeSetResponseAioToggleOn.RequestedProductCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductCount));
            Assert.That(exchangeSetResponseAioToggleOn.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount));
            Assert.That(exchangeSetResponseAioToggleOn.Links.ExchangeSetBatchStatusUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href));
            Assert.That(exchangeSetResponseAioToggleOn.Links.ExchangeSetBatchDetailsUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href));
            Assert.That(exchangeSetResponseAioToggleOn.ExchangeSetUrlExpiryDateTime, Is.EqualTo(result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime));
            Assert.That(exchangeSetResponseAioToggleOn.BatchId, Is.EqualTo(result.BatchId));
            //Aio cell details
            Assert.That(exchangeSetResponseAioToggleOn.AioExchangeSetCellCount, Is.EqualTo(result.ExchangeSetResponse.AioExchangeSetCellCount));
            Assert.That(exchangeSetResponseAioToggleOn.RequestedAioProductCount, Is.EqualTo(result.ExchangeSetResponse.RequestedAioProductCount));
            Assert.That(exchangeSetResponseAioToggleOn.RequestedAioProductsAlreadyUpToDateCount, Is.EqualTo(result.ExchangeSetResponse.RequestedAioProductsAlreadyUpToDateCount));
            Assert.That(exchangeSetResponseAioToggleOn.RequestedProductsNotInExchangeSet.Count, Is.EqualTo(result.ExchangeSetResponse.RequestedProductsNotInExchangeSet.Count));
            Assert.That(exchangeSetResponseAioToggleOn.Links.AioExchangeSetFileUri.Href, Is.EqualTo(result.ExchangeSetResponse.Links.AioExchangeSetFileUri.Href));

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOn.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is ON, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidProductDataSinceDateTimeInRequestWithExchangeSetStandardS57_AndFileSizeIsMoreThan700Mb_ThenCreateProductDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueFileSizeResponse();
            salesCatalogueResponse.LastModified = DateTime.UtcNow;

            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest()
            {
                ExchangeSetStandard = ExchangeSetStandard.s57.ToString(),
                CallbackUri = string.Empty
            }, GetAzureADToken());

            result.Should().BeOfType<ExchangeSetServiceResponse>();
            result.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task WhenValidProductDataSinceDateTimeInRequestWithExchangeSetStandardS63_AndFileSizeIsMoreThan700Mb_ThenCreateProductDataSinceDateTimeReturnsOkAndCreatesExchangeSet()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueFileSizeResponse();
            salesCatalogueResponse.LastModified = DateTime.UtcNow;
            var exchangeSetResponse = GetExchangeSetResponse();
            var CreateBatchResponseModel = CreateBatchResponse();

            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<List<RequestedProductsNotReturned>>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);
            A.CallTo(() => fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored)).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest()
            {
                ExchangeSetStandard = ExchangeSetStandard.s63.ToString(),
                CallbackUri = string.Empty
            }, GetAzureADToken());

            var exchangeSetResponseAioToggleOff = GetExchangeSetResponseAioToggleOff();

            result.Should().BeOfType<ExchangeSetServiceResponse>();
            result.HttpStatusCode.Should().Be(HttpStatusCode.Created);
            result.ExchangeSetResponse.ExchangeSetCellCount.Should().Be(exchangeSetResponseAioToggleOff.ExchangeSetCellCount);
            result.ExchangeSetResponse.RequestedProductCount.Should().Be(exchangeSetResponseAioToggleOff.RequestedProductCount);
            result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount.Should().Be(exchangeSetResponseAioToggleOff.RequestedProductsAlreadyUpToDateCount);
            result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri.Href.Should().Be(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchStatusUri.Href);
            result.ExchangeSetResponse.Links.ExchangeSetBatchDetailsUri.Href.Should().Be(exchangeSetResponseAioToggleOff.Links.ExchangeSetBatchDetailsUri.Href);
            result.ExchangeSetResponse.Links.ExchangeSetFileUri.Href.Should().Be(exchangeSetResponseAioToggleOff.Links.ExchangeSetFileUri.Href);
            result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime.Should().Be(exchangeSetResponseAioToggleOff.ExchangeSetUrlExpiryDateTime);
            result.BatchId.Should().Be(exchangeSetResponseAioToggleOff.BatchId);

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.AIOToggleIsOff.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO toggle is Off, additional aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FSSCreateBatchRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();

            A.CallTo(logger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSResponseStoreRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        #endregion ProductDataSinceDateTime

        #region ScsProductDataSinceDateTime

        [Test]
        public async Task GetProductDataSinceDateTimeReturnNotModified()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.NotModified;
            salesCatalogueResponse.LastModified = DateTime.UtcNow;
            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var result = await service.GetProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.That(result, Is.InstanceOf<SalesCatalogueResponse>());
        }

        [Test]
        public async Task GetProductDataSinceDateTimeReturnSuccess()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueFileSizeResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            salesCatalogueResponse.LastModified = DateTime.UtcNow;

            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var result = await service.GetProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.That(HttpStatusCode.OK, Is.EqualTo(result.ResponseCode));
        }

        [Test]
        public async Task WhenInvalidSinceDateTimeInRequest_ThenValidateScsDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeScsDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    { new ValidationFailure("SinceDateTime", "Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format'.")}));

            var result = await service.ValidateScsDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.That(result.IsValid, Is.False);
            Assert.That("Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format'.", Is.EqualTo(result.Errors.Single().ErrorMessage));
        }

        [Test]
        public async Task WhenSinceDateTimeFormatIsGreaterThanCurrrentDateTimeInRequest_ThenValidateScsDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeScsDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    { new ValidationFailure("SinceDateTime", "Provided sinceDateTime cannot be a future date.")}));

            var result = await service.ValidateScsDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.That(result.IsValid, Is.False);
            Assert.That("Provided sinceDateTime cannot be a future date.", Is.EqualTo(result.Errors.Single().ErrorMessage));
        }

        #endregion ScsProductDataSinceDateTime

    }
}
