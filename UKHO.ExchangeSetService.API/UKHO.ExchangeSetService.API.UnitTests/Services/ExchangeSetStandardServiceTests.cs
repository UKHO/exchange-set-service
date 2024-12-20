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
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.UnitTests.Services
{
    [TestFixture]
    public class ExchangeSetStandardServiceTests
    {
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();

        private ILogger<ExchangeSetStandardService> _fakeLogger;
        private IUpdatesSinceValidator _fakeUpdatesSinceValidator;

        private ExchangeSetStandardService _service;

        [SetUp]
        public void Setup()
        {
            _fakeLogger = A.Fake<ILogger<ExchangeSetStandardService>>();
            _fakeUpdatesSinceValidator = A.Fake<IUpdatesSinceValidator>();

            _service = new ExchangeSetStandardService(_fakeLogger, _fakeUpdatesSinceValidator);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new ExchangeSetStandardService(null, _fakeUpdatesSinceValidator);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullUpdatesSinceValidator = () => new ExchangeSetStandardService(_fakeLogger, null);
            nullUpdatesSinceValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("updatesSinceValidator");
        }

        [Test]
        public async Task WhenValidSinceDateTimeRequested_ThenCreateUpdatesSince_Returns202Accepted()
        {
            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture) };

            A.CallTo(() => _fakeUpdatesSinceValidator.Validate(A<UpdatesSinceRequest>.Ignored))
                .Returns(new ValidationResult());

            var result = await _service.CreateUpdatesSince(updatesSinceRequest, _fakeCorrelationId, CancellationToken.None);

            result.StatusCode.Should().Be(HttpStatusCode.Accepted);
            result.Value.Should().NotBeNull();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateUpdatesSinceStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of update since started | X-Correlation-ID : {CorrelationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateUpdatesSinceCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of update since completed | X-Correlation-ID : {CorrelationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.CreateUpdatesSinceException.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of update since exception occurred | X-Correlation-ID : {CorrelationId}").MustNotHaveHappened();
        }

        [Test]
        public async Task WhenInValidSinceDateTimeRequested_ThenCreateUpdatesSince_ReturnsBadRequest()
        {
            var validationFailureMessage = "Provided sinceDateTime is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2024-12-20T11:51:00.000Z').";

            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = "" };

            var validationMessage = new ValidationFailure("SinceDateTime", validationFailureMessage)
            {
                ErrorCode = HttpStatusCode.BadRequest.ToString()
            };

            A.CallTo(() => _fakeUpdatesSinceValidator.Validate(A<UpdatesSinceRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            var result = await _service.CreateUpdatesSince(updatesSinceRequest, _fakeCorrelationId, CancellationToken.None);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorDescription.Should().NotBeNull();
            result.ErrorDescription.Errors.Should().ContainSingle(e => e.Description == validationFailureMessage);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateUpdatesSinceStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of update since started | X-Correlation-ID : {CorrelationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.CreateUpdatesSinceException.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of update since exception occurred | X-Correlation-ID : {CorrelationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreateUpdatesSinceCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of update since completed | X-Correlation-ID : {CorrelationId}").MustNotHaveHappened();
        }
    }
}
