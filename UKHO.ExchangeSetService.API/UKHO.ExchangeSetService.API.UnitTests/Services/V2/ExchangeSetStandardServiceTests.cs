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
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue.V2;
using UKHO.ExchangeSetService.Common.Models.V2.Request;
using UKHO.ExchangeSetService.Common.Storage.V2;
using IFileShareService = UKHO.ExchangeSetService.Common.Helpers.IFileShareService;
using ISalesCatalogueService = UKHO.ExchangeSetService.Common.Helpers.V2.ISalesCatalogueService;
using ExchangeSetStandard = UKHO.ExchangeSetService.Common.Models.V2.Enums.ExchangeSetStandard;

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
        private IExchangeSetServiceStorageProvider _fakeExchangeSetServiceStorageProvider;

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
            _fakeExchangeSetServiceStorageProvider= A.Fake<IExchangeSetServiceStorageProvider>();

            _service = new ExchangeSetStandardService(
                _fakeLogger,
                _fakeUpdatesSinceValidator,
                _fakeProductVersionsValidator,
                _fakeProductNameValidator,
                _fakeSalesCatalogueService,
                _fakeFileShareService,
                _fakeUserIdentifier,
                _fakeExchangeSetServiceStorageProvider);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new ExchangeSetStandardService(null, _fakeUpdatesSinceValidator, _fakeProductVersionsValidator, _fakeProductNameValidator, _fakeSalesCatalogueService, _fakeFileShareService, _fakeUserIdentifier, _fakeExchangeSetServiceStorageProvider);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullUpdatesSinceValidator = () => new ExchangeSetStandardService(_fakeLogger, null, _fakeProductVersionsValidator, _fakeProductNameValidator, _fakeSalesCatalogueService, _fakeFileShareService, _fakeUserIdentifier, _fakeExchangeSetServiceStorageProvider);
            nullUpdatesSinceValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("updatesSinceValidator");

            Action nullProductVersionsValidator = () => new ExchangeSetStandardService(_fakeLogger, _fakeUpdatesSinceValidator, null, _fakeProductNameValidator, _fakeSalesCatalogueService, _fakeFileShareService, _fakeUserIdentifier, _fakeExchangeSetServiceStorageProvider);
            nullProductVersionsValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productVersionsValidator");

            Action nullProductNamesValidator = () => new ExchangeSetStandardService(_fakeLogger, _fakeUpdatesSinceValidator, _fakeProductVersionsValidator, null, _fakeSalesCatalogueService, _fakeFileShareService, _fakeUserIdentifier, _fakeExchangeSetServiceStorageProvider);
            nullProductNamesValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productNameValidator");

            var nullSalesCatalogueService = () => new ExchangeSetStandardService(_fakeLogger, _fakeUpdatesSinceValidator, _fakeProductVersionsValidator, _fakeProductNameValidator, null, _fakeFileShareService, _fakeUserIdentifier, _fakeExchangeSetServiceStorageProvider);
            nullSalesCatalogueService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("salesCatalogueService");

            var nullFileShareService = () => new ExchangeSetStandardService(_fakeLogger, _fakeUpdatesSinceValidator, _fakeProductVersionsValidator, _fakeProductNameValidator, _fakeSalesCatalogueService, null, _fakeUserIdentifier, _fakeExchangeSetServiceStorageProvider);
            nullFileShareService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileShareService");

            var nullUserIdentifier = () => new ExchangeSetStandardService(_fakeLogger, _fakeUpdatesSinceValidator, _fakeProductVersionsValidator, _fakeProductNameValidator, _fakeSalesCatalogueService, _fakeFileShareService, null, _fakeExchangeSetServiceStorageProvider);
            nullUserIdentifier.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userIdentifier");

            var nullExchangeSetServiceStorageProvider = () => new ExchangeSetStandardService(_fakeLogger, _fakeUpdatesSinceValidator, _fakeProductVersionsValidator, _fakeProductNameValidator, _fakeSalesCatalogueService, _fakeFileShareService, _fakeUserIdentifier, null);
            nullExchangeSetServiceStorageProvider.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("exchangeSetServiceStorageProvider");
        }

        [Test]
        public async Task WhenValidSinceDateTimeRequested_ThenProcessUpdatesSinceReturns202Accepted()
        {
            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture) };

            A.CallTo(() => _fakeUpdatesSinceValidator.Validate(A<UpdatesSinceRequest>.Ignored))
                .Returns(new ValidationResult());

            A.CallTo(() => _fakeSalesCatalogueService.GetProductsFromUpdatesSinceAsync(A<ApiVersion>.Ignored, A<string>.Ignored, A<UpdatesSinceRequest>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(ServiceResponseResult<V2SalesCatalogueResponse>.Success(GetSalesCatalogueResponse()));

            A.CallTo(() => _fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored))
                .Returns(GetCreateBatchResponse());

            var result = await _service.ProcessUpdatesSinceRequestAsync(updatesSinceRequest, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), "s101", CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.StatusCode.Should().Be(HttpStatusCode.Accepted);
            result.Value.Should().NotBeNull();
            result.Value.ExchangeSetStandardResponse.ExchangeSetProductCount.Should().Be(2);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustNotHaveHappened();
        }

        [Test]
        public async Task WhenNoUpdatesFoundInSalesCatalogService_ThenProcessUpdatesSinceReturns304NotModified()
        {
            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture) };

            A.CallTo(() => _fakeUpdatesSinceValidator.Validate(A<UpdatesSinceRequest>.Ignored))
                .Returns(new ValidationResult());

            A.CallTo(() => _fakeSalesCatalogueService.GetProductsFromUpdatesSinceAsync(A<ApiVersion>.Ignored, A<string>.Ignored, A<UpdatesSinceRequest>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(ServiceResponseResult<V2SalesCatalogueResponse>.NotModified(new V2SalesCatalogueResponse()));

            var result = await _service.ProcessUpdatesSinceRequestAsync(updatesSinceRequest, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), "s101", CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.StatusCode.Should().Be(HttpStatusCode.NotModified);
            result.Value.Should().NotBeNull();
            result.Value.ExchangeSetStandardResponse.ExchangeSetProductCount.Should().Be(0);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustNotHaveHappened();
        }

        [Test]
        [TestCase(HttpStatusCode.NoContent)]
        [TestCase(HttpStatusCode.Unauthorized)]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.InternalServerError)]
        public async Task WhenSalesCatalogServiceReturnsOtherThanOkAndNotModified_ThenProcessUpdatesSinceReturns500InternalServerError(HttpStatusCode httpStatusCode)
        {
            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture) };

            A.CallTo(() => _fakeUpdatesSinceValidator.Validate(A<UpdatesSinceRequest>.Ignored))
                .Returns(new ValidationResult());

            A.CallTo(() => _fakeSalesCatalogueService.GetProductsFromUpdatesSinceAsync(A<ApiVersion>.Ignored, A<string>.Ignored, A<UpdatesSinceRequest>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(
                            httpStatusCode switch
                            {
                                HttpStatusCode.BadRequest => ServiceResponseResult<V2SalesCatalogueResponse>.BadRequest(new ErrorDescription { CorrelationId = _fakeCorrelationId, Errors = [] }),
                                HttpStatusCode.NotFound => ServiceResponseResult<V2SalesCatalogueResponse>.NotFound(new ErrorResponse { CorrelationId = _fakeCorrelationId, Detail = "Not found"}),
                                _ => ServiceResponseResult<V2SalesCatalogueResponse>.InternalServerError()
                            });

            var result = await _service.ProcessUpdatesSinceRequestAsync(updatesSinceRequest, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), "s101", CallbackUri, _fakeCorrelationId, CancellationToken.None);

            var statusCode = httpStatusCode switch
            {
                HttpStatusCode.BadRequest => HttpStatusCode.BadRequest,
                HttpStatusCode.NotFound => HttpStatusCode.NotFound,
                _ => HttpStatusCode.InternalServerError
            };

            result.StatusCode.Should().Be(statusCode);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustNotHaveHappened();
        }

        [Test]
        public async Task WhenNullOrEmptySinceDateTimeRequested_ThenProcessUpdatesSinceReturnsBadRequest()
        {
            var result = await _service.ProcessUpdatesSinceRequestAsync(null, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), "s101", CallbackUri, _fakeCorrelationId, CancellationToken.None);

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
        public async Task WhenInValidDataRequested_ThenProcessUpdatesSinceReturnsBadRequest(string inValidProductIdentifier, string inValidCallBackUri)
        {
            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = DateTime.UtcNow.AddDays(-10).ToString() };

            A.CallTo(() => _fakeUpdatesSinceValidator.Validate(A<UpdatesSinceRequest>.Ignored))
                .Returns(GetValidationResult());

            var result = await _service.ProcessUpdatesSinceRequestAsync(updatesSinceRequest, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), inValidProductIdentifier, inValidCallBackUri, _fakeCorrelationId, CancellationToken.None);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Provided sinceDateTime is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2024-12-20T11:51:00.000Z').");
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Invalid callbackUri format.");

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustHaveHappened();
        }

        [Test]
        public async Task WhenFssBatchResponseIsNotCreated_ThenProcessUpdatesSinceRequestReturnsInternalServerError()
        {
            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture) };

            A.CallTo(() => _fakeUpdatesSinceValidator.Validate(A<UpdatesSinceRequest>.Ignored))
                .Returns(new ValidationResult());

            A.CallTo(() => _fakeSalesCatalogueService.GetProductsFromUpdatesSinceAsync(A<ApiVersion>.Ignored, A<string>.Ignored, A<UpdatesSinceRequest>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(ServiceResponseResult<V2SalesCatalogueResponse>.Success(GetSalesCatalogueResponse()));

            A.CallTo(() => _fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored))
                .Returns(new CreateBatchResponse { ResponseCode = HttpStatusCode.BadRequest });

            var result = await _service.ProcessUpdatesSinceRequestAsync(updatesSinceRequest, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), "s101", CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
                                                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                                                && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
                                                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustNotHaveHappened();
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
        public async Task WhenProductNamesValidationPasses_ThenShouldReturnSuccess()
        {
            string[] productNames = ["Product1"];

            A.CallTo(() => _fakeProductNameValidator.Validate(A<ProductNameRequest>.Ignored)).Returns(new ValidationResult());
            A.CallTo(() => _fakeSalesCatalogueService.PostProductNamesAsync(A<ApiVersion>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(ServiceResponseResult<V2SalesCatalogueResponse>
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
                .Returns(ServiceResponseResult<V2SalesCatalogueResponse>
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
        public async Task WhenProductNamesFSSBatchResponseIsNotCreated_ThenProcessProductNamesShouldReturnInternalServerError()
        {
            string[] productNames = { "Product1" };

            A.CallTo(() => _fakeProductNameValidator.Validate(A<ProductNameRequest>.Ignored)).Returns(new ValidationResult());

            A.CallTo(() => _fakeSalesCatalogueService.PostProductNamesAsync(A<ApiVersion>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(ServiceResponseResult<V2SalesCatalogueResponse>
                .Success(GetSalesCatalogueResponse()));

            A.CallTo(() => _fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored))
                .Returns(new CreateBatchResponse { ResponseCode = HttpStatusCode.BadRequest });

            var result = await _service.ProcessProductNamesRequestAsync(productNames, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustNotHaveHappened();
        }

        [Test]
        public async Task WhenSalesCatalogueServiceReturnsBadRequest_ThenProcessProductNamesRequestAsyncReturnsBadRequest()
        {
            string[] productNames = { "Product1" };

            var salesCatalogueResponse = ServiceResponseResult<V2SalesCatalogueResponse>.BadRequest(GetBadRequestResponse());

            A.CallTo(() => _fakeSalesCatalogueService.PostProductNamesAsync(A<ApiVersion>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(salesCatalogueResponse);

            var result = await _service.ProcessProductNamesRequestAsync(productNames, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustNotHaveHappened();
        }

        [Test]
        public async Task WhenValidProductVersionsRequestAndScsServiceReturns200Ok_ThenProcessProductVersionsRequestReturns202Accepted()
        {
            var productVersions = new List<ProductVersionRequest>
            {
                new () { ProductName = "101GB40079ABCDEFG", EditionNumber = 7, UpdateNumber = 10 }
            };

            A.CallTo(() => _fakeProductVersionsValidator.Validate(A<ProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult());

            A.CallTo(() => _fakeSalesCatalogueService.PostProductVersionsAsync(A<ApiVersion>.Ignored, A<string>.Ignored, A<IEnumerable<ProductVersionRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
               .Returns(ServiceResponseResult<V2SalesCatalogueResponse>.Success(GetSalesCatalogueServiceResponseForProductVersions(HttpStatusCode.OK)));

            A.CallTo(() => _fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored))
                .Returns(GetCreateBatchResponse());

            var result = await _service.ProcessProductVersionsRequestAsync(productVersions, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.StatusCode.Should().Be(HttpStatusCode.Accepted);
            result.Should().NotBeNull();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustNotHaveHappened();
        }

        [Test]
        public async Task WhenValidProductVersionsRequestAndScsServiceReturns304NotModified_ThenProcessProductVersionsRequestReturns202Accepted()
        {
            var productVersions = new List<ProductVersionRequest>
            {
                new () { ProductName = "101GB40079ABCDEFG", EditionNumber = 7, UpdateNumber = 10 }
            };

            A.CallTo(() => _fakeProductVersionsValidator.Validate(A<ProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult());

            A.CallTo(() => _fakeSalesCatalogueService.PostProductVersionsAsync(A<ApiVersion>.Ignored, A<string>.Ignored, A<IEnumerable<ProductVersionRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
               .Returns(ServiceResponseResult<V2SalesCatalogueResponse>.NotModified(GetSalesCatalogueServiceResponseForProductVersions(HttpStatusCode.NotModified)));

            A.CallTo(() => _fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored))
                .Returns(GetCreateBatchResponse());

            var result = await _service.ProcessProductVersionsRequestAsync(productVersions, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.StatusCode.Should().Be(HttpStatusCode.Accepted);
            result.Should().NotBeNull();
            result.Value.ExchangeSetStandardResponse.ExchangeSetProductCount.Should().Be(0);
            result.Value.ExchangeSetStandardResponse.RequestedProductCount.Should().Be(productVersions.Count);
            result.Value.ExchangeSetStandardResponse.RequestedProductsAlreadyUpToDateCount.Should().Be(productVersions.Count);
            result.Value.ExchangeSetStandardResponse.RequestedProductsNotInExchangeSet.Should().BeEmpty();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustNotHaveHappened();
        }

        [Test]
        public async Task WhenSalesCatalogServiceReturnsBadRequest_ThenProcessProductVersionsRequestsReturnsBadRequest()
        {
            var productVersions = new List<ProductVersionRequest>
            {
                new () { ProductName = "101GB40079ABCDEFG", EditionNumber = 7, UpdateNumber = 10 }
            };

            A.CallTo(() => _fakeProductVersionsValidator.Validate(A<ProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult());

            A.CallTo(() => _fakeSalesCatalogueService.PostProductVersionsAsync(A<ApiVersion>.Ignored, A<string>.Ignored, A<IEnumerable<ProductVersionRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(ServiceResponseResult<V2SalesCatalogueResponse>.BadRequest(GetBadRequestResponse()));

            var result = await _service.ProcessProductVersionsRequestAsync(productVersions, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test]
        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.NoContent)]
        [TestCase(HttpStatusCode.InternalServerError)]
        public async Task WhenSalesCatalogServiceReturnsOtherResponse_ThenProcessProductVersionsRequestsReturns500InternalServerError(HttpStatusCode httpStatusCode)
        {
            var productVersions = new List<ProductVersionRequest>
            {
                new () { ProductName = "101GB40079ABCDEFG", EditionNumber = 7, UpdateNumber = 10 }
            };

            A.CallTo(() => _fakeProductVersionsValidator.Validate(A<ProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult());

            A.CallTo(() => _fakeSalesCatalogueService.PostProductVersionsAsync(A<ApiVersion>.Ignored, A<string>.Ignored, A<IEnumerable<ProductVersionRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(
                            httpStatusCode switch
                            {
                                HttpStatusCode.BadRequest => ServiceResponseResult<V2SalesCatalogueResponse>.BadRequest(new ErrorDescription { CorrelationId = _fakeCorrelationId, Errors = [] }),
                                HttpStatusCode.NotFound => ServiceResponseResult<V2SalesCatalogueResponse>.NotFound(new ErrorResponse { CorrelationId = _fakeCorrelationId, Detail = "Not found" }),
                                _ => ServiceResponseResult<V2SalesCatalogueResponse>.InternalServerError()
                            });

            var result = await _service.ProcessProductVersionsRequestAsync(productVersions, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

            var statusCode = httpStatusCode switch
            {
                HttpStatusCode.BadRequest => HttpStatusCode.BadRequest,
                HttpStatusCode.NotFound => HttpStatusCode.NotFound,
                _ => HttpStatusCode.InternalServerError
            };

            result.StatusCode.Should().Be(statusCode);

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

            var result = await _service.ProcessProductVersionsRequestAsync(productVersions, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

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

            var result = await _service.ProcessProductVersionsRequestAsync(null, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Should().NotBeNull();
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == error);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.EmptyBodyError.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either body is null or malformed | _X-Correlation-ID : {correlationId}").MustHaveHappened();
        }

        [Test]
        public async Task WhenProductVersionsFSSBatchResponseIsNotCreated_ThenProcessProductVersionsRequestReturnsInternalServerError()
        {
            var productVersions = new List<ProductVersionRequest>
            {
                new () { ProductName = "101GB40079ABCDEFG", EditionNumber = 7, UpdateNumber = 10 }
            };

            A.CallTo(() => _fakeProductVersionsValidator.Validate(A<ProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult());

            A.CallTo(() => _fakeSalesCatalogueService.PostProductVersionsAsync(A<ApiVersion>.Ignored, A<string>.Ignored, A<IEnumerable<ProductVersionRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(ServiceResponseResult<V2SalesCatalogueResponse>.Success(GetSalesCatalogueServiceResponseForProductVersions(HttpStatusCode.OK)));

            A.CallTo(() => _fakeFileShareService.CreateBatch(A<string>.Ignored, A<string>.Ignored))
                .Returns(new CreateBatchResponse { ResponseCode = HttpStatusCode.BadRequest });

            var result = await _service.ProcessProductVersionsRequestAsync(productVersions, ApiVersion.V2, ExchangeSetStandard.s100.ToString(), CallbackUri, _fakeCorrelationId, CancellationToken.None);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ValidationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}").MustNotHaveHappened();
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
                        PropertyName = "CallbackUri",
                        ErrorMessage = "Invalid callbackUri format.",
                    }
                });
        }

        private static V2SalesCatalogueResponse GetSalesCatalogueResponse()
        {
            return new V2SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody = new V2SalesCatalogueProductResponse
                {
                    ProductCounts = new ProductCounts
                    {
                        RequestedProductCount = 6,
                        RequestedProductsAlreadyUpToDateCount = 8,
                        ReturnedProductCount = 2,
                        RequestedProductsNotReturned = [
                                new RequestedProductsNotReturned { ProductName = "102NO32904820801012", Reason = "invalidProduct" }
                            ]
                    },
                    Products = new List<V2Products> {
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

        private static V2SalesCatalogueResponse GetSalesCatalogueServiceResponseForProductVersions(HttpStatusCode httpStatusCode)
        {
            return httpStatusCode switch
            {
                HttpStatusCode.OK =>
                    new V2SalesCatalogueResponse
                    {
                        ResponseCode = HttpStatusCode.OK,
                        ScsRequestDateTime = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        ResponseBody = new V2SalesCatalogueProductResponse
                        {
                            ProductCounts = new ProductCounts
                            {
                                RequestedProductCount = 1,
                                RequestedProductsAlreadyUpToDateCount = 0,
                                ReturnedProductCount = 1,
                                RequestedProductsNotReturned = []
                            },
                            Products = [
                               new V2Products {
                                ProductName = "101GB40079ABCDEFG",
                                EditionNumber = 7,
                                UpdateNumbers = [11, 12],
                                Dates = [new Dates { IssueDate =DateTime.Today.AddDays(-50), UpdateNumber=1},
                                new Dates{IssueDate=DateTime.Today, UpdateNumber = 2}],
                                FileSize = 900000000
                            }
                           ]
                        }
                    },
                HttpStatusCode.NotModified =>
                new V2SalesCatalogueResponse
                {
                    ResponseCode = HttpStatusCode.NotModified,
                    ScsRequestDateTime = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    ResponseBody = new V2SalesCatalogueProductResponse
                    {
                        Products = [],
                        ProductCounts = new ProductCounts()
                        {
                            RequestedProductCount = 1,
                            RequestedProductsAlreadyUpToDateCount = 1,
                            ReturnedProductCount = 0,
                            RequestedProductsNotReturned = []
                        },
                    }
                },
                _ => throw new NotImplementedException()
            };
        }

        private CreateBatchResponse GetCreateBatchResponse()
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

        private ErrorDescription GetBadRequestResponse()
        {
            return new ErrorDescription
            {
                CorrelationId = _fakeCorrelationId,
                Errors = new List<Error>
                {
                    new Error { Description = "Error in sales catalogue service", Source = "Sales catalogue service" }
                }
            };
        }
    }
}
