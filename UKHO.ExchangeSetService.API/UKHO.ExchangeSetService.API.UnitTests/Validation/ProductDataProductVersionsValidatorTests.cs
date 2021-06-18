using FluentValidation.TestHelper;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.UnitTests.Validation
{
    public class ProductDataProductVersionsValidatorTests
    {
        private ProductDataProductVersionsValidator validator;

        [SetUp]
        public void Setup()
        {
            validator = new ProductDataProductVersionsValidator();
        }

        #region ProductVersions
        [Test]
        public void WhenInvalidCallbackuriInProductDataProductVersionRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataProductVersionsRequest { CallbackUri = "demo uri" };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.CallbackUri);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Invalid CallbackUri format."));
        }

        [Test]
        public void WhenValidCallbackuriInProductDataProductVersionRequest_ThenReturnSuccess()
        {
            var model = new ProductDataProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest>
                { new ProductVersionRequest {
                UpdateNumber = 0, EditionNumber = 0, ProductName = "productName" } },
                CallbackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234"
            };
            var result = validator.TestValidate(model);
            Assert.IsTrue(result.Errors.Count == 0);
        }

        [Test]
        public void WhenProductNameNullInProductDataProductVersionRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest> { new ProductVersionRequest {
            UpdateNumber = 0, EditionNumber = 0 } }
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor("ProductVersions[0].productName");
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "productName cannot be blank or null."));
        }

        [Test]
        public void WhenEditionNumberNullInProductDataProductVersionRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest> { new ProductVersionRequest {
            UpdateNumber = 0, ProductName = "productName" } }
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor("ProductVersions[0].editionNumber");
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "editionNumber cannot be less than zero or null."));
        }

        [Test]
        public void WhenUpdateNumberNullInProductDataProductVersionRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest> {
                    new ProductVersionRequest {
                        EditionNumber = 0, ProductName = "productName"
                    } }
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor("ProductVersions[0].updateNumber");
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "updateNumber cannot be less than zero or null."));
        }

        [Test]
        public void WhenEditionNumberLessThanZeroInProductDataProductVersionRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest> { new ProductVersionRequest {
            UpdateNumber = 0, EditionNumber = -8, ProductName = "productName" } }
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor("ProductVersions[0].editionNumber");
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "editionNumber cannot be less than zero or null."));
        }

        [Test]
        public void WhenUpdateNumberLessThanZeroInProductDataProductVersionRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest> {
                    new ProductVersionRequest {
                        EditionNumber = 0, UpdateNumber = -3, ProductName = "productName"
                    } }
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor("ProductVersions[0].updateNumber");
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "updateNumber cannot be less than zero or null."));
        }

        [Test]
        public void WhenValidRequestInProductDataProductVersionRequest_ThenReturnSuccess()
        {
            var model = new ProductDataProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest> {
                    new ProductVersionRequest {
                        EditionNumber = 0, UpdateNumber = 0, ProductName = "productName"
                    } }
            };
            var result = validator.TestValidate(model);
            Assert.IsTrue(result.Errors.Count == 0);
        }

        [Test]
        public void WhenProductVersionsNullInProductDataProductVersionRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataProductVersionsRequest
            {
                ProductVersions = null
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductVersions);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "ProductVersions cannot be null."));
        }
        #endregion
    }
}
