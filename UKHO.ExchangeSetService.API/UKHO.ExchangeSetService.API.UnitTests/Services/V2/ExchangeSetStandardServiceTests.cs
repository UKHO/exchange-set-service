// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Configuration;
using UKHO.ExchangeSetService.API.Services.V2;
using UKHO.ExchangeSetService.API.Validation.V2;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.V2.Request;
using ExchangeSetStandard = UKHO.ExchangeSetService.Common.Models.V2.Enums.ExchangeSetStandard;
using ISalesCatalogueService = UKHO.ExchangeSetService.Common.Helpers.V2.ISalesCatalogueService;

namespace UKHO.ExchangeSetService.API.UnitTests.Services.V2
{
    [TestFixture]
    public class ExchangeSetStandardServiceTests
    {
        private const string CallbackUri = "https://callback.uri";
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();
        private const string BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";

        private IProductNameValidator _fakeProductNameValidator;
        private ILogger<ExchangeSetStandardService> _fakeLogger;
        private IUpdatesSinceValidator _fakeUpdatesSinceValidator;
        private IProductVersionsValidator _fakeProductVersionsValidator;
        private ISalesCatalogueService _fakeSalesCatalogueService;
        private IFileShareService _fakeFileShareService;
        private UserIdentifier _fakeUserIdentifier;

        private ExchangeSetStandardService _service;

        [SetUp]
        public void Setup()
        {
            _fakeProductNameValidator = A.Fake<IProductNameValidator>();
            _fakeLogger = A.Fake<ILogger<ExchangeSetStandardService>>();
            _fakeUpdatesSinceValidator = A.Fake<IUpdatesSinceValidator>();
            _fakeProductVersionsValidator = A.Fake<IProductVersionsValidator>();
            _fakeSalesCatalogueService = A.Fake<ISalesCatalogueService>();
            _fakeFileShareService = A.Fake<IFileShareService>();
            _fakeUserIdentifier = A.Fake<UserIdentifier>();

            _service = new ExchangeSetStandardService(
                _fakeLogger,
                _fakeUpdatesSinceValidator,
                _fakeProductVersionsValidator,
                _fakeProductNameValidator,
                _fakeSalesCatalogueService,
                _fakeFileShareService,
                _fakeUserIdentifier);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new ExchangeSetStandardService(null, _fakeUpdatesSinceValidator, _fakeProductVersionsValidator, _fakeProductNameValidator, _fakeSalesCatalogueService, _fakeFileShareService, _fakeUserIdentifier);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullUpdatesSinceValidator = () => new ExchangeSetStandardService(_fakeLogger, null, _fakeProductVersionsValidator, _fakeProductNameValidator, _fakeSalesCatalogueService, _fakeFileShareService, _fakeUserIdentifier);
            nullUpdatesSinceValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("updatesSinceValidator");

            Action nullProductVersionsValidator = () => new ExchangeSetStandardService(_fakeLogger, _fakeUpdatesSinceValidator, null, _fakeProductNameValidator, _fakeSalesCatalogueService, _fakeFileShareService, _fakeUserIdentifier);
            nullProductVersionsValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productVersionsValidator");

            Action nullProductNamesValidator = () => new ExchangeSetStandardService(_fakeLogger, _fakeUpdatesSinceValidator, _fakeProductVersionsValidator, null, _fakeSalesCatalogueService, _fakeFileShareService, _fakeUserIdentifier);
            nullProductNamesValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productNameValidator");

            var nullSalesCatalogueService = () => new ExchangeSetStandardService(_fakeLogger, _fakeUpdatesSinceValidator, _fakeProductVersionsValidator, _fakeProductNameValidator, null, _fakeFileShareService, _fakeUserIdentifier);
            nullSalesCatalogueService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("salesCatalogueService");

            var nullFileShareService = () => new ExchangeSetStandardService(_fakeLogger, _fakeUpdatesSinceValidator, _fakeProductVersionsValidator, _fakeProductNameValidator, _fakeSalesCatalogueService, null, _fakeUserIdentifier);
            nullFileShareService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileShareService");

            var nullUserIdentifier = () => new ExchangeSetStandardService(_fakeLogger, _fakeUpdatesSinceValidator, _fakeProductVersionsValidator, _fakeProductNameValidator, _fakeSalesCatalogueService, _fakeFileShareService, null);
            nullUserIdentifier.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userIdentifier");
        }

        [Test]
        public async Task WhenValidSinceDateTimeRequested_ThenCreateUpdatesSinceReturns202Accepted()
        {
            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture) };

            A.CallTo(() => _fakeUpdatesSinceValidator.Validate(A<UpdatesSinceRequest>.Ignored))
                .Returns(new ValidationResult());

            var result = await _service.ProcessUpdatesSinceRequest(updatesSinceRequest, ExchangeSetStandard.s100.ToString(), "s101", CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.StatusCode.Should().Be(HttpStatusCode.Accepted);
            result.Value.Should().NotBeNull();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustNotHaveHappened();
        }

        [Test]
        public async Task WhenNullOrEmptySinceDateTimeRequested_ThenCreateUpdatesSinceReturnsBadRequest()
        {
            var result = await _service.ProcessUpdatesSinceRequest(null, ExchangeSetStandard.s100.ToString(), "s101", CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Either body is null or malformed.");

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.EmptyBodyError.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either body is null or malformed | _X-Correlation-ID : {correlationId}").MustHaveHappened();
        }

        [Test]
        [TestCase("101", "http://callback.uri")]
        [TestCase("101s", "http//callback.uri")]
        [TestCase("S101", "http:callback.uri")]
        [TestCase("s 101", "https:\\callback.uri")]
        public async Task WhenInValidDataRequested_ThenCreateUpdatesSinceReturnsBadRequest(string inValidProductIdentifier, string inValidCallBackUri)
        {
            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = DateTime.UtcNow.AddDays(-10).ToString() };

            A.CallTo(() => _fakeUpdatesSinceValidator.Validate(A<UpdatesSinceRequest>.Ignored))
                .Returns(GetValidationResult());

            var result = await _service.ProcessUpdatesSinceRequest(updatesSinceRequest, ExchangeSetStandard.s100.ToString(), inValidProductIdentifier, inValidCallBackUri, _fakeCorrelationId, CancellationToken.None);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Provided sinceDateTime is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2024-12-20T11:51:00.000Z').");
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "ProductIdentifier must be valid value");
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Invalid callbackUri format.");

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustHaveHappened();
        }

        [Test]
        public async Task WhenProductNamesAreNull_ThenShouldReturnBadRequest()
        {
            string[] productNames = null;

            var result = await _service.ProcessProductNamesRequestAsync(productNames, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Either body is null or malformed.");

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.EmptyBodyError.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either body is null or malformed | _X-Correlation-ID : {correlationId}").MustHaveHappened();
        }

        [Test]
        public async Task WhenProductNamesAreEmpty_ThenShouldReturnBadRequest()
        {
            string[] productNames = [];

            var result = await _service.ProcessProductNamesRequestAsync(productNames, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Either body is null or malformed.");

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.EmptyBodyError.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either body is null or malformed | _X-Correlation-ID : {correlationId}").MustHaveHappened();
        }

        [Test]
        public async Task WhenProductNamesValidationFails_ThenShouldReturnBadRequest()
        {
            string[] productNames = ["Product1"];

            var validationResult = new ValidationResult(new List<ValidationFailure>
                {
                    new("ProductIdentifier", "Invalid product identifier")
                    {
                        ErrorCode = HttpStatusCode.BadRequest.ToString()
                    },
                });

            A.CallTo(() => _fakeProductNameValidator.Validate(A<ProductNameRequest>.Ignored)).Returns(validationResult);

            var result = await _service.ProcessProductNamesRequestAsync(productNames, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Invalid product identifier");

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustHaveHappened();
        }

        [Test]
        public async Task WhenProductNamesValidationPasses_ThenShouldReturnSuccess()
        {
            string[] productNames = ["Product1"];

            A.CallTo(() => _fakeProductNameValidator.Validate(A<ProductNameRequest>.Ignored)).Returns(new ValidationResult());
            A.CallTo(() => _fakeSalesCatalogueService.PostProductNamesAsync(A<ApiVersion>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(ServiceResponseResult<SalesCatalogueResponse>
                .Success(GetSalesCatalogueResponse()));
            A.CallTo(() => _fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored))
                .Returns(GetCreateBatchResponse());

            var result = await _service.ProcessProductNamesRequestAsync(productNames, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.Accepted);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustNotHaveHappened();
        }

        [Test]
        public async Task WhenValidProductNamesRequest_ThenProcessProductNamesRequestAsyncReturns202Accepted()
        {
            string[] productNames = { "Product1", "Product2" };

            A.CallTo(() => _fakeProductNameValidator.Validate(A<ProductNameRequest>.Ignored)).Returns(new ValidationResult());
            A.CallTo(() => _fakeSalesCatalogueService.PostProductNamesAsync(A<ApiVersion>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(ServiceResponseResult<SalesCatalogueResponse>
                .Success(GetSalesCatalogueResponse()));
            A.CallTo(() => _fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored))
                .Returns(GetCreateBatchResponse());

            var result = await _service.ProcessProductNamesRequestAsync(productNames, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.Accepted);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustNotHaveHappened();
        }

        [Test]
        public async Task WhenValidProductVersionRequest_ThenProcessProductVersionsRequestReturns202Accepted()
        {
            var productVersions = new List<ProductVersionRequest>
            {
                new () { ProductName = "101GB40079ABCDEFG", EditionNumber = 7, UpdateNumber = 10 }
            };

            A.CallTo(() => _fakeProductVersionsValidator.Validate(A<ProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult());

            var result = await _service.ProcessProductVersionsRequest(productVersions, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.StatusCode.Should().Be(HttpStatusCode.Accepted);
            result.Should().NotBeNull();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustNotHaveHappened();
        }

        [Test]
        public async Task WhenProductNameIsBlank_ThenProcessProductVersionsRequestShouldReturnBadRequest()
        {
            var validationFailureMessage = "productName cannot be blank or null.";

            var productVersions = new List<ProductVersionRequest>
                {
                    new () { ProductName = "", EditionNumber = 7, UpdateNumber = 10 }
                };

            var validationMessage = new ValidationFailure("productVersions", validationFailureMessage)
            {
                ErrorCode = HttpStatusCode.BadRequest.ToString()
            };

            A.CallTo(() => _fakeProductVersionsValidator.Validate(A<ProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            var result = await _service.ProcessProductVersionsRequest(productVersions, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Should().NotBeNull();
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == validationFailureMessage);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustHaveHappened();
        }

        [Test]
        public async Task WhenProductVersionsRequestIsNull_ThenProcessProductVersionsRequestShouldReturnBadRequest()
        {
            var error = "Either body is null or malformed.";

            var result = await _service.ProcessProductVersionsRequest(null, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Should().NotBeNull();
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == error);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.EmptyBodyError.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either body is null or malformed | _X-Correlation-ID : {correlationId}").MustHaveHappened();
        }

        private ValidationResult GetValidationResult()
        {
            return new ValidationResult(

                new List<ValidationFailure>
                {
                    new()
                    {
                        ErrorCode = HttpStatusCode.BadRequest.ToString(),
                        PropertyName = "SinceDateTime",
                        ErrorMessage = "Provided sinceDateTime is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2024-12-20T11:51:00.000Z').",
                    },
                    new()
                    {
                        ErrorCode = HttpStatusCode.BadRequest.ToString(),
                        PropertyName = "ProductIdentifier",
                        ErrorMessage = "ProductIdentifier must be valid value",
                    },
                    new()
                    {
                        ErrorCode = HttpStatusCode.BadRequest.ToString(),
                        PropertyName = "CallbackUri",
                        ErrorMessage = "Invalid callbackUri format.",
                    }
                });
        }

        private static SalesCatalogueResponse GetSalesCatalogueResponse()
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
                                new() { ProductName = "102NO32904820801012", Reason = "invalidProduct" }
                            }
                    },
                    Products = new List<Products> {
                            new()
                            {
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

        private static CreateBatchResponse GetCreateBatchResponse()
        {
            return new CreateBatchResponse
            {
                ResponseBody = new CreateBatchResponseModel
                {
                    BatchId = BatchId,
                    BatchStatusUri = $"http://fss.ukho.gov.uk/batch/{BatchId}/status",
                    ExchangeSetBatchDetailsUri = $"http://fss.ukho.gov.uk/batch/{BatchId}",
                    ExchangeSetFileUri = $"http://fss.ukho.gov.uk/batch/{BatchId}/files/S100.zip",
                    BatchExpiryDateTime = "2025-02-17T16:19:32.269Z"
                },
                ResponseCode = HttpStatusCode.Created
            };
        }
    }
}
