using System;
using System.Linq;
using FluentValidation.TestHelper;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.UnitTests.Validation
{
    public class ProductIdentifiersValidatorTests
    {
        private ProductIdentifierValidator validator;

        [SetUp]
        public void Setup()
        {
            validator = new ProductIdentifierValidator();
        }

        #region Product Identifiers
        [Test]
        public void WhenInvalidCallbackuriInProductIdentifierRequest_ThenReturnBadRequest()
        {
            var model = new ProductIdentifierRequest { CallbackUri = "demo uri" };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.CallbackUri);
            Assert.That(result.Errors.Any(x => x.ErrorMessage == "Invalid callbackUri format."));
        }

        [Test]
        public void WhenEmptyCallbackuriInProductDataProductIdentifierRequest_ThenReturnSuccess()
        {
            string[] productIdentifiers = { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;
            var model = new ProductIdentifierRequest
            {
                ProductIdentifier = productIdentifiers,
                CallbackUri = callbackUri
            };
            var result = validator.TestValidate(model);
            Assert.That(result.Errors.Count == 0);
        }

        [Test]
        public void WhenEmptyProductIdentifiersInProductIdentifiersRequest_ThenReturnBadRequest()
        {
            string[] productIdentifiers = { string.Empty };
            string callbackUri = string.Empty;
            var model = new ProductIdentifierRequest
            {
                ProductIdentifier = productIdentifiers,
                CallbackUri = callbackUri
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductIdentifier);
            Assert.That(result.Errors.Any(x => x.ErrorMessage == "productIdentifiers cannot be null or empty."));
        }

        [Test]
        public void WhenNullProductIdentifiersInProductIdentifiersRequest_ThenReturnBadRequest()
        {
            string[] productIdentifiers = { null };
            string callbackUri = string.Empty;
            var model = new ProductIdentifierRequest
            {
                ProductIdentifier = productIdentifiers,
                CallbackUri = callbackUri
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductIdentifier);
            Assert.That(result.Errors.Any(x => x.ErrorMessage == "productIdentifiers cannot be null or empty."));
        }

        [Test]
        public void WhenZeroLengthProductIdentifiersInProductIdentifiersRequest_ThenReturnBadRequest()
        {
            string[] productIdentifiers = Array.Empty<string>();
            string callbackUri = string.Empty;
            var model = new ProductIdentifierRequest
            {
                ProductIdentifier = productIdentifiers,
                CallbackUri = callbackUri
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductIdentifier);
            Assert.That(result.Errors.Any(x => x.ErrorMessage == "productIdentifiers cannot be null or empty."));
        }

        [Test]
        public void WhenZeroProductIdentifiersInProductIdentifiersRequest_ThenReturnBadRequest()
        {
            string[] productIdentifiers = null;
            string callbackUri = string.Empty;
            var model = new ProductIdentifierRequest
            {
                ProductIdentifier = productIdentifiers,
                CallbackUri = callbackUri
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductIdentifier);
            Assert.That(result.Errors.Any(x => x.ErrorMessage == "productIdentifiers cannot be null or empty."));
        }

        [Test]
        public void WhenValidProductIdentifiersAndvalidCallBackuriInProductIdentifiersRequest_ThenReturnSuccess()
        {
            string[] productIdentifiers = { "GB123456", "GB160060", "AU334550" };
            string callbackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            var model = new ProductIdentifierRequest
            {
                ProductIdentifier = productIdentifiers,
                CallbackUri = callbackUri
            };
            var result = validator.TestValidate(model);
            Assert.That(result.Errors, Is.Empty);
        }
        #endregion
    }
}
