using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.API.Validation.V2;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.UnitTests.Services
{
    [TestFixture]
    public class ExchangeSetServiceTests
    {
        private IProductDataService _fakeProductDataService;
        private IProductNameValidator _fakeProductNameValidator;
        private API.Services.ExchangeSetService _exchangeSetService;

        [SetUp]
        public void Setup()
        {
            _fakeProductDataService = A.Fake<IProductDataService>();
            _fakeProductNameValidator = A.Fake<IProductNameValidator>();
            _exchangeSetService = new API.Services.ExchangeSetService(_fakeProductDataService, _fakeProductNameValidator);
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
        }
    }
}
