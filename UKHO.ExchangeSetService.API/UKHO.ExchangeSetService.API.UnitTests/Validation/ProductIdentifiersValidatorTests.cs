using FluentValidation.TestHelper;
using NUnit.Framework;
using System.Linq;
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
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Invalid callbackUri format."));
        }

        [Test]
        public void WhenEmptyCallbackuriInProductDataProductIdentifierRequest_ThenReturnSuccess()
        {
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;
            var model = new ProductIdentifierRequest
            {
              ProductIdentifier = productIdentifiers,
              CallbackUri = callbackUri
            };
            var result = validator.TestValidate(model);
            Assert.IsTrue(result.Errors.Count == 0);
        }

        [Test]
        public void WhenNullProductIdentifierInProductIdentifierRequest_ThenReturnBadRequest()
        {
            string[] productIdentifiers = null;
            string callbackUri = null;
            var model = new ProductIdentifierRequest
            {
                ProductIdentifier = productIdentifiers,
                CallbackUri = callbackUri
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductIdentifier);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Product Identifiers cannot be null or empty."));
        }

        [Test]
        public void WhenEmptyProductIdentifiersInProductIdentifiersRequest_ThenReturnBadRequest()
        {
            string[] productIdentifiers = new string[] {string.Empty};
            string callbackUri = string.Empty;
            var model = new ProductIdentifierRequest
            {
                ProductIdentifier = productIdentifiers,
                CallbackUri = callbackUri
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductIdentifier);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Product Identifiers cannot be null or empty."));
        }


        [Test]
        public void WhenValidProductIdentifiersAndvalidCallBackuriInProductIdentifiersRequest_ThenReturnSuccess()
        {
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234";
            var model = new ProductIdentifierRequest
            {
                ProductIdentifier = productIdentifiers,
                CallbackUri = callbackUri
            };
            var result = validator.TestValidate(model);
            Assert.AreEqual(0, result.Errors.Count);
        }        
        #endregion
    }
}
