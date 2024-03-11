using FakeItEasy;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Controllers;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.API.UnitTests.Controllers
{
    [TestFixture]
    public class ProductDataControllerTests
    {
        private ProductDataController controller;
        private IHttpContextAccessor fakeHttpContextAccessor;
        private IProductDataService fakeProductDataService;
        private ILogger<ProductDataController> fakeLogger;
        public const string errorMessage = "Either body is null or malformed";

        [SetUp]
        public void Setup()
        {
            fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            fakeProductDataService = A.Fake<IProductDataService>();
            fakeLogger = A.Fake<ILogger<ProductDataController>>();
            A.CallTo(() => fakeHttpContextAccessor.HttpContext).Returns(new DefaultHttpContext());

            controller = new ProductDataController(fakeHttpContextAccessor, fakeLogger, fakeProductDataService);
        }

        #region GetExchangeSetResponse

        private ExchangeSetResponse GetExchangeSetResponse()
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
            Links links = new()
            {
                ExchangeSetBatchStatusUri = linkSetBatchStatusUri,
                ExchangeSetBatchDetailsUri = linkSetBatchDetailsUri,
                ExchangeSetFileUri = linkSetFileUri
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
                    ProductName = "GB123789",
                    Reason = "invalidProduct"
                }
            };
            ExchangeSetResponse exchangeSetResponse = new()
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

        #region PostProductIdentifiers

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenInvalidProductIdentifiersRequest_ThenPostProductIdentifiersReturnsBadRequest(ExchangeSetStandard exchangeSetStandard)
        {
            var exchangeSetResponse = new ExchangeSetResponse() { };
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.BadRequest
            };

            var validationMessage = new ValidationFailure("ProductIdentifiers", "Product Identifiers cannot be null or empty.");
            validationMessage.ErrorCode = HttpStatusCode.BadRequest.ToString();

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored, A<AzureAdB2C>.Ignored))
              .Returns(exchangeSetServiceResponse);

            string[] productIdentifiers = new string[] { "", "GB160060", "AU334550" };
            string callbackUri = string.Empty;

            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri, exchangeSetStandard.ToString().ToString());
            var errors = (ErrorDescription)result.Value;
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Product Identifiers cannot be null or empty.", errors.Errors.Single().Description);
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenNullProductIdentifiersRequest_ThenPostProductIdentifiersReturnsBadRequest(ExchangeSetStandard exchangeSetStandard)
        {
            var exchangeSetResponse = new ExchangeSetResponse() { };
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.BadRequest
            };

            var validationMessage = new ValidationFailure("RequestBody", "Either body is null or malformed.");
            validationMessage.ErrorCode = HttpStatusCode.BadRequest.ToString();

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored, A<AzureAdB2C>.Ignored))
              .Returns(exchangeSetServiceResponse);

            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(null, null, exchangeSetStandard.ToString());
            var errors = (ErrorDescription)result.Value;
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Either body is null or malformed.", errors.Errors.Single().Description);
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenEmptyProductIdentifiersRequest_ThenPostProductIdentifiersReturnsBadRequest(ExchangeSetStandard exchangeSetStandard)
        {
            var exchangeSetResponse = new ExchangeSetResponse() { };
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = (HttpStatusCode)(int)HttpStatusCode.BadRequest
            };

            var validationMessage = new ValidationFailure("RequestBody", "Either body is null or malformed.");
            validationMessage.ErrorCode = HttpStatusCode.BadRequest.ToString();

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored, A<AzureAdB2C>.Ignored))
              .Returns(exchangeSetServiceResponse);

            string[] productIdentifiers = new string[] { };
            string callbackUri = string.Empty;

            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri, exchangeSetStandard.ToString());
            var errors = (ErrorDescription)result.Value;
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("requestBody", errors.Errors.Single().Source);
            Assert.AreEqual("Either body is null or malformed.", errors.Errors.Single().Description);
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenInvalidProductIdentifiersRequest_ThenPostProductIdentifiersReturnsInternalServerError(ExchangeSetStandard exchangeSetStandard)
        {
            var exchangeSetResponse = new ExchangeSetResponse() { };
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.InternalServerError
            };

            var validationMessage = new ValidationFailure("ProductIdentifiers", "Internal Server Error.");
            validationMessage.ErrorCode = HttpStatusCode.InternalServerError.ToString();

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored, A<AzureAdB2C>.Ignored))
              .Returns(exchangeSetServiceResponse);

            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;

            var result = (ObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri, exchangeSetStandard.ToString());
            Assert.AreSame("Internal Server Error", ((UKHO.ExchangeSetService.Common.Models.Response.InternalServerError)result.Value).Detail);
            Assert.AreEqual(500, result.StatusCode);
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenInvalidProductIdentifiersRequest_ThenPostProductIdentifiersReturnsNotModified(ExchangeSetStandard exchangeSetStandard)
        {
            var exchangeSetResponse = new ExchangeSetResponse() { };
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.NotModified
            };

            var validationMessage = new ValidationFailure("ProductIdentifiers", "NotModified.");
            validationMessage.ErrorCode = HttpStatusCode.NotModified.ToString();

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored, A<AzureAdB2C>.Ignored))
              .Returns(exchangeSetServiceResponse);

            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;

            var result = (StatusCodeResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri, exchangeSetStandard.ToString());
            Assert.AreEqual(304, result.StatusCode);
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenLargeExchangeSetRequested_ThenPostProductIdentifiersReturnsBadRequest(ExchangeSetStandard exchangeSetStandard)
        {
            var exchangeSetResponse = GetExchangeSetResponse();
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                IsExchangeSetTooLarge = true,
                HttpStatusCode = HttpStatusCode.BadRequest
            };

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
                 .Returns(new ValidationResult(new List<ValidationFailure>()));

            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored, A<AzureAdB2C>.Ignored))
                 .Returns(exchangeSetServiceResponse);

            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri, exchangeSetStandard.ToString());
            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("The Exchange Set requested is very large and will not be created, please use a standard Exchange Set provided by the UKHO.", errors.Errors.Single().Description);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ExchangeSetTooLarge.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Requested exchange set is too large for product identifiers endpoint for _X-Correlation-ID:{correlationId}").MustHaveHappened();
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenValidProductIdentifiersRequest_ThenPostProductIdentifiersReturnsOkObjectResultCreated(ExchangeSetStandard exchangeSetStandard)
        {
            var exchangeSetResponse = GetExchangeSetResponse();
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.OK
            };

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
                 .Returns(new ValidationResult(new List<ValidationFailure>()));

            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored, A<AzureAdB2C>.Ignored))
                 .Returns(exchangeSetServiceResponse);

            var result = (OkObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri, exchangeSetStandard.ToString());

            Assert.AreEqual(exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetCellCount, ((UKHO.ExchangeSetService.Common.Models.Response.ExchangeSetResponse)result.Value).ExchangeSetCellCount);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ESSPostProductIdentifiersRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Product Identifiers Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappened();
        }

        #endregion PostProductIdentifiers

        #region ProductVersions

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenInvalidProductVersionRequest_ThenPostProductDataByProductVersionsReturnsBadRequest(ExchangeSetStandard exchangeSetStandard)
        {
            var validationMessage = new ValidationFailure("productName", "productName cannot be blank or null.")
            {
                ErrorCode = HttpStatusCode.BadRequest.ToString()
            };

            var exchangeSetResponse = new ExchangeSetResponse() { };
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.BadRequest
            };

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored, A<AzureAdB2C>.Ignored))
                .Returns(exchangeSetServiceResponse);

            var result = (BadRequestObjectResult)await controller.PostProductDataByProductVersions(
              new List<ProductVersionRequest>() { new ProductVersionRequest() { ProductName = null } }, "", exchangeSetStandard.ToString());

            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("productName cannot be blank or null.", errors.Errors.Single().Description);
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenInvalidNullProductVersionRequest_ThenPostProductDataByProductVersionsReturnsBadRequest(ExchangeSetStandard exchangeSetStandard)
        {
            var validationMessage = new ValidationFailure("RequestBody", "Either body is null or malformed.")
            {
                ErrorCode = HttpStatusCode.BadRequest.ToString()
            };

            var exchangeSetResponse = new ExchangeSetResponse() { };
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.BadRequest
            };

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored, A<AzureAdB2C>.Ignored))
                .Returns(exchangeSetServiceResponse);

            var result = (BadRequestObjectResult)await controller.PostProductDataByProductVersions(new List<ProductVersionRequest>(), "", exchangeSetStandard.ToString());
            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("requestBody", errors.Errors.Single().Source);
            Assert.AreEqual("Either body is null or malformed.", errors.Errors.Single().Description);
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenInvalidProductVersionRequest_ThenPostProductDataByProductVersionsReturnsInternalServerError(ExchangeSetStandard exchangeSetStandard)
        {
            var exchangeSetResponse = new ExchangeSetResponse() { };
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.InternalServerError
            };

            var validationMessage = new ValidationFailure("ProductVersions", "Internal Server Error.");
            validationMessage.ErrorCode = HttpStatusCode.InternalServerError.ToString();

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                             .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored, A<AzureAdB2C>.Ignored))
                .Returns(exchangeSetServiceResponse);

            var result = (ObjectResult)await controller.PostProductDataByProductVersions(new List<ProductVersionRequest>()
                            { new ProductVersionRequest() { ProductName = "demo" } }, "", exchangeSetStandard.ToString());

            Assert.AreSame("Internal Server Error", ((UKHO.ExchangeSetService.Common.Models.Response.InternalServerError)result.Value).Detail);
            Assert.AreEqual(500, result.StatusCode);
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenInvalidProductVersionRequest_ThenPostProductDataByProductVersionsReturnsNotModified(ExchangeSetStandard exchangeSetStandard)
        {
            var exchangeSetResponse = new ExchangeSetResponse() { };
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = (HttpStatusCode)(int)HttpStatusCode.NotModified
            };

            var validationMessage = new ValidationFailure("ProductVersions", "NotModified.");
            validationMessage.ErrorCode = HttpStatusCode.NotModified.ToString();

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored, A<AzureAdB2C>.Ignored))
                .Returns(exchangeSetServiceResponse);

            var result = (StatusCodeResult)await controller.PostProductDataByProductVersions(new List<ProductVersionRequest>()
                            { new ProductVersionRequest() { ProductName = "demo" } }, "", exchangeSetStandard.ToString());
            Assert.AreEqual(304, result.StatusCode);
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenLargeExchangeSetRequested_ThenPostProductDataByProductVersionsReturnsBadRequest(ExchangeSetStandard exchangeSetStandard)
        {
            var exchangeSetResponse = GetExchangeSetResponse();
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                IsExchangeSetTooLarge = true,
                HttpStatusCode = HttpStatusCode.BadRequest
            };

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure>()));

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored, A<AzureAdB2C>.Ignored))
                 .Returns(exchangeSetServiceResponse);

            var result = (BadRequestObjectResult)await controller.PostProductDataByProductVersions(new List<ProductVersionRequest>()
                            { new ProductVersionRequest() { ProductName = "demo" } }, "", exchangeSetStandard.ToString());
            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("The Exchange Set requested is very large and will not be created, please use a standard Exchange Set provided by the UKHO.", errors.Errors.Single().Description);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ExchangeSetTooLarge.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Requested exchange set is too large for product versions endpoint for _X-Correlation-ID:{correlationId}.").MustHaveHappened();
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenValidProductVersionRequest_ThenPostProductDataByProductVersionsReturnsOkResponse(ExchangeSetStandard exchangeSetStandard)
        {
            var exchangeSetResponse = GetExchangeSetResponse();
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.OK
            };

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure>()));

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored, A<AzureAdB2C>.Ignored))
                 .Returns(exchangeSetServiceResponse);

            var result = (OkObjectResult)await controller.PostProductDataByProductVersions(new List<ProductVersionRequest>()
                            { new ProductVersionRequest() { ProductName = "demo" } }, "", exchangeSetStandard.ToString());

            Assert.AreSame(exchangeSetServiceResponse.ExchangeSetResponse, result.Value);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ESSPostProductVersionsRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Product Versions Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappened();
        }

        #endregion ProductVersions

        #region ProductDataSinceDateTime

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenEmptySinceDateTimeInRequest_ThenGetProductDataSinceDateTimeReturnsBadRequest(ExchangeSetStandard exchangeSetStandard)
        {
            var validationMessage = new ValidationFailure("SinceDateTime", "Query parameter 'sinceDateTime' is required.")
            {
                ErrorCode = HttpStatusCode.BadRequest.ToString()
            };

            var exchangeSetResponse = new ExchangeSetResponse() { };
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.BadRequest
            };

            A.CallTo(() => fakeProductDataService.ValidateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.CreateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored, A<AzureAdB2C>.Ignored))
               .Returns(exchangeSetServiceResponse);

            var result = (BadRequestObjectResult)await controller.GetProductDataSinceDateTime(null, "https://www.abc.com", exchangeSetStandard.ToString());
            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("sinceDateTime", errors.Errors.Single().Source);
            Assert.AreEqual("Query parameter 'sinceDateTime' is required.", errors.Errors.Single().Description);
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenInvalidSinceDateTimeInRequest_ThenGetProductDataSinceDateTimeReturnsInternalServerError(ExchangeSetStandard exchangeSetStandard)
        {
            var exchangeSetResponse = new ExchangeSetResponse() { };
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.InternalServerError
            };

            var validationMessage = new ValidationFailure("SinceDateTime", "Internal Server Error");
            validationMessage.ErrorCode = HttpStatusCode.InternalServerError.ToString();

            A.CallTo(() => fakeProductDataService.ValidateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.CreateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored, A<AzureAdB2C>.Ignored))
               .Returns(exchangeSetServiceResponse);

            var result = (ObjectResult)await controller.GetProductDataSinceDateTime("Wed, 21 Oct 2015 07:28:00 GMT", "https://www.abc.com", exchangeSetStandard.ToString());

            Assert.AreSame("Internal Server Error", ((UKHO.ExchangeSetService.Common.Models.Response.InternalServerError)result.Value).Detail);
            Assert.AreEqual(500, result.StatusCode);
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenSinceDateTimeInRequest_ThenGetProductDataSinceDateTimeReturnsNotModified(ExchangeSetStandard exchangeSetStandard)
        {
            var exchangeSetResponse = new ExchangeSetResponse() { };
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.NotModified
            };

            var validationMessage = new ValidationFailure("SinceDateTime", "NotModified.");
            validationMessage.ErrorCode = HttpStatusCode.NotModified.ToString();

            A.CallTo(() => fakeProductDataService.ValidateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.CreateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored, A<AzureAdB2C>.Ignored))
               .Returns(exchangeSetServiceResponse);

            var result = (StatusCodeResult)await controller.GetProductDataSinceDateTime("Wed, 21 Oct 2015 07:28:00 GMT", "https://www.abc.com", exchangeSetStandard.ToString());
            Assert.AreEqual(304, result.StatusCode);
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenLargeExchangeSetRequested_ThenGetProductDataSinceDateTimeReturnBadRequest(ExchangeSetStandard exchangeSetStandard)
        {
            var exchangeSetResponse = GetExchangeSetResponse();
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                IsExchangeSetTooLarge = true,
                HttpStatusCode = HttpStatusCode.BadRequest
            };

            A.CallTo(() => fakeProductDataService.ValidateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                 .Returns(new ValidationResult(new List<ValidationFailure>()));

            A.CallTo(() => fakeProductDataService.CreateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored, A<AzureAdB2C>.Ignored))
                .Returns(exchangeSetServiceResponse);

            var result = (BadRequestObjectResult)await controller.GetProductDataSinceDateTime("Wed, 21 Oct 2015 07:28:00 GMT", "https://www.abc.com", exchangeSetStandard.ToString());
            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("The Exchange Set requested is very large and will not be created, please use a standard Exchange Set provided by the UKHO.", errors.Errors.Single().Description);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ExchangeSetTooLarge.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Requested exchange set is too large for SinceDateTime endpoint for _X-Correlation-ID:{correlationId}.").MustHaveHappened();
        }

        [Test]
        [TestCase(ExchangeSetStandard.s63)]
        [TestCase(ExchangeSetStandard.s57)]
        public async Task WhenValidRequest_ThenGetProductDataSinceDateTimeReturnSuccess(ExchangeSetStandard exchangeSetStandard)
        {
            var exchangeSetResponse = GetExchangeSetResponse();
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.OK
            };

            A.CallTo(() => fakeProductDataService.ValidateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                 .Returns(new ValidationResult(new List<ValidationFailure>()));

            A.CallTo(() => fakeProductDataService.CreateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored, A<AzureAdB2C>.Ignored))
                .Returns(exchangeSetServiceResponse);

            var result = (OkObjectResult)await controller.GetProductDataSinceDateTime("Wed, 21 Oct 2015 07:28:00 GMT", "https://www.abc.com", exchangeSetStandard.ToString());

            Assert.AreEqual(exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetCellCount, ((UKHO.ExchangeSetService.Common.Models.Response.ExchangeSetResponse)result.Value).ExchangeSetCellCount);
            Assert.AreEqual(200, result.StatusCode);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ESSGetProductsFromSpecificDateRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Product Data SinceDateTime Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappened();
        }

        #endregion ProductDataSinceDateTime

        #region ValidatePostProductIdentifiers

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

            var result = (OkObjectResult)await controller.PostValidateProductIdentifiers(productIdentifiers);

            Assert.AreEqual(salesCatalogueResponse.ResponseBody.ProductCounts, ((SalesCatalogueProductResponse)result.Value).ProductCounts);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
                                               && call.GetArgument<LogLevel>(0) == LogLevel.Information
                                               && call.GetArgument<EventId>(1) == EventIds.PostValidateProductIdentifiersRequestForScsResponseStart.ToEventId()
                                               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validate Product Identifiers Endpoint request for _X-Correlation-ID:{correlationId}").MustHaveHappened();
        }

        [Test]
        public async Task WhenValidateProductIdentifiersRequest_ThenPostValidateProductIdentifiersReturnsBadRequestResult()
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

            var result = (BadRequestObjectResult)await controller.PostValidateProductIdentifiers(productIdentifiers);

            var errors = (ErrorDescription)result.Value;
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Product Identifiers cannot be null or empty.", errors.Errors.Single().Description);
        }

        #endregion ValidatePostProductIdentifiers

        #region GetScsResponsebySinceDateTime

        [Test]
        public async Task WhenValidRequest_ThenGetProductDataSinceDateTimeShouldReturnSuccess()
        {
            var salesCatalogueResponse = GetSalesCatalogueResponse();


            A.CallTo(() => fakeProductDataService.ValidateScsDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            A.CallTo(() => fakeProductDataService.GetProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(salesCatalogueResponse);

            var result = (OkObjectResult)await controller.GetProductDataSinceDateTime("Wed, 21 Oct 2015 07:28:00 GMT");

            Assert.AreEqual(200, result.StatusCode);

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

            var result = (BadRequestObjectResult)await controller.GetProductDataSinceDateTime("Fri, 8 Mar 2024");
            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("sinceDateTime", errors.Errors.Single().Source);
            Assert.AreEqual("Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format' (e.g. 'Wed, 21 Oct 2020 07:28:00 GMT').", errors.Errors.Single().Description);
        }



        [Test]
        public async Task WhenEmptySinceDateTimeInRequest_ThenGetProductDataSinceDateTimeShouldReturnBadRequest()
        {
            var result = (BadRequestObjectResult)await controller.GetProductDataSinceDateTime(null);
            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("sinceDateTime", errors.Errors.Single().Source);
            Assert.AreEqual("Query parameter 'sinceDateTime' is required.", errors.Errors.Single().Description);
        }


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

        #endregion GetScsResponsebySinceDateTime

    }
}