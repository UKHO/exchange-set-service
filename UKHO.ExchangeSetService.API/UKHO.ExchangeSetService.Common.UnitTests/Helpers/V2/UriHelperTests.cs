// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using FluentAssertions;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Helpers.V2;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers.V2
{
    [TestFixture]
    public class UriHelperTests
    {
        private UriHelper _uriHelper;

        [SetUp]
        public void Setup()
        {
            _uriHelper = new UriHelper();
        }

        [Test]
        public void WhenValidInputsAreProvided_ThenCreateUriShouldReturnCorrectUri()
        {
            var baseUrl = "https://example.com";
            var endpointFormat = "/api/resource/{0}";
            object[] args = ["123"];

            var result = _uriHelper.CreateUri(baseUrl, endpointFormat, args);

            result.Should().Be(new Uri("https://example.com/api/resource/123"));
        }

        [Test]
        public void WhenInvalidBaseUrlIsProvided_ThenCreateUriShouldThrowUriFormatException()
        {
            var baseUrl = "invalid-url";
            var endpointFormat = "/api/resource/{0}";
            object[] args = ["123"];

            Action act = () => _uriHelper.CreateUri(baseUrl, endpointFormat, args);

            act.Should().Throw<UriFormatException>();
        }

        [Test]
        public void WhenInvalidEndpointFormatIsProvided_ThenCreateUriShouldThrowFormatException()
        {
            var baseUrl = "https://example.com";
            var endpointFormat = "/api/resource/{0}/{1}";
            object[] args = ["123"];

            Action act = () => _uriHelper.CreateUri(baseUrl, endpointFormat, args);

            act.Should().Throw<FormatException>();
        }

        [Test]
        public void WhenNoArgsAreProvided_ThenCreateUriShouldReturnCorrectUri()
        {
            var baseUrl = "https://example.com";
            var endpointFormat = "/api/resource";

            var result = _uriHelper.CreateUri(baseUrl, endpointFormat);

            result.Should().Be(new Uri("https://example.com/api/resource"));
        }
    }
}
