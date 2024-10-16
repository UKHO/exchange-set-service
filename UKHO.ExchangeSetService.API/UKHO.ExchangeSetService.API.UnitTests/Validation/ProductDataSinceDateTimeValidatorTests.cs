using FluentValidation.Results;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.UnitTests.Validation
{
    public class ProductDataSinceDateTimeValidatorTests
    {
        private ProductDataSinceDateTimeValidator validator;
        private IConfiguration configuration;
        
        [SetUp]
        public void Setup()
        {
            var inMemorySettings = new Dictionary<string, string> {
                {"MaximumNumerOfDaysValidForSinceDateTimeEndpoint", "28"}};

            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            validator = new ProductDataSinceDateTimeValidator(configuration);
        }

        [Test]
        public void WhenEmptySinceDateTimeInProductDataSinceDateTimeRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataSinceDateTimeRequest { SinceDateTime = null };
            var result = validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(fb => fb.SinceDateTime);
            Assert.That(result.Errors.Any(x => x.ErrorMessage == "Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format' (e.g. 'Wed, 21 Oct 2020 07:28:00 GMT')."));
        }

        [Test]
        public void WhenInvalidSinceDateTimeInProductDataSinceDateTimeRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataSinceDateTimeRequest { SinceDateTime = "Wed, 21 Oct 2015" };
            var result = validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(fb => fb.SinceDateTime);
            Assert.That(result.Errors.Any(x => x.ErrorMessage == "Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format' (e.g. 'Wed, 21 Oct 2020 07:28:00 GMT')."));
        }

        [Test]
        public void WhenSinceDateTimeGreaterThanCurrentDateTimeInProductDataSinceDateTimeRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataSinceDateTimeRequest { SinceDateTime = DateTime.UtcNow.AddDays(5).ToString("R") };
            var result = validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(fb => fb.SinceDateTime);
            Assert.That(result.Errors.Any(x => x.ErrorMessage == "Provided sinceDateTime cannot be a future date."));
        } 

        [Test]
        public void WhenInvalidCallBackUriInProductDataSinceDateTimeRequest_ThenReturnBadRequest()
        {
            var model = new ProductDataSinceDateTimeRequest { SinceDateTime = "Wed, 21 Oct 2015 07:28:00 GMT", CallbackUri = "abc" };
            var result = validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(fb => fb.CallbackUri);
            Assert.That(result.Errors.Any(x => x.ErrorMessage == "Invalid callbackUri format."));
        }

        [Test]
        public void WhenValidInProductDataSinceDateTimeRequest_ThenReturnSuccess()
        {
            var model = new ProductDataSinceDateTimeRequest { SinceDateTime = DateTime.UtcNow.AddDays(-7).ToString("R"), CallbackUri = "https://exchange-set-service.com/myCallback?secret=sharedSecret&po=1234" };
            var result = validator.TestValidate(model);
            Assert.That(result.Errors.Count == 0);
        }

        [Test]
        public void WhenSinceDateTimeLessThanMaximumNumerOfDaysValidInProductDataSinceDateTimeRequest_ThenReturnBadRequest()
        {
            int maximumNumerOfDaysValidForSinceDateTimeEndpoint = (Convert.ToInt32(configuration.GetValue<string>("MaximumNumerOfDaysValidForSinceDateTimeEndpoint")));
            var model = new ProductDataSinceDateTimeRequest { SinceDateTime = DateTime.UtcNow.AddDays(-maximumNumerOfDaysValidForSinceDateTimeEndpoint).ToString("R") };
            var result = validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(fb => fb.SinceDateTime);
            string errorMessage = "Provided sinceDateTime must be within last " + configuration.GetValue<string>("MaximumNumerOfDaysValidForSinceDateTimeEndpoint") + " days.";
            Assert.That(result.Errors.Any(x => x.ErrorMessage == errorMessage));
        }

        [Test]
        public void WhenSinceDateTimeWithinMaximumNumerOfDaysValidInProductDataSinceDateTimeRequest_ThenReturnSuccess()
        {
            var model = new ProductDataSinceDateTimeRequest { SinceDateTime = DateTime.UtcNow.AddDays(-1).ToString("R") };
            var result = validator.TestValidate(model);
            Assert.That(result.Errors.Count == 0);
        }
    }
}
