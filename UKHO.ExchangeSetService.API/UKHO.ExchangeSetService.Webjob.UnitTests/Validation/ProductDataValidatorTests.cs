using FluentValidation.TestHelper;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Validation;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Validation
{
    public class ProductDataValidatorTests
    {
        private ProductDataValidator validator;

        [SetUp]
        public void Setup()
        {
            validator = new ProductDataValidator();
        }

        public List<Products> GetProducts()
        {
            List<Products> products = new List<Products>()
            {
                new Products()
                {
                    ProductName = "DE110000",
                    EditionNumber = 6,
                    UpdateNumbers = new List<int?> { 0, 1 },
                    Dates = new List<Dates>()
                    {
                        new Dates()
                        {
                            UpdateNumber = 0,
                            UpdateApplicationDate = DateTime.Now,
                            IssueDate = DateTime.Now,
                        }
                    },
                    Cancellation = null,
                    FileSize = 1803557,
                    IgnoreCache = false,
                    Bundle = new List<Bundle>()
                    {
                        new Bundle()
                        {
                            BundleType = "DVD",
                            Location = "M2;B8"
                        }
                    }
                }
            };

            return products;
        }

        #region Product Data

        [Test]
        public void WhenEmptyProduct_ThenReturnBadRequest()
        {
            List<Products> fakeproducts = new List<Products>() { };

            var result = validator.TestValidate(fakeproducts);

            Assert.IsTrue(result.Errors[0].ErrorMessage.Equals("Products cannot be null or empty."));
        }

        [Test]
        public void WhenEmptyBundleTypeProduct_ThenReturnBadRequest()
        {
            var fakeproducts = GetProducts();
            fakeproducts[0].Bundle[0].BundleType = String.Empty;

            var result = validator.TestValidate(fakeproducts);

            Assert.IsTrue(result.Errors[0].ErrorMessage.Equals("BundleType value cannot not be null or empty and must be DVD."));
        }

        [Test]
        public void WhenNullBundleTypeProduct_ThenReturnBadRequest()
        {
            var fakeproducts = GetProducts();
            fakeproducts[0].Bundle[0].BundleType = null;

            var result = validator.TestValidate(fakeproducts);

            Assert.IsTrue(result.Errors[0].ErrorMessage.Equals("BundleType value cannot not be null or empty and must be DVD."));
        }

        [Test]
        public void WhenInvalidBundleTypeProduct_ThenReturnBadRequest()
        {
            var fakeproducts = GetProducts();
            fakeproducts[0].Bundle[0].BundleType = "AVCS";

            var result = validator.TestValidate(fakeproducts);

            Assert.IsTrue(result.Errors[0].ErrorMessage.Equals("BundleType value cannot not be null or empty and must be DVD."));
        }

        [Test]
        public void WhenEmptyLocationProduct_ThenReturnBadRequest()
        {
            var fakeproducts = GetProducts();
            fakeproducts[0].Bundle[0].Location = String.Empty;

            var result = validator.TestValidate(fakeproducts);

            Assert.IsTrue(result.Errors[0].ErrorCode.Equals(HttpStatusCode.BadRequest.ToString()));
        }

        [Test]
        public void WhenNullLocationProduct_ThenReturnBadRequest()
        {
            var fakeproducts = GetProducts();
            fakeproducts[0].Bundle[0].Location = null;

            var result = validator.TestValidate(fakeproducts);

            Assert.IsTrue(result.Errors[0].ErrorCode.Equals(HttpStatusCode.BadRequest.ToString()));
        }

        [Test]
        public void WhenInvalidLocationProduct_ThenReturnBadRequest()
        {
            var fakeproducts = GetProducts();
            fakeproducts[0].Bundle[0].Location = "M03;B1";

            var result = validator.TestValidate(fakeproducts);

            Assert.IsTrue(result.Errors[0].ErrorCode.Equals(HttpStatusCode.BadRequest.ToString()));
        }

        [Test]
        public void WhenInvalidBaseProduct_ThenReturnBadRequest()
        {
            var fakeproducts = GetProducts();
            fakeproducts[0].Bundle[0].Location = "M01;B0";

            var result = validator.TestValidate(fakeproducts);

            Assert.IsTrue(result.Errors[0].ErrorCode.Equals(HttpStatusCode.BadRequest.ToString()));
        }

        [Test]
        public void WhenValidProduct_ThenReturnSuccess()
        {
            var result = validator.TestValidate(GetProducts());

            Assert.IsTrue(result.Errors.Count == 0);
        }

        #endregion
    }
}
