using NUnit.Framework;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class ExchangeSetAfterSpecifiedDateTime
    {
        private ExchangeSetApiClient ExchangesetApiClient { get; set; }
        private Configuration Config { get; set; }

        [SetUp]
        public void Setup()
        {
            Config = new Configuration();
            ExchangesetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
        }
        [Test]
        public async Task WhenICallTheApiWithAValidRFC1123DateTime_ThenASuccessStatusIsReturned()
        {
            string sincedatetime = "Mon, 01 Mar 2021 00:00:00 GMT";
            var apiresponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sincedatetime);
            Assert.AreEqual(200, (int)apiresponse.StatusCode, $"Exchange Set for datetime is  returned {apiresponse.StatusCode}, instead of the expected 200.");
        }

        [Test]
        public async Task WhenICallTheApiWithAFutureRFC1123DateTime_ThenABadRequestStatusIsReturned()
        {
            string sincedatetime = DateTime.Now.AddDays(1).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);
            var apiresponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sincedatetime);
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Exchange Set for datetime is  returned {apiresponse.StatusCode}, instead of the expected 400.");
            
            var errorMessage = await apiresponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "sinceDateTime"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Since date time cannot be a future date."));
        }

        [Test]
        public async Task WhenICallTheApiWithACurrentRFC1123DateTime_ThenABadRequestStatusIsReturned()
        {
            string sincedatetime = DateTime.Now.ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);
            var apiresponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sincedatetime);
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Exchange Set for datetime is  returned {apiresponse.StatusCode}, instead of the expected 400.");
            
            var errorMessage = await apiresponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "sinceDateTime"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Since date time cannot be a future date."));
        }

        [TestCase("01 Mar 2021 00:00:00 GMT", TestName="Invalid RFC format 'DD MMM YYYY HH24:MI:SS GMT'")]
        [TestCase("01 03 2021", TestName = "Invalid RFC format 'DD MM YYYY'")]
        public async Task WhenICallTheApiWithInValidRFC1123DateTime_ThenABadRequestStatusIsReturned(string sincedatetime)
        {
            var apiresponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sincedatetime);
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Exchange Set for datetime is  returned {apiresponse.StatusCode}, instead of the expected 400.");
            
            var errorMessage = await apiresponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "sinceDateTime"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Provided since date time is either invalid or invalid format, the valid formats are 'RFC1123 formats'."));
        }




    }
}
