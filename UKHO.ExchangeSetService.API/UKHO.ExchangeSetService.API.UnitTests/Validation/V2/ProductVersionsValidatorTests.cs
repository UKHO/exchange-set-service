using FluentAssertions;
using FluentValidation.TestHelper;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net;
using UKHO.ExchangeSetService.API.Validation.V2;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.UnitTests.Validation.V2
{
    public class ProductVersionsValidatorTests
    {
        private ProductVersionsValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new ProductVersionsValidator();
        }        

        [TestCase("http://invaliduri.com")]
        [TestCase("https://]validuri.com")]
        public void WhenCallbackUriIsNotHttpsOrInvalidFormatInProductVersionsRequest_ThenReturnBadRequest(string callbackUri)
        {
            var model = new ProductVersionsRequest
            {
                CallbackUri = callbackUri,
                ProductVersions = new List<ProductVersionRequest>
                {
                    new() { ProductName = "Product1", EditionNumber = 1, UpdateNumber = 1 }
                }
            };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.CallbackUri).WithErrorMessage("Invalid callbackUri format.").WithErrorCode(HttpStatusCode.BadRequest.ToString());
        }

        [Test]
        public void WhenCallbackUriIsValidInProductVersionRequest_ThenReturnSuccesss()
        {
            var model = new ProductVersionsRequest
            {
                CallbackUri = "https://validuri.com",
                ProductVersions = new List<ProductVersionRequest>
                {
                    new () { ProductName = "Product1", EditionNumber = 1, UpdateNumber = 1 }
                }
            };
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.CallbackUri);
            result.Errors.Count.Should().Be(0);
        }

        [Test]
        public void WhenProductVersionsIsNullInProductVersionsRequest_ThenReturnBadRequest()
        {
            var model = new ProductVersionsRequest { ProductVersions = null };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.ProductVersions).WithErrorMessage("productVersions cannot be null or empty.").WithErrorCode(HttpStatusCode.BadRequest.ToString());
        }

        [Test]
        public void WhenProductVersionsIsEmptyInProductVersionsRequest_ThenReturnBadRequest()
        {
            var model = new ProductVersionsRequest { ProductVersions = new List<ProductVersionRequest>() };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.ProductVersions).WithErrorMessage("productVersions cannot be null or empty.").WithErrorCode(HttpStatusCode.BadRequest.ToString());
        }

        [Test]
        public void WhenValidProductVersionsRequest_ThenReturnSuccess()
        {
            var model = new ProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest>
                {
                    new () { ProductName = "Product1", EditionNumber = 1, UpdateNumber = 1 }
                }
            };
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.ProductVersions);
            result.Errors.Count.Should().Be(0);
        }

        [TestCase(null)]
        [TestCase("")]
        public void WhenProductNameIsNullInProductVersionsRequest_ThenReturnsBadRequest(string productName)
        {
            var model = new ProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest>
                {
                    new() { ProductName = productName, EditionNumber = 1, UpdateNumber = 1 }
                }
            };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor("ProductVersions[0].productName").WithErrorMessage("productName cannot be blank or null.").WithErrorCode(HttpStatusCode.BadRequest.ToString());
        }

        [TestCase(null)]
        [TestCase(-1)]
        public void WhenEditionNumberIsNullOrLessThan0InProductVersionsRequest_ThenReturnsBadRequest(int? editionNmer)
        {
            var model = new ProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest>
                {
                    new () { ProductName = "Product1", EditionNumber = editionNmer, UpdateNumber = 1 }
                }
            };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor("ProductVersions[0].editionNumber").WithErrorMessage("editionNumber cannot be less than zero or null.").WithErrorCode(HttpStatusCode.BadRequest.ToString());
        }

        [TestCase(null)]
        [TestCase(-1)]
        public void WhenUpdateNumberIsNullOrLessThan0InProductVersionsRequest_ThenReturnsBadRequest(int? updateNumber)
        {
            var model = new ProductVersionsRequest
            {
                ProductVersions = new List<ProductVersionRequest>
                {
                    new () { ProductName = "Product1", EditionNumber = 1, UpdateNumber = updateNumber }
                }
            };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor("ProductVersions[0].updateNumber").WithErrorMessage("updateNumber cannot be less than zero or null.").WithErrorCode(HttpStatusCode.BadRequest.ToString());
        }
    }
}

