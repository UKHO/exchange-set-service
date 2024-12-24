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
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.API.Validation.V2;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.UnitTests.Services
{
    [TestFixture]
    public class ExchangeSetStandardServiceTests
    {
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();

        private IProductDataService _fakeProductDataService;
        private IProductNameValidator _fakeProductNameValidator;
        private ILogger<ExchangeSetStandardService> _fakeLogger;
        private IUpdatesSinceValidator _fakeUpdatesSinceValidator;

        private ExchangeSetStandardService _service;

        [SetUp]
        public void Setup()
        {
            _fakeProductDataService = A.Fake<IProductDataService>();
            _fakeProductNameValidator = A.Fake<IProductNameValidator>();
            _fakeLogger = A.Fake<ILogger<ExchangeSetStandardService>>();
            _fakeUpdatesSinceValidator = A.Fake<IUpdatesSinceValidator>();

            _service = new ExchangeSetStandardService(_fakeLogger, _fakeUpdatesSinceValidator, _fakeProductDataService, _fakeProductNameValidator);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new ExchangeSetStandardService(null, _fakeUpdatesSinceValidator, _fakeProductDataService, _fakeProductNameValidator);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullProductDataService = () => new ExchangeSetStandardService(_fakeLogger, _fakeUpdatesSinceValidator, null, _fakeProductNameValidator);
            nullProductDataService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productDataService");

            Action nullUpdatesSinceValidator = () => new ExchangeSetStandardService(_fakeLogger, null, _fakeProductDataService, _fakeProductNameValidator);
            nullUpdatesSinceValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("updatesSinceValidator");

            Action nullProductNameValidator = () => new ExchangeSetStandardService(_fakeLogger, _fakeUpdatesSinceValidator, _fakeProductDataService, null);
            nullProductNameValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productNameValidator");
        }

        [Test]
        public async Task WhenValidSinceDateTimeRequested_ThenCreateUpdatesSinceReturns202Accepted()
        {
            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture) };

            A.CallTo(() => _fakeUpdatesSinceValidator.Validate(A<UpdatesSinceRequest>.Ignored))
                .Returns(new ValidationResult());

            var result = await _service.CreateUpdatesSince(updatesSinceRequest, "s101", "https://callback.uri", _fakeCorrelationId, CancellationToken.None);

            result.StatusCode.Should().Be(HttpStatusCode.Accepted);
            result.Value.Should().NotBeNull();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateUpdatesSinceStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of update since started | X-Correlation-ID : {correlationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateUpdatesSinceCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of update since completed | X-Correlation-ID : {correlationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.CreateUpdatesSinceException.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of update since exception occurred | X-Correlation-ID : {correlationId}").MustNotHaveHappened();
        }

        [Test]
        public async Task WhenNullOrEmptySinceDateTimeRequested_ThenCreateUpdatesSinceReturnsBadRequest()
        {
            var result = await _service.CreateUpdatesSince(null, "s101", "https://callback.uri", _fakeCorrelationId, CancellationToken.None);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Either body is null or malformed.");

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateUpdatesSinceStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of update since started | X-Correlation-ID : {correlationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.CreateUpdatesSinceException.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of update since exception occurred | X-Correlation-ID : {correlationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateUpdatesSinceCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of update since completed | X-Correlation-ID : {correlationId}").MustNotHaveHappened();
        }

        [Test]
        [TestCase("101", "http://callback.uri")]
        [TestCase("101s", "http//callback.uri")]
        [TestCase("S101", "http:callback.uri")]
        public async Task WhenInValidDataRequested_ThenCreateUpdatesSinceReturnsBadRequest(string inValidProductIdentifier, string inValidCallBackUri)
        {
            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = DateTime.UtcNow.AddDays(-10).ToString() };

            A.CallTo(() => _fakeUpdatesSinceValidator.Validate(A<UpdatesSinceRequest>.Ignored))
                .Returns(GetValidationResult());

            var result = await _service.CreateUpdatesSince(updatesSinceRequest, inValidProductIdentifier, inValidCallBackUri, _fakeCorrelationId, CancellationToken.None);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Provided sinceDateTime is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2024-12-20T11:51:00.000Z').");
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "ProductIdentifier must be valid value");
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Invalid callbackUri format.");

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateUpdatesSinceStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of update since started | X-Correlation-ID : {correlationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.CreateUpdatesSinceException.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of update since exception occurred | X-Correlation-ID : {correlationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateUpdatesSinceCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of update since completed | X-Correlation-ID : {correlationId}").MustNotHaveHappened();
        }

        [Test]
        public async Task WhenProductNamesAreNull_ThenShouldReturnBadRequest()
        {
            string[] productNames = null;
            string callbackUri = "http://callback.uri";
            string correlationId = "correlationId";

            var result = await _service.CreateProductDataByProductNames(productNames, callbackUri, correlationId);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Either body is null or malformed.");

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.CreateProductDataByProductNamesStarted.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of Product data for product Names started | X-Correlation-ID : {correlationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.EmptyBodyError.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either body is null or malformed | X-Correlation-ID : {correlationId}").MustHaveHappened();
        }

        [Test]
        public async Task WhenProductNamesAreEmpty_ThenShouldReturnBadRequest()
        {
            string[] productNames = Array.Empty<string>();
            string callbackUri = "http://callback.uri";
            string correlationId = "correlationId";

            var result = await _service.CreateProductDataByProductNames(productNames, callbackUri, correlationId);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Either body is null or malformed.");

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.CreateProductDataByProductNamesStarted.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of Product data for product Names started | X-Correlation-ID : {correlationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.EmptyBodyError.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either body is null or malformed | X-Correlation-ID : {correlationId}").MustHaveHappened();
        }

        [Test]
        public async Task WhenValidationFails_ThenShouldReturnBadRequest()
        {
            string[] productNames = new[] { "Product1" };
            string callbackUri = "http://callback.uri";
            string correlationId = "correlationId";

            var validationResult = new ValidationResult(new List<ValidationFailure>
                {
                    new ValidationFailure("ProductIdentifier", "Invalid product identifier")
                    {
                        ErrorCode = HttpStatusCode.BadRequest.ToString()
                    },
                });

            A.CallTo(() => _fakeProductNameValidator.Validate(A<ProductNameRequest>.Ignored)).Returns(Task.FromResult(validationResult));

            var result = await _service.CreateProductDataByProductNames(productNames, callbackUri, correlationId);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Invalid product identifier");

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.CreateProductDataByProductNamesStarted.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of Product data for product Names started | X-Correlation-ID : {correlationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.InvalidProductNames.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Product name validation failed. | X-Correlation-ID : {correlationId}").MustHaveHappened();
        }

        [Test]
        public async Task WhenValidationPasses_ThenShouldReturnSuccess()
        {
            string[] productNames = new[] { "Product1" };
            string callbackUri = "http://callback.uri";
            string correlationId = "correlationId";

            var validationResult = new ValidationResult();

            A.CallTo(() => _fakeProductNameValidator.Validate(A<ProductNameRequest>.Ignored)).Returns(Task.FromResult(validationResult));

            var result = await _service.CreateProductDataByProductNames(productNames, callbackUri, correlationId);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.Accepted);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
         && call.GetArgument<LogLevel>(0) == LogLevel.Information
         && call.GetArgument<EventId>(1) == EventIds.CreateProductDataByProductNamesStarted.ToEventId()
         && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of Product data for product Names started | X-Correlation-ID : {correlationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
         && call.GetArgument<LogLevel>(0) == LogLevel.Information
         && call.GetArgument<EventId>(1) == EventIds.CreateProductDataByProductNamesCompleted.ToEventId()
         && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of Product data for product Names completed | X-Correlation-ID : {correlationId}").MustHaveHappened();
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
    }
}
