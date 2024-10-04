using NUnit.Framework;
using System.Linq;
using FluentValidation.TestHelper;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.UnitTests.Validation
{
    public class ScsProductIdentifiersValidatorTests
    {
        private ScsProductIdentifierValidator validator;

        [SetUp]
        public void Setup()
        {
            validator = new ScsProductIdentifierValidator();
        }

        #region Scs Product Identifiers

        [Test]
        public void WhenNullScsProductIdentifierInProductIdentifierRequest_ThenReturnBadRequest()
        {
            string[] productIdentifiers = null;
            var model = new ScsProductIdentifierRequest
            {
                ProductIdentifier = productIdentifiers
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductIdentifier);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "productIdentifiers cannot be null or empty."));
        }

        [Test]
        public void WhenEmptyScsProductIdentifiersInProductIdentifiersRequest_ThenReturnBadRequest()
        {
            string[] productIdentifiers = new string[] { string.Empty };
            var model = new ScsProductIdentifierRequest
            {
                ProductIdentifier = productIdentifiers
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductIdentifier);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "productIdentifiers cannot be null or empty."));
        }


        [Test]
        public void WhenValidScsProductIdentifiersAndvalidCallBackuriInProductIdentifiersRequest_ThenReturnSuccess()
        {
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            var model = new ScsProductIdentifierRequest
            {
                ProductIdentifier = productIdentifiers,
            };
            var result = validator.TestValidate(model);
            Assert.That(0, Is.EqualTo(result.Errors.Count));
        }
        #endregion
    }
}
