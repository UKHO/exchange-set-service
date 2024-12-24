using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.API.Validation.V2;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.UnitTests.Services
{
    [TestFixture]
    public class ExchangeSetStandardServiceTests
    {
        private IProductDataService _fakeProductDataService;
        private IProductNameValidator _fakeProductNameValidator;
        private ILogger<ExchangeSetStandardService> _fakeLogger;
        private ExchangeSetStandardService _exchangeSetService;

        [SetUp]
        public void Setup()
        {
            _fakeProductDataService = A.Fake<IProductDataService>();
            _fakeProductNameValidator = A.Fake<IProductNameValidator>();
            _fakeLogger = A.Fake<ILogger<ExchangeSetStandardService>>();
            _exchangeSetService = new ExchangeSetStandardService(_fakeProductDataService, _fakeProductNameValidator, _fakeLogger);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullProductDataService = () => new ExchangeSetStandardService(null, _fakeProductNameValidator, _fakeLogger);
            nullProductDataService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productDataService");

            Action nullProductNameValidator = () => new ExchangeSetStandardService(_fakeProductDataService, null, _fakeLogger);
            nullProductNameValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productNameValidator");

            Action nullLogger = () => new ExchangeSetStandardService(_fakeProductDataService, _fakeProductNameValidator,null);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [Test]
        public async Task WhenProductNamesAreNull_ThenShouldReturnBadRequest()
        {
            string[] productNames = null;
            string callbackUri = "http://callback.uri";
            string correlationId = "correlationId";

            var result = await _exchangeSetService.CreateProductDataByProductNames(productNames, callbackUri, correlationId);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Either body is null or malformed.");

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.CreateProductDataByProductNamesStarted.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of Product data started | X-Correlation-ID : {correlationId}").MustHaveHappened();

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

            var result = await _exchangeSetService.CreateProductDataByProductNames(productNames, callbackUri, correlationId);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Either body is null or malformed.");

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.CreateProductDataByProductNamesStarted.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of Product data started | X-Correlation-ID : {correlationId}").MustHaveHappened();

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

            var result = await _exchangeSetService.CreateProductDataByProductNames(productNames, callbackUri, correlationId);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == "Invalid product identifier");

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.CreateProductDataByProductNamesStarted.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of Product data started | X-Correlation-ID : {correlationId}").MustHaveHappened();

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

            var result = await _exchangeSetService.CreateProductDataByProductNames(productNames, callbackUri, correlationId);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.Accepted);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
         && call.GetArgument<LogLevel>(0) == LogLevel.Information
         && call.GetArgument<EventId>(1) == EventIds.CreateProductDataByProductNamesStarted.ToEventId()
         && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of Product data started | X-Correlation-ID : {correlationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
         && call.GetArgument<LogLevel>(0) == LogLevel.Information
         && call.GetArgument<EventId>(1) == EventIds.CreateProductDataByProductNamesCompleted.ToEventId()
         && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of Product data completed | X-Correlation-ID : {correlationId}").MustHaveHappened();
        }
    }
}
