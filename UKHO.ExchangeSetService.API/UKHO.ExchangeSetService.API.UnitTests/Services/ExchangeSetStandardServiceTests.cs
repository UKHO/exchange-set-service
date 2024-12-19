// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.UnitTests.Services
{
    public class ExchangeSetStandardServiceTests
    {
        private IUpdatesSinceValidator _fakeUpdatesSinceValidator;
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();

        private ExchangeSetStandardService _service;

        [SetUp]
        public void Setup()
        {
            _fakeUpdatesSinceValidator = A.Fake<IUpdatesSinceValidator>();

            _service = new ExchangeSetStandardService(_fakeUpdatesSinceValidator);
        }

        [Test]
        public async Task WhenValidSinceDateTimeRequested_ThenCreateUpdatesSince_Returns202Accepted()
        {
            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = "Tue, 10 Dec 2024 05:46:00 GMT" };

            A.CallTo(() => _fakeUpdatesSinceValidator.Validate(A<UpdatesSinceRequest>.Ignored))
                .Returns(new ValidationResult());

            var result = await _service.CreateUpdatesSince(updatesSinceRequest, _fakeCorrelationId, CancellationToken.None);

            result.StatusCode.Should().Be(HttpStatusCode.Accepted);
            result.Value.Should().NotBeNull();
        }

        [Test]
        public async Task WhenInValidSinceDateTimeRequested_ThenCreateUpdatesSince_ReturnsBadRequest()
        {
            var validationFailureMessage = "Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format' (e.g. 'Wed, 21 Oct 2020 07:28:00 GMT').";

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
        }
    }
}
