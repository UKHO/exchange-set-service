// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Helpers.V2;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers.V2
{
    [TestFixture]
    public class UriHelperTests
    {
        private ILogger<UriHelper> _fakeLogger;
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();

        private UriHelper _uriHelper;

        [SetUp]
        public void Setup()
        {
            _fakeLogger = A.Fake<ILogger<UriHelper>>();

            _uriHelper = new UriHelper(_fakeLogger);
        }

        [Test]
        public void WhenValidInputsAreProvided_ThenCreateUriShouldReturnCorrectUri()
        {
            var baseUrl = "https://example.com";
            var endpointFormat = "/api/resource/{0}";
            object[] args = ["123"];

            var result = _uriHelper.CreateUri(baseUrl, endpointFormat, _fakeCorrelationId, args);

            result.Should().Be(new Uri("https://example.com/api/resource/123"));

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.UriException.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Exception occurred while creating Uri. Error: {Error} | StackTrace: {StackTrace} | _X-Correlation-ID: {CorrelationId}").MustNotHaveHappened();
        }

        [Test]
        public void WhenInvalidBaseUrlIsProvided_ThenCreateUriShouldThrowUriFormatException()
        {
            var baseUrl = "invalid-url";
            var endpointFormat = "/api/resource/{0}";
            object[] args = ["123"];

            Action act = () => _uriHelper.CreateUri(baseUrl, endpointFormat, _fakeCorrelationId, args);

            act.Should().Throw<UriFormatException>();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.UriException.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Exception occurred while creating Uri. Error: {Error} | StackTrace: {StackTrace} | _X-Correlation-ID: {CorrelationId}").MustHaveHappened();
        }

        [Test]
        public void WhenInvalidEndpointFormatIsProvided_ThenCreateUriShouldThrowFormatException()
        {
            var baseUrl = "https://example.com";
            var endpointFormat = "/api/resource/{0}/{1}";
            object[] args = ["123"];

            Action act = () => _uriHelper.CreateUri(baseUrl, endpointFormat, _fakeCorrelationId, args);

            act.Should().Throw<FormatException>();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.UriException.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Exception occurred while creating Uri. Error: {Error} | StackTrace: {StackTrace} | _X-Correlation-ID: {CorrelationId}").MustHaveHappened();
        }

        [Test]
        public void WhenNoArgsAreProvided_ThenCreateUriShouldReturnCorrectUri()
        {
            var baseUrl = "https://example.com";
            var endpointFormat = "/api/resource";

            var result = _uriHelper.CreateUri(baseUrl, endpointFormat, _fakeCorrelationId);

            result.Should().Be(new Uri("https://example.com/api/resource"));

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.UriException.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Exception occurred while creating Uri. Error: {Error} | StackTrace: {StackTrace} | _X-Correlation-ID: {CorrelationId}").MustNotHaveHappened();
        }
    }
}
