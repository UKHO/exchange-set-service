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
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;


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
            Links links = new Links()
            {
                ExchangeSetBatchStatusUri = linkSetBatchStatusUri,
                ExchangeSetBatchDetailsUri = linkSetBatchDetailsUri,
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

        #region PostProductIdentifiers

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenInvalidProductIdentifiersRequest_ThenPostProductIdentifiersReturnsBadRequest(bool         isUnencrypted)
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

            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri, isUnencrypted);
            var errors = (ErrorDescription)result.Value;
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Product Identifiers cannot be null or empty.", errors.Errors.Single().Description);

        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenNullProductIdentifiersRequest_ThenPostProductIdentifiersReturnsBadRequest(bool            isUnencrypted)
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

            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(null, null, isUnencrypted);
            var errors = (ErrorDescription)result.Value;
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Either body is null or malformed.", errors.Errors.Single().Description);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenEmptyProductIdentifiersRequest_ThenPostProductIdentifiersReturnsBadRequest(bool           isUnencrypted)
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

            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri, isUnencrypted);
            var errors = (ErrorDescription)result.Value;
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Either body is null or malformed.", errors.Errors.Single().Description);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenInvalidProductIdentifiersRequest_ThenPostProductIdentifiersReturnsInternalServerError(
             bool isUnencrypted)
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

            var result = (ObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri, isUnencrypted);
            Assert.AreSame("Internal Server Error", ((UKHO.ExchangeSetService.Common.Models.Response.InternalServerError)result.Value).Detail);
            Assert.AreEqual(500, result.StatusCode);

        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenInvalidProductIdentifiersRequest_ThenPostProductIdentifiersReturnsNotModified(bool        isUnencrypted)
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

            var result = (StatusCodeResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri, isUnencrypted);
            Assert.AreEqual(304, result.StatusCode);

        }

        [Test]
        [TestCase(false)]
        public async Task WhenLargeExchangeSetRequested_ThenPostProductIdentifiersReturnsBadRequest(bool                isUnencrypted)
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

            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri, isUnencrypted);
            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("The Exchange Set requested is very large and will not be created, please use a standard Exchange Set provided by the UKHO.", errors.Errors.Single().Description);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidProductIdentifiersRequest_ThenPostProductIdentifiersReturnsOkObjectResultCreated     (bool isUnencrypted)
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

            var result = (OkObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri, isUnencrypted);

            Assert.AreEqual(exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetCellCount, ((UKHO.ExchangeSetService.Common.Models.Response.ExchangeSetResponse)result.Value).ExchangeSetCellCount);

        }

        #endregion PostProductIdentifiers

        #region ProductVersions

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenInvalidProductVersionRequest_ThenPostProductDataByProductVersionsReturnsBadRequest(bool   isUnencrypted)
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
              new List<ProductVersionRequest>() { new ProductVersionRequest() { ProductName = null } }, "", isUnencrypted);

            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("productName cannot be blank or null.", errors.Errors.Single().Description);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenInvalidNullProductVersionRequest_ThenPostProductDataByProductVersionsReturnsBadRequest      (bool isUnencrypted)
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

            var result = (BadRequestObjectResult)await controller.PostProductDataByProductVersions(new List<ProductVersionRequest>(), "", isUnencrypted);
            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Either body is null or malformed.", errors.Errors.Single().Description);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenInvalidProductVersionRequest_ThenPostProductDataByProductVersionsReturnsInternalServerError
            (bool isUnencrypted)
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
                            { new ProductVersionRequest() { ProductName = "demo" } }, "", isUnencrypted);

            Assert.AreSame("Internal Server Error", ((UKHO.ExchangeSetService.Common.Models.Response.InternalServerError)result.Value).Detail);
            Assert.AreEqual(500, result.StatusCode);

        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenInvalidProductVersionRequest_ThenPostProductDataByProductVersionsReturnsNotModified        (bool isUnencrypted)
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
                            { new ProductVersionRequest() { ProductName = "demo" } }, "", isUnencrypted);
            Assert.AreEqual(304, result.StatusCode);

        }

        [Test]
        [TestCase(false)]
        public async Task WhenLargeExchangeSetRequested_ThenPostProductDataByProductVersionsReturnsBadRequest(bool      isUnencrypted)
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
                            { new ProductVersionRequest() { ProductName = "demo" } }, "", isUnencrypted);
            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("The Exchange Set requested is very large and will not be created, please use a standard Exchange Set provided by the UKHO.", errors.Errors.Single().Description);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidProductVersionRequest_ThenPostProductDataByProductVersionsReturnsOkResponse(
             bool isUnencrypted)
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
                            { new ProductVersionRequest() { ProductName = "demo" } }, "", isUnencrypted);

            Assert.AreSame(exchangeSetServiceResponse.ExchangeSetResponse, result.Value);
        }

        #endregion

        #region ProductDataSinceDateTime

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenEmptySinceDateTimeInRequest_ThenGetProductDataSinceDateTimeReturnsBadRequest(bool         isUnencrypted)
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

            var result = (BadRequestObjectResult)await controller.GetProductDataSinceDateTime(null, "https://www.abc.com", isUnencrypted);
            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Query parameter 'sinceDateTime' is required.", errors.Errors.Single().Description);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenInvalidSinceDateTimeInRequest_ThenGetProductDataSinceDateTimeReturnsInternalServerError   (bool isUnencrypted)
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

            var result = (ObjectResult)await controller.GetProductDataSinceDateTime("Wed, 21 Oct 2015 07:28:00 GMT", "https://www.abc.com", isUnencrypted);

            Assert.AreSame("Internal Server Error", ((UKHO.ExchangeSetService.Common.Models.Response.InternalServerError)result.Value).Detail);
            Assert.AreEqual(500, result.StatusCode);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenSinceDateTimeInRequest_ThenGetProductDataSinceDateTimeReturnsNotModified(bool             isUnencrypted)
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

            var result = (StatusCodeResult)await controller.GetProductDataSinceDateTime("Wed, 21 Oct 2015 07:28:00 GMT", "https://www.abc.com", isUnencrypted);
            Assert.AreEqual(304, result.StatusCode);
        }

        [Test]
        [TestCase(false)]
        public async Task WhenLargeExchangeSetRequested_ThenGetProductDataSinceDateTimeReturnBadRequest(bool            isUnencrypted)
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

            var result = (BadRequestObjectResult)await controller.GetProductDataSinceDateTime("Wed, 21 Oct 2015 07:28:00 GMT", "https://www.abc.com", isUnencrypted);
            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("The Exchange Set requested is very large and will not be created, please use a standard Exchange Set provided by the UKHO.", errors.Errors.Single().Description);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidRequest_ThenGetProductDataSinceDateTimeReturnSuccess(
             bool isUnencrypted)
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

            var result = (OkObjectResult)await controller.GetProductDataSinceDateTime("Wed, 21 Oct 2015 07:28:00 GMT", "https://www.abc.com", isUnencrypted);

            Assert.AreEqual(exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetCellCount, ((UKHO.ExchangeSetService.Common.Models.Response.ExchangeSetResponse)result.Value).ExchangeSetCellCount);
            Assert.AreEqual(200, result.StatusCode);
        }

        #endregion ProductDataSinceDateTime
    }
}
