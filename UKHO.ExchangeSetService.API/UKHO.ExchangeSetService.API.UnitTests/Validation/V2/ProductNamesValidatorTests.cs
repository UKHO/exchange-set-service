using System;
using System.Linq;
using FluentValidation.TestHelper;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Validation.V2;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.UnitTests.Validation.V2
{
    public class ProductNamesValidatorTests
    {
        private ProductNameValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new ProductNameValidator();
        }

        [Test]
        public void WhenInvalidCallBackUriInProductNamesRequest_ThenReturnBadRequest()
        {
            var model = new ProductNameRequest { CallbackUri = "demo uri" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.CallbackUri);
            Assert.That(result.Errors.Any(x => x.ErrorMessage == "Invalid callbackUri format."));
        }

        [Test]
        public void WhenEmptyCallBackUriInProductDataProductNamesRequest_ThenReturnSuccess()
        {
            string[] productIdentifiers = { "101GB40079ABCDEFG", "102NO32904820801012", "104US00_CHES_TYPE1_20210630_0600" };
            var callbackUri = string.Empty;
            var model = new ProductNameRequest
            {
                ProductNames = productIdentifiers,
                CallbackUri = callbackUri
            };
            var result = _validator.TestValidate(model);
            Assert.That(result.Errors.Count == 0);
        }

        [Test]
        public void WhenEmptyProductNamesInProductNamesRequest_ThenReturnBadRequest()
        {
            string[] productIdentifiers = { string.Empty };
            var callbackUri = string.Empty;
            var model = new ProductNameRequest
            {
                ProductNames = productIdentifiers,
                CallbackUri = callbackUri
            };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductNames);
            Assert.That(result.Errors.Any(x => x.ErrorMessage == "productIdentifiers cannot be null or empty."));
        }

        [Test]
        public void WhenNullProductNamesInProductNamesRequest_ThenReturnBadRequest()
        {
            string[] productIdentifiers = { null };
            var callbackUri = string.Empty;
            var model = new ProductNameRequest
            {
                ProductNames = productIdentifiers,
                CallbackUri = callbackUri
            };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductNames);
            Assert.That(result.Errors.Any(x => x.ErrorMessage == "productIdentifiers cannot be null or empty."));
        }

        [Test]
        public void WhenZeroLengthProductNamesInProductNamesRequest_ThenReturnBadRequest()
        {
            var productIdentifiers = Array.Empty<string>();
            var callbackUri = string.Empty;
            var model = new ProductNameRequest
            {
                ProductNames = productIdentifiers,
                CallbackUri = callbackUri
            };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductNames);
            Assert.That(result.Errors.Any(x => x.ErrorMessage == "productIdentifiers cannot be null or empty."));
        }

        [Test]
        public void WhenZeroProductNamesInProductNamesRequest_ThenReturnBadRequest()
        {
            string[] productNames = null;
            var callbackUri = string.Empty;
            var model = new ProductNameRequest
            {
                ProductNames = productNames,
                CallbackUri = callbackUri
            };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductNames);
            Assert.That(result.Errors.Any(x => x.ErrorMessage == "productIdentifiers cannot be null or empty."));
        }

        [Test]
        public void WhenValidProductNamesAndvalidCallBackUriInProductNamesRequest_ThenReturnSuccess()
        {
            string[] productIdentifiers = { "104US00_CHES_TYPE1_20210630_0600", "102NO32904820801012", "104US00_CHES_TYPE1_20210630_0600" };
            var callbackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            var model = new ProductNameRequest
            {
                ProductNames = productIdentifiers,
                CallbackUri = callbackUri
            };
            var result = _validator.TestValidate(model);
            Assert.That(0, Is.EqualTo(result.Errors.Count));
        }
    }
}
