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
        private TestConfiguration Config { get; set; }

        [SetUp]
        public void Setup()
        {
            Config = new TestConfiguration();
            ExchangesetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
        }


        [Test]
        public async Task WhenICallTheApiWithAValidRFC1123DateTime_ThenACorrectResponseIsReturned()
        {
            string sincedatetime = "Mon, 01 Mar 2021 00:00:00 GMT";
            var apiresponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sincedatetime);
            Assert.AreEqual(200, (int)apiresponse.StatusCode, $"Incorrect status code is returned {apiresponse.StatusCode}, instead of the expected 200.");

            //verify model structure
            await apiresponse.CheckModelStructureForSuccessResponse();
        }

        [Test]
        public async Task WhenICallTheApiWithAValidRFC1123DateTimeAndValidCallbackURL_ThenASuccessStatusIsReturned()
        {
            string sincedatetime = "Mon, 01 Mar 2021 00:00:00 GMT";
            var apiresponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sincedatetime, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272");
            Assert.AreEqual(200, (int)apiresponse.StatusCode, $"Incorrect status code is returned {apiresponse.StatusCode}, instead of the expected 200.");

        }

        [Test]
        public async Task WhenICallTheApiWithAValidDateButNoLatestRelease_ThenANotModifiedResponseStatusIsReturned()
        {
            string sincedatetime = DateTime.UtcNow.AddDays(-1).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);
            var apiresponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sincedatetime, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272");
            Assert.AreEqual(304, (int)apiresponse.StatusCode, $"Incorrect status code is returned {apiresponse.StatusCode}, instead of the expected 304.");
        }

        [TestCase(0, TestName = "Current DateTime with valid RFC1123 format")]
        [TestCase(1, TestName = "Future DateTime with valid RFC1123 format")]
        public async Task WhenICallTheApiWithACurrentOrFutureRFC1123DateTime_ThenABadRequestStatusIsReturned(int days)
        {
            string sincedatetime = DateTime.Now.AddDays(days).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);
            var apiresponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sincedatetime);
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Incorrect status code is returned {apiresponse.StatusCode}, instead of the expected 400.");

            var errorMessage = await apiresponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "SinceDateTime"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Provided SinceDateTime cannot be a future date."));
        }


        [TestCase("Mon, 02 Mar 2021 00:00:00 GMT", TestName = "Invalid day but valid RFC1123 Format")]
        [TestCase("01 Mar 2021 00:00:00 GMT", TestName = "Invalid RFC format 'DD MMM YYYY HH24:MI:SS GMT'")]
        [TestCase("01 03 2021", TestName = "Invalid RFC format 'DD MM YYYY'")]
        public async Task WhenICallTheApiWithInValidRFC1123DateTime_ThenABadRequestStatusIsReturned(string sincedatetime)
        {
            var apiresponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sincedatetime);
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Incorrect status code is returned {apiresponse.StatusCode}, instead of the expected 400.");

            var errorMessage = await apiresponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "SinceDateTime"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Provided SinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format' (e.g. 'Wed, 21 Oct 2020 07:28:00 GMT')."));
        }


        [TestCase("", TestName = "Provided Empty DateTime in query parameter")]
        [TestCase(null, TestName = "Provided Null DateTime in query parameter")]
        public async Task WhenICallTheApiWithANullDateTime_ThenABadRequestStatusIsReturned(string sincedatetime)
        {
            var apiresponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sincedatetime);
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Incorrect status code is returned {apiresponse.StatusCode}, instead of the expected 400.");

            var errorMessage = await apiresponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "SinceDateTime"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Query parameter 'SinceDateTime' is required."));
        }

        [TestCase("fss.ukho.gov.uk", TestName = "Callback URL without https")]
        [TestCase("https:/fss.ukho.gov.uk", TestName = "Callback URL with wrong https request")]
        [TestCase("ftp://fss.ukho.gov.uk", TestName = "Callback URL with ftp request")]
        [TestCase("http://fss.ukho.gov.uk", TestName = "Callback URL with http request")]
        [TestCase("https://", TestName = "Callback URL with only https request")]
        public async Task WhenICallTheApiWithInvalidCallbackURI_ThenABadRequestResponseIsReturned(string callbackurl)
        {
            string sincedatetime = "Mon, 01 Mar 2021 00:00:00 GMT";
            var apiresponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sincedatetime, callbackurl);
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Incorrect status code is returned {apiresponse.StatusCode}, instead of the expected 400.");

            var errorMessage = await apiresponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "CallbackUri"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Invalid CallbackUri format."));
        }
    }
}
