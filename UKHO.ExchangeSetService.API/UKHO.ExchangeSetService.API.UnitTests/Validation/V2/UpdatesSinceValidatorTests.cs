// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Validation.V2;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.UnitTests.Validation.V2
{
    [TestFixture]
    public class UpdatesSinceValidatorTests
    {
        private const string Iso8601DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        private IConfiguration _fakeConfiguration;
        private UpdatesSinceValidator _validator;

        [SetUp]
        public void Setup()
        {
            _fakeConfiguration = A.Fake<IConfiguration>();
            _fakeConfiguration["MaximumNumerOfDaysValidForSinceDateTimeEndpoint"] = "28";

            _validator = new UpdatesSinceValidator(_fakeConfiguration);
        }

        [Test]
        public async Task WhenSinceDateTimeIsValid_ThenValidatorReturnsTrue()
        {
            var request = new UpdatesSinceRequest
            {
                SinceDateTime = DateTime.UtcNow.AddDays(-10).ToString(Iso8601DateTimeFormat, CultureInfo.InvariantCulture)
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public async Task WhenSinceDateTimeIsInValid_ThenValidatorReturnsFalse()
        {
            var request = new UpdatesSinceRequest
            {
                SinceDateTime = "Invalid Date"
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Provided sinceDateTime is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2024-12-20T11:51:00.000Z').");
        }

        [Test]
        public async Task WhenSinceDateTimeIsInFuture_ThenValidatorReturnsFalse()
        {
            var request = new UpdatesSinceRequest
            {
                SinceDateTime = DateTime.UtcNow.AddDays(10).ToString(Iso8601DateTimeFormat, CultureInfo.InvariantCulture)
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Provided sinceDateTime cannot be a future date.");
        }

        [Test]
        public async Task WhenSinceDateTimeIsOlderThanMaximumDays_ThenValidatorReturnsFalse()
        {
            var request = new UpdatesSinceRequest
            {
                SinceDateTime = DateTime.UtcNow.AddDays(-40).ToString(Iso8601DateTimeFormat, CultureInfo.InvariantCulture)
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorMessage == $"Provided sinceDateTime must be within last " + _fakeConfiguration["MaximumNumerOfDaysValidForSinceDateTimeEndpoint"] + " days.");
        }

        [Test]
        public async Task WhenCallbackUriIsValid_ThenValidatorReturnsTrue()
        {
            var request = new UpdatesSinceRequest
            {
                SinceDateTime = DateTime.UtcNow.AddDays(-10).ToString(Iso8601DateTimeFormat, CultureInfo.InvariantCulture),
                CallbackUri = "https://valid.callback.uri"
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public async Task WhenCallbackUriIsInvalid_ThenValidatorReturnsFalse()
        {
            var request = new UpdatesSinceRequest
            {
                SinceDateTime = DateTime.UtcNow.AddDays(-10).ToString(Iso8601DateTimeFormat, CultureInfo.InvariantCulture),
                CallbackUri = "invalid-uri"
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Invalid callbackUri format.");
        }
    }
}
