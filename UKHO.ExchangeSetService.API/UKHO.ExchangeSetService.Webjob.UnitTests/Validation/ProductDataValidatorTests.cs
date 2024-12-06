using System;
using System.Collections.Generic;
using System.Net;
using FluentValidation.TestHelper;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Validation;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Validation
{
    public class ProductDataValidatorTests
    {
        private ProductDataValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new ProductDataValidator();
        }

        private static List<Products> GetProducts()
        {
            return
            [
                new Products
                {
                    ProductName = "DE110000",
                    EditionNumber = 6,
                    UpdateNumbers = [0, 1],
                    Dates =
                    [
                        new Dates
                        {
                            UpdateNumber = 0,
                            UpdateApplicationDate = DateTime.Now,
                            IssueDate = DateTime.Now,
                        }
                    ],
                    Cancellation = null,
                    FileSize = 1803557,
                    IgnoreCache = false,
                    Bundle =
                    [
                        new Bundle
                        {
                            BundleType = "DVD",
                            Location = "M2;B8"
                        }
                    ]
                }
            ];
        }

        #region Product Data

        [Test]
        public void WhenEmptyProduct_ThenReturnBadRequest()
        {
            var fakeproducts = new List<Products>() { };

            var result = _validator.TestValidate(fakeproducts);

            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo("Products cannot be null or empty."));
        }

        [Test]
        public void WhenEmptyBundleTypeProduct_ThenReturnBadRequest()
        {
            var fakeproducts = GetProducts();
            fakeproducts[0].Bundle[0].BundleType = string.Empty;

            var result = _validator.TestValidate(fakeproducts);

            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo("BundleType value cannot not be null or empty and must be DVD."));
        }

        [Test]
        public void WhenNullBundleTypeProduct_ThenReturnBadRequest()
        {
            var fakeproducts = GetProducts();
            fakeproducts[0].Bundle[0].BundleType = null;

            var result = _validator.TestValidate(fakeproducts);

            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo("BundleType value cannot not be null or empty and must be DVD."));
        }

        [Test]
        public void WhenInvalidBundleTypeProduct_ThenReturnBadRequest()
        {
            var fakeproducts = GetProducts();
            fakeproducts[0].Bundle[0].BundleType = "AVCS";

            var result = _validator.TestValidate(fakeproducts);

            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo("BundleType value cannot not be null or empty and must be DVD."));
        }

        [Test]
        public void WhenEmptyLocationProduct_ThenReturnBadRequest()
        {
            var fakeproducts = GetProducts();
            fakeproducts[0].Bundle[0].Location = string.Empty;

            var result = _validator.TestValidate(fakeproducts);

            Assert.That(result.Errors[0].ErrorCode, Is.EqualTo(HttpStatusCode.BadRequest.ToString()));
        }

        [Test]
        public void WhenNullLocationProduct_ThenReturnBadRequest()
        {
            var fakeproducts = GetProducts();
            fakeproducts[0].Bundle[0].Location = null;

            var result = _validator.TestValidate(fakeproducts);

            Assert.That(result.Errors[0].ErrorCode, Is.EqualTo(HttpStatusCode.BadRequest.ToString()));
        }

        [Test]
        public void WhenInvalidLocationProduct_ThenReturnBadRequest()
        {
            var fakeproducts = GetProducts();
            fakeproducts[0].Bundle[0].Location = "M03;B1";

            var result = _validator.TestValidate(fakeproducts);

            Assert.That(result.Errors[0].ErrorCode, Is.EqualTo(HttpStatusCode.BadRequest.ToString()));
        }

        [Test]
        public void WhenInvalidBaseProduct_ThenReturnBadRequest()
        {
            var fakeproducts = GetProducts();
            fakeproducts[0].Bundle[0].Location = "M01;B0";

            var result = _validator.TestValidate(fakeproducts);

            Assert.That(result.Errors[0].ErrorCode, Is.EqualTo(HttpStatusCode.BadRequest.ToString()));
        }

        [Test]
        public void WhenValidProduct_ThenReturnSuccess()
        {
            var result = _validator.TestValidate(GetProducts());

            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        #endregion
    }
}
