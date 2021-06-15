using AutoMapper;
using FakeItEasy;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Helpers;
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
        private ILogger<FileShareService> logger;
        private IMapper fakeMapper;
        private IExchangeSetStorageProvider fakeExchangeSetStorageProvider; 

        [SetUp]
        public void Setup()
        {
            fakeProductIdentifierValidator = A.Fake<IProductIdentifierValidator>();
            fakeProductVersionValidator = A.Fake<IProductDataProductVersionsValidator>();
            fakeProductDataSinceDateTimeValidator = A.Fake<IProductDataSinceDateTimeValidator>();
            fakeSalesCatalogueService = A.Fake<ISalesCatalogueService>();
            fakeMapper = A.Fake<IMapper>();
            fakeFileShareService = A.Fake<IFileShareService>();
            logger = A.Fake<ILogger<FileShareService>>();
            fakeExchangeSetStorageProvider = A.Fake<ExchangeSetStorageProvider>();            

            service = new ProductDataService(fakeProductIdentifierValidator, fakeProductVersionValidator, fakeProductDataSinceDateTimeValidator,
                fakeSalesCatalogueService, fakeMapper, fakeFileShareService, logger, fakeExchangeSetStorageProvider);
        }

        #region GetExchangeSetResponse

        private ExchangeSetResponse GetExchangeSetResponse()
        {
            LinkSetBatchStatusUri linkSetBatchStatusUri = new LinkSetBatchStatusUri()
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
                ExchangeSetFileUri = linkSetFileUri
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
            ExchangeSetResponse exchangeSetResponse = new ExchangeSetResponse()
            {
                Links = links,
                ExchangeSetUrlExpiryDateTime = Convert.ToDateTime("2021-02-17T16:19:32.269Z").ToUniversalTime(),
                RequestedProductCount = 22,
                ExchangeSetCellCount = 15,
                RequestedProductsAlreadyUpToDateCount = 5,
                RequestedProductsNotInExchangeSet = lstRequestedProductsNotInExchangeSet
            };
            return exchangeSetResponse;
        }

        #endregion GetExchangeSetResponse

        #region GetSalesCatalogueResponse

        private SalesCatalogueResponse GetSalesCatalogueResponse()
        {
            return new SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.BadRequest,
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
                }
            };
        }
        #endregion

        #region CreateBatchResponse
        private static CreateBatchResponse CreateBatchResponse()
        {
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            return new CreateBatchResponse()
            {
                ResponseBody = new CreateBatchResponseModel()
                {
                    BatchId = batchId,
                    BatchStatusUri = $"http://fss.ukho.gov.uk/batch/{batchId}",
                    ExchangeSetFileUri = $"http://fss.ukho.gov.uk/batch/{batchId}/files/exchangeset123.zip",
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
        public async Task WhenValidProductIdentifierRequest_ThenCreateProductDataByProductIdentifierReturnsOkrequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored))
                .Returns(salesCatalogueResponse);
            var exchangeSetResponse = GetExchangeSetResponse();
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<RequestedProductsNotReturned>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;
            string callBackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";

            A.CallTo(() => fakeFileShareService.CreateBatch()).Returns(CreateBatchResponseModel);
            A.CallTo(() => fakeExchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, CreateBatchResponseModel.ResponseBody.BatchId, callBackUri, correlationId)).Returns(true);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri
                });

            Assert.AreEqual(HttpStatusCode.OK, result.HttpStatusCode);
            Assert.AreEqual(exchangeSetResponse.ExchangeSetCellCount, result.ExchangeSetResponse.ExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponse.RequestedProductCount, result.ExchangeSetResponse.RequestedProductCount);
            Assert.AreEqual(exchangeSetResponse.RequestedProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount);
            Assert.AreEqual(exchangeSetResponse.Links.ExchangeSetBatchStatusUri, result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri);
            Assert.AreEqual(exchangeSetResponse.Links.ExchangeSetFileUri, result.ExchangeSetResponse.Links.ExchangeSetFileUri);
            Assert.AreEqual(exchangeSetResponse.ExchangeSetUrlExpiryDateTime, result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime);
        }

        [Test]
        public async Task WhenValidProductIdentifierRequest_ThenCreateProductDataByProductIdentifierReturnsOkrequestWithLastModified()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            salesCatalogueResponse.LastModified = DateTime.Now.AddDays(-4);
            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored))
                .Returns(salesCatalogueResponse);
            var exchangeSetResponse = GetExchangeSetResponse();
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<RequestedProductsNotReturned>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;

            A.CallTo(() => fakeFileShareService.CreateBatch()).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri
                });
            Assert.AreEqual(HttpStatusCode.OK, result.HttpStatusCode);
            Assert.NotNull(result.LastModified);
            Assert.AreEqual(exchangeSetResponse.ExchangeSetCellCount, result.ExchangeSetResponse.ExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponse.RequestedProductCount, result.ExchangeSetResponse.RequestedProductCount);
            Assert.AreEqual(exchangeSetResponse.RequestedProductsAlreadyUpToDateCount, result.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount);
        }

        [Test]
        public async Task WhenValidProductIdentifierRequest_ThenCreateProductDataByProductIdentifierReturnsInternalServerError()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.BadRequest;
            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored))
                .Returns(salesCatalogueResponse);
            var exchangeSetResponse = GetExchangeSetResponse();
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<RequestedProductsNotReturned>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri
                });

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
            
            A.CallTo(() => fakeSalesCatalogueService.PostProductIdentifiersAsync(A<List<string>>.Ignored))
                .Returns(salesCatalogueResponse);
            
            var exchangeSetResponse = GetExchangeSetResponse();
            
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<RequestedProductsNotReturned>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.InternalServerError;
            
            A.CallTo(() => fakeFileShareService.CreateBatch()).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri
                });

            Assert.IsNull(result.ExchangeSetResponse);
            Assert.AreEqual(HttpStatusCode.InternalServerError, result.HttpStatusCode);
        }

        #endregion

        #region ProductVersions

        [Test]
        public async Task WhenInvalidProductVersionRequest_ThenValidateProductDataByProductVersionsReturnsBadrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("ProductName", "productName cannot be blank or null.")}));

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
        public async Task WhenValidProductVersionRequest_ThenCreateProductDataByProductVersionsReturnsOkrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored))
                .Returns(salesCatalogueResponse);
            var exchangeSetResponse = GetExchangeSetResponse();
            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<RequestedProductsNotReturned>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;

            A.CallTo(() => fakeFileShareService.CreateBatch()).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            {
                ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest {
                ProductName = "GB123789", EditionNumber = 6, UpdateNumber = 3 } },
                CallbackUri = ""
            });

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(exchangeSetResponse.Links.ExchangeSetBatchStatusUri, result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri);
            Assert.AreEqual(exchangeSetResponse.Links.ExchangeSetFileUri, result.ExchangeSetResponse.Links.ExchangeSetFileUri);
            Assert.AreEqual(exchangeSetResponse.ExchangeSetUrlExpiryDateTime, result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime);

        }

        [Test]
        public async Task WhenValidProductVersionRequest_ThenCreateProductDataByProductVersionsReturnsNotModifiedToOkrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.NotModified;
            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored))
                .Returns(salesCatalogueResponse);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;

            A.CallTo(() => fakeFileShareService.CreateBatch()).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            {
                ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest {
                ProductName = "GB123789", EditionNumber = 6, UpdateNumber = 3 } },
                CallbackUri = ""
            });

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.OK, result.HttpStatusCode);
            Assert.Null(result.LastModified);
        }

        [Test]
        public async Task WhenValidProductVersionRequest_ThenCreateProductDataByProductVersionsReturnsNotModifiedToOkrequestWithLastModified()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.NotModified;
            salesCatalogueResponse.LastModified = DateTime.Now.AddDays(-2);
            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored))
                .Returns(salesCatalogueResponse);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;

            A.CallTo(() => fakeFileShareService.CreateBatch()).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            {
                ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest {
                ProductName = "GB123789", EditionNumber = 6, UpdateNumber = 3 } },
                CallbackUri = ""
            });

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(HttpStatusCode.OK, result.HttpStatusCode);
            Assert.NotNull(result.LastModified);
        }

        [Test]
        public async Task WhenValidProductVersionRequest_ThenCreateProductDataByProductVersionsReturnsNotModifiedToBadRequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.BadRequest;
            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored))
                .Returns(salesCatalogueResponse);

            var result = await service.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            {
                ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest {
                ProductName = "GB123789", EditionNumber = 6, UpdateNumber = 3 } },
                CallbackUri = ""
            });

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

            A.CallTo(() => fakeSalesCatalogueService.PostProductVersionsAsync(A<List<ProductVersionRequest>>.Ignored))
                .Returns(salesCatalogueResponse);
            
            var exchangeSetResponse = GetExchangeSetResponse();

            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<RequestedProductsNotReturned>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.InternalServerError;

            A.CallTo(() => fakeFileShareService.CreateBatch()).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            {
                ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest {
                ProductName = "GB123789", EditionNumber = 6, UpdateNumber = 3 } },
                CallbackUri = ""
            });

            Assert.IsNull(result.ExchangeSetResponse);
            Assert.AreEqual(HttpStatusCode.InternalServerError, result.HttpStatusCode);
        }

        #endregion

        #region ProductDataSinceDateTime

        [Test]
        public async Task WhenInvalidSinceDateTimeInRequest_ThenValidateProductDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                            { new ValidationFailure("SinceDateTime", "Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format' (e.g. 'Wed, 21 Oct 2020 07:28:00 GMT').")}));

            var result = await service.ValidateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format' (e.g. 'Wed, 21 Oct 2020 07:28:00 GMT').", result.Errors.Single().ErrorMessage);
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
                            { new ValidationFailure("callbackUrl", "Invalid callbackUri format.")}));

            var result = await service.ValidateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Invalid callbackUri format.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenValidateProductDataSinceDateTimeInRequest_ThenCreateProductDataSinceDateTimeReturnSuccess()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

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
            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
        }

        [Test]
        public async Task WhenValidateProductDataSinceDateTimeInRequest_ThenCreateProductDataSinceDateTimeReturnOk()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.OK;
            salesCatalogueResponse.LastModified = DateTime.UtcNow;

            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var exchangeSetResponse = GetExchangeSetResponse();

            A.CallTo(() => fakeMapper.Map<ExchangeSetResponse>(A<ProductCounts>.Ignored)).Returns(exchangeSetResponse);
            A.CallTo(() => fakeMapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(A<RequestedProductsNotReturned>.Ignored))
                .Returns(exchangeSetResponse.RequestedProductsNotInExchangeSet);
            
            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.Created;

            A.CallTo(() => fakeFileShareService.CreateBatch()).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.IsInstanceOf<ExchangeSetServiceResponse>(result);
            Assert.AreEqual(exchangeSetResponse.Links.ExchangeSetBatchStatusUri, result.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri);
            Assert.AreEqual(exchangeSetResponse.Links.ExchangeSetFileUri, result.ExchangeSetResponse.Links.ExchangeSetFileUri);
            Assert.AreEqual(exchangeSetResponse.ExchangeSetUrlExpiryDateTime, result.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime);
        }

        [Test]
        public async Task WhenInvalidFSSCreateBatchProductDataSinceDateTimeInRequest_ThenCreateProductDataSinceDateTimeReturnInternalServerError()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            
            var salesCatalogueResponse = GetSalesCatalogueResponse();
            salesCatalogueResponse.ResponseCode = HttpStatusCode.InternalServerError;
            
            A.CallTo(() => fakeSalesCatalogueService.GetProductsFromSpecificDateAsync(A<string>.Ignored))
                .Returns(salesCatalogueResponse);

            var CreateBatchResponseModel = CreateBatchResponse();
            CreateBatchResponseModel.ResponseCode = HttpStatusCode.InternalServerError;

            A.CallTo(() => fakeFileShareService.CreateBatch()).Returns(CreateBatchResponseModel);

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.IsNull(result.ExchangeSetResponse);
            Assert.AreEqual(HttpStatusCode.InternalServerError, result.HttpStatusCode);
        }

        #endregion ProductDataSinceDateTime
    }
}
