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
          
            Assert.AreEqual("http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272", apiresponsedata.Links.ExchangeSetBatchStatusUri.Href, $"Exchange set returned batch status URI {apiresponsedata.Links.ExchangeSetBatchStatusUri.Href}, instead of expected batch status URI 'http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272'");
            Assert.AreEqual("http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip", apiresponsedata.Links.ExchangeSetFileUri.Href, $"Exchange set returned file URI {apiresponsedata.Links.ExchangeSetFileUri.Href}, instead of expected file URI 'http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip'");
                      
            Assert.AreEqual("2021-02-17T16:19:32.269Z",apiresponsedata.ExchangeSetUrlExpiryDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffK", CultureInfo.InvariantCulture), $"Exchange set returned URL expiry date time {apiresponsedata.ExchangeSetUrlExpiryDateTime}, instead of expected URL expiry date time '2021 - 02 - 17T16: 19:32.269Z'");
            Assert.AreEqual(22, apiresponsedata.RequestedProductCount, $"Exchange set returned Requested Product Count {apiresponsedata.RequestedProductCount}, instead of expected Requested Product Count '22'");
            Assert.AreEqual(15, apiresponsedata.ExchangeSetCellCount, $"Exchange set returned Exchange Set Cell Count {apiresponsedata.ExchangeSetCellCount}, instead of expected Exchange Set Cell Count '15'");
            Assert.AreEqual(5, apiresponsedata.RequestedProductsAlreadyUpToDateCount, $"Exchange set returned Requested Products Already UpDate Count {apiresponsedata.RequestedProductsAlreadyUpToDateCount}, instead of expected Products Already UpDate Count '5'");
            Assert.AreEqual("GB123456", apiresponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName, $"Exchange set returned Product Name {apiresponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName}, instead of expected Product Name 'GB123456'");
            Assert.AreEqual("productWithdrawn", apiresponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, $"Exchange set returned Reason {apiresponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'productWithdrawn'");
            Assert.AreEqual("GB123789", apiresponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, $"Exchange set returned Product Name {apiresponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'GB123789'");
            Assert.AreEqual("invalidProduct", apiresponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().Reason, $"Exchange set returned Reason {apiresponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

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

        [TestCase("fss.ukho.gov.uk", TestName = "Callback URL without http or https")]
        [TestCase("https:/fss.ukho.gov.uk", TestName = "Callback URL with wrong https parameter")]
        [TestCase("http:/fss.ukho.gov.uk", TestName = "Callback URL with wrong http parameter")]
        [TestCase("https://", TestName = "Callback URL with only https parameter")]
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
