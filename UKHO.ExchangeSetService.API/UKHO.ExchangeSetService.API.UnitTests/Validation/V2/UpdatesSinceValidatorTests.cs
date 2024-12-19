// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Validation.V2;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.UnitTests.Validation.V2
{
    [TestFixture]
    public class UpdatesSinceValidatorTests
    {
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
        public async Task WhenSinceDateTimeIsValid_ThenValidator_ReturnsTrue()
        {
            var request = new UpdatesSinceRequest
            {
                SinceDateTime = DateTime.UtcNow.AddDays(-5).ToString("R")
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public async Task WhenSinceDateTimeIsInValid_ThenValidator_ReturnsFalse()
        {
            var request = new UpdatesSinceRequest
            {
                SinceDateTime = "Invalid Date"
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format' (e.g. 'Wed, 21 Oct 2020 07:28:00 GMT').");
        }

        [Test]
        public async Task WhenSinceDateTimeIsInFuture_ThenValidator_ReturnsFalse()
        {
            var request = new UpdatesSinceRequest
            {
                SinceDateTime = DateTime.UtcNow.AddDays(1).ToString("R")
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Provided sinceDateTime cannot be a future date.");
        }

        [Test]
        public async Task WhenSinceDateTimeIsOlderThanMaximumDays_ThenValidator_ReturnsFalse()
        {
            var request = new UpdatesSinceRequest
            {
                SinceDateTime = DateTime.UtcNow.AddDays(-30).ToString("R")
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorMessage == $"Provided sinceDateTime must be within last " + _fakeConfiguration["MaximumNumerOfDaysValidForSinceDateTimeEndpoint"] + " days.");
        }

        [Test]
        public async Task WhenCallbackUriIsValid_ThenValidator_ReturnsTrue()
        {
            var request = new UpdatesSinceRequest
            {
                SinceDateTime = DateTime.UtcNow.AddDays(-5).ToString("R"),
                CallbackUri = "https://valid.callback.uri"
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public async Task WhenCallbackUriIsInvalid_ThenValidator_ReturnsFalse()
        {
            var request = new UpdatesSinceRequest
            {
                SinceDateTime = "Wed, 21 Oct 2020 07:28:00 GMT",
                CallbackUri = "invalid-uri"
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Invalid callbackUri format.");
        }

        [Test]
        public async Task WhenProductIdentifierIsValid_ThenValidator_ReturnsTrue()
        {
            var request = new UpdatesSinceRequest
            {
                SinceDateTime = DateTime.UtcNow.AddDays(-5).ToString("R"),
                ProductIdentifier = S100ProductType.s101.ToString()
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public async Task WhenProductIdentifierIsInvalid_ThenValidator_ReturnsFalse()
        {
            var request = new UpdatesSinceRequest
            {
                SinceDateTime = "Wed, 21 Oct 2020 07:28:00 GMT",
                ProductIdentifier = "InvalidProduct"
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("productIdentifier must be valid value"));
        }
    }
}
