﻿using FakeItEasy;
using FluentAssertions;
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
            Assert.That(400, Is.EqualTo(result.StatusCode));
            Assert.That("Product Identifiers cannot be null or empty.", Is.EqualTo(errors.Errors.Single().Description));
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
            Assert.That(400, Is.EqualTo(result.StatusCode));
            Assert.That("Either body is null or malformed.", Is.EqualTo(errors.Errors.Single().Description));
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
            Assert.That(400, Is.EqualTo(result.StatusCode));
            Assert.That("requestBody", Is.EqualTo(errors.Errors.Single().Source));
            Assert.That("Either body is null or malformed.", Is.EqualTo(errors.Errors.Single().Description));
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
            
            Assert.That("Internal Server Error", Is.SameAs(((UKHO.ExchangeSetService.Common.Models.Response.InternalServerError)result.Value).Detail));

            Assert.That(500, Is.EqualTo(result.StatusCode));
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
            Assert.That(304, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task WhenLargeExchangeSetRequestedAndExchangeSetStandardIsS57_ThenPostProductIdentifiersReturnsBadRequest()
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

            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri, ExchangeSetStandard.s57.ToString());
            var errors = (ErrorDescription)result.Value;

            result.StatusCode.Should().Be(400);
            errors.Errors.Single().Description.Should().Be("The Exchange Set requested is very large and will not be created, please use a standard Exchange Set provided by the UKHO.");

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ExchangeSetTooLarge.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Requested exchange set is too large for product identifiers endpoint for _X-Correlation-ID:{correlationId}").MustHaveHappened();
        }

        [Test]
        public async Task WhenLargeExchangeSetRequestedAndExchangeSetStandardIsS63_ThenPostProductIdentifiersReturnsOkObjectResultAndExchangeSetIsCreated()
        {
            var exchangeSetResponse = GetExchangeSetResponse();
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                IsExchangeSetTooLarge = false,
                HttpStatusCode = HttpStatusCode.OK
            };

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
                 .Returns(new ValidationResult(new List<ValidationFailure>()));

            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored, A<AzureAdB2C>.Ignored))
                 .Returns(exchangeSetServiceResponse);

            var result = (OkObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri, ExchangeSetStandard.s63.ToString());

            result.StatusCode.Should().Be(200);
            ((UKHO.ExchangeSetService.Common.Models.Response.ExchangeSetResponse)result.Value).ExchangeSetCellCount.Should().Be(exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetCellCount);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ESSPostProductIdentifiersRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Product Identifiers Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ESSPostProductIdentifiersRequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Product Identifiers Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappenedOnceOrLess();
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

            Assert.That(exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetCellCount, Is.EqualTo(((UKHO.ExchangeSetService.Common.Models.Response.ExchangeSetResponse)result.Value).ExchangeSetCellCount));

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

            Assert.That(400, Is.EqualTo(result.StatusCode));
            Assert.That("productName cannot be blank or null.", Is.EqualTo(errors.Errors.Single().Description));
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

            Assert.That(400, Is.EqualTo(result.StatusCode));
            Assert.That("requestBody", Is.EqualTo(errors.Errors.Single().Source));
            Assert.That("Either body is null or malformed.", Is.EqualTo(errors.Errors.Single().Description));
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

            Assert.That("Internal Server Error", Is.SameAs(((UKHO.ExchangeSetService.Common.Models.Response.InternalServerError)result.Value).Detail));
            Assert.That(500, Is.EqualTo(result.StatusCode));
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
            Assert.That(304, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task WhenLargeExchangeSetRequestedAndExchangeSetStandardIsS57_ThenPostProductDataByProductVersionsReturnsBadRequest()
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
                            { new() { ProductName = "demo" } }, "", ExchangeSetStandard.s57.ToString());
            var errors = (ErrorDescription)result.Value;

            result.StatusCode.Should().Be(400);
            errors.Errors.Single().Description.Should().Be("The Exchange Set requested is very large and will not be created, please use a standard Exchange Set provided by the UKHO.");

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ExchangeSetTooLarge.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Requested exchange set is too large for product versions endpoint for _X-Correlation-ID:{correlationId}.").MustHaveHappened();
        }

        [Test]
        public async Task WhenLargeExchangeSetRequestedAndExchangeSetStandardIsS63_ThenPostProductDataByProductVersionsReturnsOkObjectResultAndExchangeSetIsCreated()
        {
            var exchangeSetResponse = GetExchangeSetResponse();
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                IsExchangeSetTooLarge = false,
                HttpStatusCode = HttpStatusCode.OK
            };

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure>()));

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored, A<AzureAdB2C>.Ignored))
                 .Returns(exchangeSetServiceResponse);

            var result = (OkObjectResult)await controller.PostProductDataByProductVersions(new List<ProductVersionRequest>()
                            { new() { ProductName = "demo" } }, "", ExchangeSetStandard.s63.ToString());

            result.StatusCode.Should().Be(200);
            ((UKHO.ExchangeSetService.Common.Models.Response.ExchangeSetResponse)result.Value).ExchangeSetCellCount.Should().Be(exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetCellCount);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ESSPostProductVersionsRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Product Versions Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ESSPostProductVersionsRequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Product Versions Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappenedOnceOrLess();
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
            Assert.That(exchangeSetServiceResponse.ExchangeSetResponse, Is.SameAs(result.Value));
            
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

            Assert.That(400, Is.EqualTo(result.StatusCode));
            Assert.That("sinceDateTime", Is.EqualTo(errors.Errors.Single().Source));
            Assert.That("Query parameter 'sinceDateTime' is required.", Is.EqualTo(errors.Errors.Single().Description));
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

            Assert.That("Internal Server Error", Is.SameAs(((UKHO.ExchangeSetService.Common.Models.Response.InternalServerError)result.Value).Detail));
            Assert.That(500, Is.EqualTo(result.StatusCode));
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
            Assert.That(304, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task WhenLargeExchangeSetRequestedAndExchangeSetStandardIsS57_ThenGetProductDataSinceDateTimeReturnsBadRequest()
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

            var result = (BadRequestObjectResult)await controller.GetProductDataSinceDateTime("Wed, 21 Oct 2015 07:28:00 GMT", "https://www.abc.com", ExchangeSetStandard.s57.ToString());
            var errors = (ErrorDescription)result.Value;

            result.StatusCode.Should().Be(400);
            errors.Errors.Single().Description.Should().Be("The Exchange Set requested is very large and will not be created, please use a standard Exchange Set provided by the UKHO.");

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ExchangeSetTooLarge.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Requested exchange set is too large for SinceDateTime endpoint for _X-Correlation-ID:{correlationId}.").MustHaveHappened();
        }

        [Test]
        public async Task WhenLargeExchangeSetRequestedAndExchangeSetStandardIsS63_ThenGetProductDataSinceDateTimeReturnsOkObjectResultAndExchangeSetIsCreated()
        {
            var exchangeSetResponse = GetExchangeSetResponse();
            var exchangeSetServiceResponse = new ExchangeSetServiceResponse()
            {
                ExchangeSetResponse = exchangeSetResponse,
                IsExchangeSetTooLarge = false,
                HttpStatusCode = HttpStatusCode.OK
            };

            A.CallTo(() => fakeProductDataService.ValidateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                 .Returns(new ValidationResult(new List<ValidationFailure>()));

            A.CallTo(() => fakeProductDataService.CreateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored, A<AzureAdB2C>.Ignored))
                .Returns(exchangeSetServiceResponse);

            var result = (OkObjectResult)await controller.GetProductDataSinceDateTime("Wed, 21 Oct 2015 07:28:00 GMT", "https://www.abc.com", ExchangeSetStandard.s63.ToString());

            result.StatusCode.Should().Be(200);
            ((UKHO.ExchangeSetService.Common.Models.Response.ExchangeSetResponse)result.Value).ExchangeSetCellCount.Should().Be(exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetCellCount);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ESSGetProductsFromSpecificDateRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Product Data SinceDateTime Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappened();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ESSGetProductsFromSpecificDateRequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Product Data SinceDateTime Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappenedOnceOrLess();
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

            Assert.That(exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetCellCount, Is.EqualTo(((UKHO.ExchangeSetService.Common.Models.Response.ExchangeSetResponse)result.Value).ExchangeSetCellCount));
            Assert.That(200, Is.EqualTo(result.StatusCode));

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ESSGetProductsFromSpecificDateRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Product Data SinceDateTime Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappened();
        }

        #endregion ProductDataSinceDateTime
    }
}
