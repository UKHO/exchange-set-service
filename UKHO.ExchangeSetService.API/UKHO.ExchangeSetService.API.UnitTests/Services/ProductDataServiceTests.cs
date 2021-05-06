using FakeItEasy;
using FluentValidation.Results;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.UnitTests.Services
{
    [TestFixture]
    public class ProductDataServiceTests
    {
        private IProductIdentifierValidator fakeProductIdentifierValidator;
        private IProductDataService fakeProductDataService;

        [SetUp]
        public void Setup()
        {
            fakeProductIdentifierValidator = A.Fake<IProductIdentifierValidator>();

            fakeProductDataService = new ProductDataService(fakeProductIdentifierValidator);
        }

        #region ProductIdentifiers

        [Test]
        public async Task WhenInvalidProductIdentifierRequest_ThenValidateProductDataByProductIdentifiersReturnsBadrequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("ProductIdentifiers", "Product Identifiers cannot be blank or null.")}));

            var result = await fakeProductDataService.ValidateProductDataByProductIdentifiers(new ProductIdentifierRequest());
           
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Product Identifiers cannot be blank or null.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenInvalidNullProductIdentifierRequest_ThenValidateProductDataByProductIdentifiersReturnsBadrequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("RequestBody", "Either body is null or malformed.")}));

            var result = await fakeProductDataService.ValidateProductDataByProductIdentifiers(null);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Either body is null or malformed.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenValidProductIdentifierRequest_ThenValidateProductDataByProductIdentifierReturnsOkrequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;

            var result = await fakeProductDataService.ValidateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri
                });          
            
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task WhenValidProductIdentifierRequest_ThenCreateProductDataByProductIdentifierReturnsOkrequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));
            string[] productIdentifiers = new string[] { "GB123456", "GB160060", "AU334550" };
            string callbackUri = string.Empty;
            var result = await fakeProductDataService.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri
                });
            LinkSetBatchStatusUri linkSetBatchStatusUri = new LinkSetBatchStatusUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
            };
            LinkSetFileUri linkSetFileUri = new LinkSetFileUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip",
            };
            Links links = new Links()
            {
                ExchangeSetBatchStatusUri = linkSetBatchStatusUri,
                ExchangeSetFileUri = linkSetFileUri
            };
            List<RequestedProductsNotInExchangeSet> lstRequestedProductsNotInExchangeSet = new List<RequestedProductsNotInExchangeSet>()
            {
                new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "GB123456",
                    Reason = "productWithdrawn"
                },
                new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "GB123789",
                    Reason = "invalidProduct"
                }
            };
            ExchangeSetResponse exchangeSetResponse = new ExchangeSetResponse()
            {
                Links = links,
                ExchangeSetUrlExpiryDateTime = Convert.ToDateTime("2021-02-17T16:19:32.269Z").ToUniversalTime(),
                RequestedProductCount = 22,
                ExchangeSetCellCount = 15,
                RequestedProductsAlreadyUpToDateCount = 5,
                RequestedProductsNotInExchangeSet = lstRequestedProductsNotInExchangeSet
            };
            Assert.AreEqual(exchangeSetResponse.ExchangeSetCellCount,result.ExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponse.RequestedProductCount, result.RequestedProductCount);
        }
        #endregion
    }
}
