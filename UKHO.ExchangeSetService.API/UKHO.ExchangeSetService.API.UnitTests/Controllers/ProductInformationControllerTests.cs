using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FakeItEasy;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Controllers;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.API.UnitTests.Controllers
{
    [TestFixture]
    public class ProductInformationControllerTests
    {
        private ProductInformationController controller;
        private IHttpContextAccessor fakeHttpContextAccessor;
        private IProductDataService fakeProductDataService;
        private ILogger<ProductInformationController> fakeLogger;

        [SetUp]
        public void Setup()
        {
            fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            fakeProductDataService = A.Fake<IProductDataService>();
            fakeLogger = A.Fake<ILogger<ProductInformationController>>();
            A.CallTo(() => fakeHttpContextAccessor.HttpContext).Returns(new DefaultHttpContext());
            controller = new ProductInformationController(fakeHttpContextAccessor, fakeLogger, fakeProductDataService);
        }

        #region PostProductIdentifiers

        [Test]
        public async Task WhenValidateProductIdentifiersRequest_ThenPostValidateProductIdentifiersReturnsOkStatusCodeResult()
        {
            var mockSalesCatalogueResponse = GetSalesCatalogueResponse();
            var salesCatalogueResponse = new SalesCatalogueResponse()
            {
                ResponseBody = mockSalesCatalogueResponse.ResponseBody,
                ResponseCode = HttpStatusCode.OK
            };

            A.CallTo(() => fakeProductDataService.ValidateScsProductDataByProductIdentifiers(A<ScsProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ScsProductIdentifierRequest>.Ignored))
                .Returns(salesCatalogueResponse);

            var result = (OkObjectResult)await controller.PostProductIdentifiers(productIdentifiers);

            Assert.That(salesCatalogueResponse.ResponseBody.ProductCounts, Is.EqualTo(((SalesCatalogueProductResponse)result.Value).ProductCounts));

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.PostValidateProductIdentifiersRequestForScsResponseStart.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validate Product Identifiers Endpoint request for _X-Correlation-ID:{correlationId}").MustHaveHappened();
        }

        [Test]
        public async Task WhenInValidateScsProductIdentifiersRequest_ThenPostValidateProductIdentifiersReturnsBadRequestResult()
        {
            var mockSalesCatalogueResponse = GetSalesCatalogueResponse();
            var salesCatalogueResponse = new SalesCatalogueResponse()
            {
                ResponseBody = mockSalesCatalogueResponse.ResponseBody,
                ResponseCode = HttpStatusCode.BadRequest
            };

            var validationMessage = new ValidationFailure("ProductIdentifiers", "Product Identifiers cannot be null or empty.");
            validationMessage.ErrorCode = HttpStatusCode.BadRequest.ToString();

            A.CallTo(() => fakeProductDataService.ValidateScsProductDataByProductIdentifiers(A<ScsProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            string[] productIdentifiers = new string[] { "", "GB160060", "AU334550" };

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ScsProductIdentifierRequest>.Ignored))
                .Returns(salesCatalogueResponse);

            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(productIdentifiers);

            var errors = (ErrorDescription)result.Value;
            Assert.Multiple(() =>
            {
                Assert.That(result.StatusCode, Is.EqualTo(400));
                Assert.That(errors.Errors.Single().Description, Is.EqualTo("Product Identifiers cannot be null or empty."));
            });
        }

        [Test]
        public async Task WhenNullScsProductIdentifiersRequest_ThenPostProductIdentifiersReturnsBadRequest()
        {
            var mockSalesCatalogueResponse = GetSalesCatalogueResponse();
            var salesCatalogueResponse = new SalesCatalogueResponse()
            {
                ResponseBody = mockSalesCatalogueResponse.ResponseBody,
                ResponseCode = HttpStatusCode.BadRequest
            };

            var validationMessage = new ValidationFailure("RequestBody", "Either body is null or malformed.");
            validationMessage.ErrorCode = HttpStatusCode.BadRequest.ToString();

            A.CallTo(() => fakeProductDataService.ValidateScsProductDataByProductIdentifiers(A<ScsProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ScsProductIdentifierRequest>.Ignored))
                .Returns(salesCatalogueResponse);

            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(null);
            var errors = (ErrorDescription)result.Value;
            Assert.Multiple(() =>
            {
                Assert.That(result.StatusCode, Is.EqualTo(400));
                Assert.That(errors.Errors.Single().Description, Is.EqualTo("Either body is null or malformed."));
            });
        }

        [Test]
        public async Task WhenScsDoesNotRespond200OK_ThenPostProductIdentifiersReturnsInternalServerError()
        {
            var mockSalesCatalogueResponse = GetSalesCatalogueResponse();
            var salesCatalogueResponse = new SalesCatalogueResponse()
            {
                ResponseBody = mockSalesCatalogueResponse.ResponseBody,
                ResponseCode = HttpStatusCode.Unauthorized
            };

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ScsProductIdentifierRequest>.Ignored))
                .Returns(salesCatalogueResponse);

            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };

            var result = (ObjectResult)await controller.PostProductIdentifiers(productIdentifiers);
            Assert.Multiple(() =>
            {
                Assert.That(((InternalServerError)result.Value).Detail, Is.SameAs("Internal Server Error"));
                Assert.That(result.StatusCode, Is.EqualTo(500));
            });
        }

        #endregion PostProductIdentifiers

        #region GetScsResponsebySinceDateTime

        [Test]
        public async Task WhenValidRequest_ThenGetProductDataSinceDateTimeShouldReturnSuccess()
        {
            var salesCatalogueResponse = GetSalesCatalogueResponse();


            A.CallTo(() => fakeProductDataService.ValidateScsDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            A.CallTo(() => fakeProductDataService.GetProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(salesCatalogueResponse);

            var result = (OkObjectResult)await controller.GetProductInformationSinceDateTime("Wed, 21 Oct 2015 07:28:00 GMT");

            Assert.That(result.StatusCode, Is.EqualTo(200));

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.SCSGetProductDataSinceDateTimeRequestStart.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SCS Product Data SinceDateTime Endpoint request for _X-Correlation-ID:{correlationId} ").MustHaveHappened();
        }

        [Test]
        public async Task WhenInvalidSinceDateTimeFormatInRequest_ThenGetProductDataSinceDateTimeShouldReturnBadRequest()
        {
            var validationMessage = new ValidationFailure("SinceDateTime", "Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format' (e.g. 'Wed, 21 Oct 2020 07:28:00 GMT').")
            {
                ErrorCode = HttpStatusCode.BadRequest.ToString()
            };

            var salesCatalogueResponse = new SalesCatalogueResponse();

            A.CallTo(() => fakeProductDataService.ValidateScsDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.GetProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(salesCatalogueResponse);

            var result = (BadRequestObjectResult)await controller.GetProductInformationSinceDateTime("Fri, 8 Mar 2024");
            var errors = (ErrorDescription)result.Value;

            Assert.Multiple(() =>
            {
                Assert.That(result.StatusCode, Is.EqualTo(400));
                Assert.That(errors.Errors.Single().Source, Is.EqualTo("sinceDateTime"));
                Assert.That(errors.Errors.Single().Description, Is.EqualTo("Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format' (e.g. 'Wed, 21 Oct 2020 07:28:00 GMT')."));
            });
        }

        [Test]
        public async Task WhenEmptySinceDateTimeInRequest_ThenGetProductDataSinceDateTimeShouldReturnBadRequest()
        {
            var result = (BadRequestObjectResult)await controller.GetProductInformationSinceDateTime(null);
            var errors = (ErrorDescription)result.Value;

            Assert.Multiple(() =>
            {
                Assert.That(result.StatusCode, Is.EqualTo(400));
                Assert.That(errors.Errors.Single().Source, Is.EqualTo("sinceDateTime"));
                Assert.That(errors.Errors.Single().Description, Is.EqualTo("Query parameter 'sinceDateTime' is required."));
            });
        }

        [Test]
        public async Task WhenScsDoesNotRespond200OK_ThenGetSinceDateTimeReturnsInternalServerError()
        {

            var mockSalesCatalogueResponse = GetSalesCatalogueResponse();
            var salesCatalogueResponse = new SalesCatalogueResponse()
            {
                ResponseBody = mockSalesCatalogueResponse.ResponseBody,
                ResponseCode = HttpStatusCode.Unauthorized
            };

            A.CallTo(() => fakeProductDataService.GetProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(salesCatalogueResponse);

            var result = (ObjectResult)await controller.GetProductInformationSinceDateTime("Fri, 22 Mar 2024");
            Assert.Multiple(() =>
            {
                Assert.That(((InternalServerError)result.Value).Detail, Is.SameAs("Internal Server Error"));
                Assert.That(result.StatusCode, Is.EqualTo(500));
            });
        }

        #endregion GetScsResponsebySinceDateTime

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
    }
}
