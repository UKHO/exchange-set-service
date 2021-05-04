using FluentValidation.Results;
using FluentValidation.TestHelper;
using NUnit.Framework;
using System;
using System.Linq;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.UnitTests.Validation
{
    public class ProductDataSinceDateTimeValidatorTests
    {
        private ProductDataSinceDateTimeValidator validator;

        [SetUp]
        public void Setup()
        {
            validator = new ProductDataSinceDateTimeValidator();
        }

        [Test()]
        public void WhenEmptySinceDateTimeInProductDataSinceDateTimeRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataSinceDateTimeRequest { SinceDateTime = null };
            var result = validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(fb => fb.SinceDateTime);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Provided since date time is either invalid or invalid format, the valid format is 'RFC1123 format'."));
        }

        [Test()]
        public void WhenInvalidSinceDateTimeInProductDataSinceDateTimeRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataSinceDateTimeRequest { SinceDateTime = "Wed, 21 Oct 2015" };
            var result = validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(fb => fb.SinceDateTime);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Provided since date time is either invalid or invalid format, the valid format is 'RFC1123 format'."));
        }

        [Test()]
        public void WhenSinceDateTimeGreaterThanCurrentDateTimeInProductDataSinceDateTimeRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataSinceDateTimeRequest { SinceDateTime = DateTime.UtcNow.AddDays(5).ToString("R") };
            var result = validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(fb => fb.SinceDateTime);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Provided since date time cannot be a future date."));
        } 

        [Test()]
        public void WhenInvalidCallBackUriInProductDataSinceDateTimeRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataSinceDateTimeRequest { SinceDateTime = "Wed, 21 Oct 2015 07:28:00 GMT", CallbackUri = "abc" };
            var result = validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(fb => fb.CallbackUri);
            Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Invalid CallbackUri format."));
        }

        [Test()]
        public void WhenValidInProductDataSinceDateTimeRequest_ThenReturnSuccess()
        {
            var model = new ProductDataSinceDateTimeRequest { SinceDateTime = "Wed, 21 Oct 2015 07:28:00 GMT", CallbackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234" };
            var result = validator.TestValidate(model);
            Assert.IsTrue(result.Errors.Count == 0);
        }
    }
}
