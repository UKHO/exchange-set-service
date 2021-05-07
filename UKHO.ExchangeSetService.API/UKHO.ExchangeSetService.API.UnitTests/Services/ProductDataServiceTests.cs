using FakeItEasy;
using FluentValidation.Results;
using NUnit.Framework;
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
        private IProductDataSinceDateTimeValidator fakeIProductDataValidatorService;
        private ProductDataService service;

        [SetUp]
        public void Setup()
        {
            fakeIProductDataValidatorService = A.Fake<IProductDataSinceDateTimeValidator>();
            service = new ProductDataService(fakeIProductDataValidatorService);
        }

        #region ProductDataSinceDateTime

        [Test]
        public async Task WhenInvalidSinceDateTimeInRequest_ThenValidateProductDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeIProductDataValidatorService.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                            { new ValidationFailure("SinceDateTime", "Provided since date time is either invalid or invalid format, the valid format is 'RFC1123 format'.")}));

            var result = await service.ValidateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Provided since date time is either invalid or invalid format, the valid format is 'RFC1123 format'.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenSinceDateTimeFormatIsGreaterThanCurrrentDateTimeInRequest_ThenValidateProductDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeIProductDataValidatorService.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                            { new ValidationFailure("SinceDateTime", "Provided since date time cannot be a future date.")}));

            var result = await service.ValidateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Provided since date time cannot be a future date.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenCallbackUrlParameterNotValidInRequest_ThenValidateProductDataSinceDateTimeReturnsBadRequest()
        {
            A.CallTo(() => fakeIProductDataValidatorService.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                            { new ValidationFailure("callbackUrl", "Invalid CallbackUri format.")}));

            var result = await service.ValidateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Invalid CallbackUri format.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenValidateProductDataSinceDateTimeInRequest_ThenCreateProductDataSinceDateTimeReturnSuccess()
        {
            A.CallTo(() => fakeIProductDataValidatorService.Validate(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            var result = await service.CreateProductDataSinceDateTime(new ProductDataSinceDateTimeRequest());
            Assert.IsInstanceOf<ExchangeSetResponse>(result);
        }

        #endregion ProductDataSinceDateTime
    }
}
