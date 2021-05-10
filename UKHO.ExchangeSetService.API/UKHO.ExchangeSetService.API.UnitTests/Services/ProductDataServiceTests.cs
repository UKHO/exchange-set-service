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
        private IProductDataProductVersionsValidator fakeProductVersionValidator;
        private IProductDataSinceDateTimeValidator fakeProductDataSinceDateTimeValidator;
        private ProductDataService service;

        [SetUp]
        public void Setup()
        {
            fakeProductIdentifierValidator = A.Fake<IProductIdentifierValidator>();
            fakeProductVersionValidator = A.Fake<IProductDataProductVersionsValidator>();
            fakeProductDataSinceDateTimeValidator = A.Fake<IProductDataSinceDateTimeValidator>();
            service = new ProductDataService(fakeProductIdentifierValidator,fakeProductVersionValidator, fakeProductDataSinceDateTimeValidator);
        }

        #region GetExchangeSetResponse

        private ExchangeSetResponse GetExchangeSetResponse()
        {
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
            return exchangeSetResponse;
        }

        #endregion GetExchangeSetResponse


        #region ProductIdentifiers

        [Test]
        public async Task WhenInvalidProductIdentifierRequest_ThenValidateProductDataByProductIdentifiersReturnsBadrequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("ProductIdentifiers", "Product Identifiers cannot be blank or null.")}));

            var result = await service.ValidateProductDataByProductIdentifiers(new ProductIdentifierRequest());

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Product Identifiers cannot be blank or null.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenInvalidNullProductIdentifierRequest_ThenValidateProductDataByProductIdentifiersReturnsBadrequest()
        {
            A.CallTo(() => fakeProductIdentifierValidator.Validate(A<ProductIdentifierRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("RequestBody", "Either body is null or malformed.")}));

            var result = await service.ValidateProductDataByProductIdentifiers(null);
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

            var result = await service.ValidateProductDataByProductIdentifiers(
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
            var result = await service.CreateProductDataByProductIdentifiers(
                new ProductIdentifierRequest()
                {
                    ProductIdentifier = productIdentifiers,
                    CallbackUri = callbackUri
                });
            var exchangeSetResponse = GetExchangeSetResponse();

            Assert.AreEqual(exchangeSetResponse.ExchangeSetCellCount, result.ExchangeSetCellCount);
            Assert.AreEqual(exchangeSetResponse.RequestedProductCount, result.RequestedProductCount);
            Assert.AreEqual(exchangeSetResponse.RequestedProductsAlreadyUpToDateCount, result.RequestedProductsAlreadyUpToDateCount);
        }
        #endregion

        #region ProductVersions

        [Test]
        public async Task WhenInvalidProductVersionRequest_ThenValidateProductDataByProductVersionsReturnsBadrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("ProductName", "ProductName cannot be blank or null.")}));

            var result = await service.ValidateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            { ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest() { ProductName = null } } });

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("ProductName cannot be blank or null.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenInvalidNullProductVersionRequest_ThenValidateProductDataByProductVersionsReturnsBadrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("RequestBody", "Either body is null or malformed.")}));

            var result = await service.ValidateProductDataByProductVersions(null);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Either body is null or malformed.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenValidProductVersionRequest_ThenValidateProductDataByProductVersionsReturnsOkrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            var result = await service.ValidateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            { ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest() { ProductName = "Demo", EditionNumber = 5, UpdateNumber = 0 } } });

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task WhenValidProductVersionRequest_ThenCreateProductDataByProductVersionsReturnsOkrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            var result = await service.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            {
                ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest {
                ProductName = "GB123789", EditionNumber = 6, UpdateNumber = 3 } },
                CallbackUri = ""
            });

            Assert.IsInstanceOf<ExchangeSetResponse>(result);
        }
        #endregion

        #region ProductDataSinceDateTime

        [Test]
        public async Task WhenInvalidSinceDateTimeInRequest_ThenValidateProductDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                            { new ValidationFailure("SinceDateTime", "Provided since date time is either invalid or invalid format, the valid format is 'RFC1123 format'.")}));

            var result = await service.ValidateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Provided since date time is either invalid or invalid format, the valid format is 'RFC1123 format'.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenSinceDateTimeFormatIsGreaterThanCurrrentDateTimeInRequest_ThenValidateProductDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                            { new ValidationFailure("SinceDateTime", "Provided since date time cannot be a future date.")}));

            var result = await service.ValidateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Provided since date time cannot be a future date.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenCallbackUrlParameterNotValidInRequest_ThenValidateProductDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                            { new ValidationFailure("callbackUrl", "Invalid CallbackUri format.")}));

            var result = await service.ValidateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Invalid CallbackUri format.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenValidateProductDataSinceDateTimeInRequest_ThenCreateProductDataSinceDateTimeReturnSuccess()
        {
            A.CallTo(() => fakeProductDataSinceDateTimeValidator.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());

            Assert.IsInstanceOf<ExchangeSetResponse>(result);
        }

        #endregion ProductDataSinceDateTime
    }
}
