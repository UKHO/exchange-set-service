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
            Assert.AreEqual(200, (int)apiresponse.StatusCode, $"Exchange Set for datetime is  returned {apiresponse.StatusCode}, instead of the expected 200.");

            var apiresponsedata = await apiresponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            Assert.IsNotNull(apiresponsedata.Links.ExchangeSetBatchStatusUri.Href, "Response body returns null, instead of expected links should be not null.");
            Assert.IsTrue(Uri.IsWellFormedUriString(apiresponsedata.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned batch status URI {apiresponsedata.Links.ExchangeSetBatchStatusUri.Href}, Its not valid uri");
            Assert.IsNotNull(apiresponsedata.Links.ExchangeSetFileUri.Href, "Response body returns null instead of valid links.");
            Assert.IsTrue(Uri.IsWellFormedUriString(apiresponsedata.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned file URI {apiresponsedata.Links.ExchangeSetFileUri.Href}, Its not valid uri");

            Assert.AreEqual("2021-02-17T16:19:32.269Z",apiresponsedata.ExchangeSetUrlExpiryDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffK", CultureInfo.InvariantCulture), $"Exchange set returned URL expiry date time {apiresponsedata.ExchangeSetUrlExpiryDateTime}, instead of expected URL expiry date time '2021 - 02 - 17T16: 19:32.269Z'");
            Assert.IsTrue(apiresponsedata.RequestedProductCount >= 0, "Response body returns zero, instead of expected Product Count should be not zero.");
            Assert.AreEqual(apiresponsedata.RequestedProductCount, Is.TypeOf<int>(), $"Exchange set returned Requested Product Count {apiresponsedata.RequestedProductCount}, Its not valid.");

            Assert.IsTrue(apiresponsedata.ExchangeSetCellCount >= 0, "Response body returns zero, instead of expected Exchange Set Cell Count should be not zero.");
            Assert.AreEqual(apiresponsedata.ExchangeSetCellCount, Is.TypeOf<int>(), $"Exchange set returned Exchange Set Cell Count {apiresponsedata.RequestedProductCount}, Its not valid.");

            Assert.IsTrue(apiresponsedata.RequestedProductsAlreadyUpToDateCount >= 0, "Response body returns zero, instead of expected Requested Products already UptoDate Count should be not zero.");
            Assert.AreEqual(apiresponsedata.RequestedProductsAlreadyUpToDateCount, Is.TypeOf<int>(), $"Exchange set returned Requested Products already UptoDate Count {apiresponsedata.RequestedProductCount}, Its not valid.");

            Assert.IsNotNull(apiresponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName, "Response body returns null instead of valid Product Name.");
            Assert.IsNotNull(apiresponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, "Response body returns null instead of valid Reason.");
            Assert.IsNotNull(apiresponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, "Response body returns null instead of valid Product Name.");
            Assert.IsNotNull(apiresponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().Reason, "Response body returns null instead of valid Reason.");
        }

        [Test]
        public async Task WhenICallTheApiWithAValidRFC1123DateTimeAndValidCallbackURL_ThenASuccessStatusIsReturned()
        {
            string sincedatetime = "Mon, 01 Mar 2021 00:00:00 GMT";
            var apiresponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sincedatetime, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272");
            Assert.AreEqual(200, (int)apiresponse.StatusCode, $"Exchange Set for datetime is  returned {apiresponse.StatusCode}, instead of the expected 200.");

        }

        [TestCase(0, TestName = "Current DateTime with valid RFC1123 format")]
        [TestCase(1, TestName = "Future DateTime with valid RFC1123 format")]
        public async Task WhenICallTheApiWithACurrentOrFutureRFC1123DateTime_ThenABadRequestStatusIsReturned(int days)
        {
            string sincedatetime = DateTime.Now.AddDays(days).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);
            var apiresponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sincedatetime);
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Exchange Set for datetime is  returned {apiresponse.StatusCode}, instead of the expected 400.");

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
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Exchange Set for datetime is  returned {apiresponse.StatusCode}, instead of the expected 400.");

            var errorMessage = await apiresponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "SinceDateTime"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Provided SinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format' (e.g. 'Wed, 21 Oct 2020 07:28:00 GMT')."));
        }


        [TestCase("", TestName = "Provided Empty DateTime in query parameter")]
        [TestCase(null, TestName = "Provided Null DateTime in query parameter")]
        public async Task WhenICallTheApiWithANullDateTime_ThenABadRequestStatusIsReturned(string sincedatetime)
        {
            var apiresponse = await ExchangesetApiClient.GetExchangeSetBasedOnDateTimeAsync(sincedatetime);
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Exchange Set for datetime is  returned {apiresponse.StatusCode}, instead of the expected 400.");

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
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Exchange Set for datetime is  returned {apiresponse.StatusCode}, instead of the expected 400.");

            var errorMessage = await apiresponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "CallbackUri"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Invalid CallbackUri format."));
        }


    }
}
