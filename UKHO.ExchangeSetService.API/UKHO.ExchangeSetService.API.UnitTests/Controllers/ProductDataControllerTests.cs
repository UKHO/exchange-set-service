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
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
            };
            LinkSetFileUri linkSetFileUri = new LinkSetFileUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip",
            };
            Links links = new Links()
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

        #region PostProductIdentifiers

        [Test]
        public async Task WhenInvalidProductIdentifiersRequest_ThenPostProductIdentifiersReturnsBadRequest()
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

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
              .Returns(exchangeSetServiceResponse);
          
            string[] productIdentifiers = new string[] { "", "GB160060", "AU334550" };
            string callbackUri = string.Empty;

            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri);
            var errors = (ErrorDescription)result.Value;
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Product Identifiers cannot be null or empty.", errors.Errors.Single().Description);

        }

        [Test]
        public async Task WhenNullProductIdentifiersRequest_ThenPostProductIdentifiersReturnsBadRequest()
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

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
              .Returns(exchangeSetServiceResponse);
           
            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(null, null);
            var errors = (ErrorDescription)result.Value;
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Either body is null or malformed.", errors.Errors.Single().Description);
        }


        [Test]
        public async Task WhenEmptyProductIdentifiersRequest_ThenPostProductIdentifiersReturnsBadRequest()
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

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
              .Returns(exchangeSetServiceResponse);

            string[] productIdentifiers = new string[] { };
            string callbackUri = string.Empty;

            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri);
            var errors = (ErrorDescription)result.Value;
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Either body is null or malformed.", errors.Errors.Single().Description);
        }

        [Test]
        public async Task WhenInvalidProductIdentifiersRequest_ThenPostProductIdentifiersReturnsInternalServerError()
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

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
              .Returns(exchangeSetServiceResponse);

            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;

            var result = (ObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri);
            Assert.AreSame("Internal Server Error", ((UKHO.ExchangeSetService.Common.Models.Response.InternalServerError)result.Value).Detail);          
            Assert.AreEqual(500, result.StatusCode);

        }

        [Test]
       public async Task WhenInvalidProductIdentifiersRequest_ThenPostProductIdentifiersReturnsNotModified()
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

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
              .Returns(exchangeSetServiceResponse);

            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;

            var result = (StatusCodeResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri);           
            Assert.AreEqual(304, result.StatusCode);

        }

        [Test]
        public async Task WhenValidProductIdentifiersRequest_ThenPostProductIdentifiersReturnsOkObjectResultCreated()
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

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
                 .Returns(exchangeSetServiceResponse);

            var result = (OkObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri);

            Assert.AreEqual(exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetCellCount, ((UKHO.ExchangeSetService.Common.Models.Response.ExchangeSetResponse)result.Value).ExchangeSetCellCount);

        }
        #endregion PostProductIdentifiers

        #region ProductVersions

        [Test]
        public async Task WhenInvalidProductVersionRequest_ThenPostProductDataByProductVersionsReturnsBadRequest()
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

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(exchangeSetServiceResponse);

            var result = (BadRequestObjectResult)await controller.PostProductDataByProductVersions(
              new List<ProductVersionRequest>() { new ProductVersionRequest() { ProductName = null } }, "");

            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("productName cannot be blank or null.", errors.Errors.Single().Description);
        }

        [Test]
        public async Task WhenInvalidNullProductVersionRequest_ThenPostProductDataByProductVersionsReturnsBadRequest()

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

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(exchangeSetServiceResponse);

            var result = (BadRequestObjectResult)await controller.PostProductDataByProductVersions(new List<ProductVersionRequest>(), "");
            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Either body is null or malformed.", errors.Errors.Single().Description);
        }

        [Test]
        public async Task WhenInvalidProductVersionRequest_ThenPostProductDataByProductVersionsReturnsInternalServerError()
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

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(exchangeSetServiceResponse);

            var result = (ObjectResult)await controller.PostProductDataByProductVersions(new List<ProductVersionRequest>()
                            { new ProductVersionRequest() { ProductName = "demo" } }, "");

            Assert.AreSame("Internal Server Error", ((UKHO.ExchangeSetService.Common.Models.Response.InternalServerError)result.Value).Detail);
            Assert.AreEqual(500, result.StatusCode);         

        }

        [Test]
        public async Task WhenInvalidProductVersionRequest_ThenPostProductDataByProductVersionsReturnsNotModified()
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

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(exchangeSetServiceResponse);

            var result = (StatusCodeResult)await controller.PostProductDataByProductVersions(new List<ProductVersionRequest>()
                            { new ProductVersionRequest() { ProductName = "demo" } }, "");
            Assert.AreEqual(304, result.StatusCode);

        }

        [Test]
        public async Task WhenValidProductVersionRequest_ThenPostProductDataByProductVersionsReturnsOkResponse()
        {
            var exchangeSetResponse = GetExchangeSetResponse();
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.OK
            };

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure>()));

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                 .Returns(exchangeSetServiceResponse);

            var result = (OkObjectResult)await controller.PostProductDataByProductVersions(new List<ProductVersionRequest>()
                            { new ProductVersionRequest() { ProductName = "demo" } }, "");

            Assert.AreSame(exchangeSetServiceResponse.ExchangeSetResponse, result.Value);
        }

        #endregion

        #region ProductDataSinceDateTime

        [Test]
        public async Task WhenEmptySinceDateTimeInRequest_ThenGetProductDataSinceDateTimeReturnsBadRequest()
        {
            var validationMessage = new ValidationFailure("sinceDateTime", "Query parameter 'sinceDateTime' is required.")
            {
                ErrorCode = HttpStatusCode.BadRequest.ToString()
            };

            var exchangeSetResponse = new ExchangeSetResponse(){  };
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.BadRequest
            };

            A.CallTo(() => fakeProductDataService.ValidateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.CreateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
               .Returns(exchangeSetServiceResponse);          

            var result = (BadRequestObjectResult)await controller.GetProductDataSinceDateTime(null, "https://www.abc.com");
            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Query parameter 'sinceDateTime' is required.", errors.Errors.Single().Description);
        }

        [Test]
        public async Task WhenInvalidSinceDateTimeInRequest_ThenGetProductDataSinceDateTimeReturnsInternalServerError()
        {
            var exchangeSetResponse = new ExchangeSetResponse() { };
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.InternalServerError
            };

            var validationMessage = new ValidationFailure("sinceDateTime", "Internal Server Error");
            validationMessage.ErrorCode = HttpStatusCode.InternalServerError.ToString();

            A.CallTo(() => fakeProductDataService.ValidateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.CreateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
               .Returns(exchangeSetServiceResponse);

            var result = (ObjectResult)await controller.GetProductDataSinceDateTime("Wed, 21 Oct 2015 07:28:00 GMT", "https://www.abc.com");

            Assert.AreSame("Internal Server Error", ((UKHO.ExchangeSetService.Common.Models.Response.InternalServerError)result.Value).Detail);
            Assert.AreEqual(500, result.StatusCode);
        }

        [Test]
        public async Task WhenSinceDateTimeInRequest_ThenGetProductDataSinceDateTimeReturnsNotModified()
        {
            var exchangeSetResponse = new ExchangeSetResponse() { };
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.NotModified
            };

            var validationMessage = new ValidationFailure("sinceDateTime", "NotModified.");
            validationMessage.ErrorCode = HttpStatusCode.NotModified.ToString();

            A.CallTo(() => fakeProductDataService.ValidateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            A.CallTo(() => fakeProductDataService.CreateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
               .Returns(exchangeSetServiceResponse);

            var result = (StatusCodeResult)await controller.GetProductDataSinceDateTime("Wed, 21 Oct 2015 07:28:00 GMT", "https://www.abc.com");
            Assert.AreEqual(304, result.StatusCode);
        }

        [Test]
        public async Task WhenValidRequest_ThenGetProductDataSinceDateTimeReturnSuccess()
        {
            var exchangeSetResponse = GetExchangeSetResponse();
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                HttpStatusCode = HttpStatusCode.OK
            };

            A.CallTo(() => fakeProductDataService.ValidateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                 .Returns(new ValidationResult(new List<ValidationFailure>()));

            A.CallTo(() => fakeProductDataService.CreateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(exchangeSetServiceResponse);
          
            var result = (OkObjectResult)await controller.GetProductDataSinceDateTime("Wed, 21 Oct 2015 07:28:00 GMT", "https://www.abc.com");

            Assert.AreEqual(exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetCellCount, ((UKHO.ExchangeSetService.Common.Models.Response.ExchangeSetResponse)result.Value).ExchangeSetCellCount);
            Assert.AreEqual(200, result.StatusCode);
        }

        #endregion ProductDataSinceDateTime
    }
}
