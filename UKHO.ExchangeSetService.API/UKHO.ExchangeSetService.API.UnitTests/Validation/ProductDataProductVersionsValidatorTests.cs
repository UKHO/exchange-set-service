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
        public void WhenInvalidCallbackuriInProductDataProductVesrionRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataProductVersionsRequest { CallbackUri = "demo uri" };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.CallbackUri);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Invalid CallbackUri format."));
        }

        [Test]
        public void WhenValidCallbackuriInProductDataProductVesrionRequest_ThenReturnSuccess()
        {
            var model = new ProductDataProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest>
                { new ProductVersionRequest {
                UpdateNumber = 0, EditionNumber = 0, ProductName = "ProductName" } },
                CallbackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234"
            };
            var result = validator.TestValidate(model);
            Assert.IsTrue(result.Errors.Count == 0);
        }

        [Test]
        public void WhenProductNameNullInProductDataProductVesrionRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest> { new ProductVersionRequest {
            UpdateNumber = 0, EditionNumber = 0 } }
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductVersions);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Product Versions product name cannot be blank or null."));
        }

        [Test]
        public void WhenEditionNumberNullInProductDataProductVesrionRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest> { new ProductVersionRequest {
            UpdateNumber = 0, ProductName = "ProductName" } }
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductVersions);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Product Versions edition number cannot be less than zero or null."));
        }

        [Test]
        public void WhenUpdateNumberNullInProductDataProductVesrionRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest> {
                    new ProductVersionRequest {
                        EditionNumber = 0, ProductName = "ProductName"
                    } }
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductVersions);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Product Versions update number cannot be less than zero or null."));
        }

        [Test]
        public void WhenEditionNumberLessThanZeroInProductDataProductVesrionRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest> { new ProductVersionRequest {
            UpdateNumber = 0, EditionNumber = -8, ProductName = "ProductName" } }
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductVersions);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Product Versions edition number cannot be less than zero or null."));
        }

        [Test]
        public void WhenUpdateNumberLessThanZeroInProductDataProductVesrionRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest> {
                    new ProductVersionRequest {
                        EditionNumber = 0, UpdateNumber = -3, ProductName = "ProductName"
                    } }
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductVersions);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Product Versions update number cannot be less than zero or null."));
        }

        [Test]
        public void WhenValidRequestInProductDataProductVesrionRequest_ThenReturnSuccess()
        {
            var model = new ProductDataProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest> {
                    new ProductVersionRequest {
                        EditionNumber = 0, UpdateNumber = 0, ProductName = "ProductName"
                    } }
            };
            var result = validator.TestValidate(model);
            Assert.IsTrue(result.Errors.Count == 0);
        }

        [Test]
        public void WhenProductVersionsNullInProductDataProductVesrionRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataProductVersionsRequest
            {
                ProductVersions = null
            };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(fb => fb.ProductVersions);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Product Versions cannot be null."));
        }
        #endregion
    }
}
