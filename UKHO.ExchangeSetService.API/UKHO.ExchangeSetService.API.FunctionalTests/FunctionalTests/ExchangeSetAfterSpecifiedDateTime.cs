using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class ExchangeSetAfterSpecifiedDateTime
    {
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        private string EssJwtToken { get; set; }
        private string EssJwtTokenNoRole { get; set; }
        private string EssJwtCustomizedToken { get; set; }
        private FssApiClient FssApiClient { get; set; }
        private string FssJwtToken { get; set; }
        private readonly List<string> CleanUpBatchIdList = new List<string>();
        private readonly string SinceDateTime = DateTime.Now.AddDays(-5).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            FssApiClient = new FssApiClient();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            EssJwtTokenNoRole = await authTokenProvider.GetEssTokenNoAuth();
            EssJwtCustomizedToken = authTokenProvider.GenerateCustomToken();
            FssJwtToken = await authTokenProvider.GetFssToken();
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")][Category("SmokeTemp")]
        public async Task WhenICallTheApiWithOutAuthToken_ThenAnUnauthorisedResponseIsReturned()
        {

            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(SinceDateTime);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")][Category("SmokeTemp")]
        public async Task WhenICallTheApiWithTamperedToken_ThenAnUnauthorisedResponseIsReturned()
        {
            string tamperedEssJwtToken = EssJwtToken.Remove(EssJwtToken.Length - 2);
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(SinceDateTime, accessToken: tamperedEssJwtToken);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")][Category("SmokeTemp")]
        public async Task WhenICallTheApiWithCustomToken_ThenAnUnauthorisedResponseIsReturned()
        {

            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(SinceDateTime, accessToken: EssJwtCustomizedToken);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")][Category("Temp")]
        public async Task WhenICallTheApiWithNoRoleToken_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(SinceDateTime, accessToken: EssJwtTokenNoRole);

            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //Get the BatchId
            var batchId = await apiResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")][Category("Temp")]
        public async Task WhenICallTheApiWithAValidRFC1123DateTime_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(SinceDateTime, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            //Get the BatchId
            var batchId = await apiResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")][Category("Temp")]
        public async Task WhenICallTheApiWithAValidDateWithCallBackUri_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(SinceDateTime, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272", accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Exchange Set for datetime is returned {apiResponse.StatusCode}, instead of the expected 200.");

            //Get the BatchId
            var batchId = await apiResponse.GetBatchId();
            CleanUpBatchIdList.Add(batchId);

        }

        [TestCase(0, TestName = "Current DateTime with valid RFC1123 format")]
        [TestCase(1, TestName = "Future DateTime with valid RFC1123 format")]
        [Category("QCOnlyTest-AIODisabled")][Category("SmokeTemp")]
        public async Task WhenICallTheApiWithACurrentOrFutureRFC1123DateTime_ThenABadRequestStatusIsReturned(int days)
        {
            string sinceDatetime = DateTime.Now.AddDays(days).AddHours(1).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDatetime, accessToken: EssJwtToken);
            Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Exchange Set for datetime is returned {apiResponse.StatusCode}, instead of the expected 400. For given date {sinceDatetime}");

            var errorMessage = await apiResponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "sinceDateTime"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Provided sinceDateTime cannot be a future date."));
        }


        [TestCase("Mon, 02 Mar 2021 00:00:00 GMT", TestName = "Invalid day but valid RFC1123 Format")]
        [TestCase("01 Mar 2021 00:00:00 GMT", TestName = "Invalid RFC format 'DD MMM YYYY HH24:MI:SS GMT'")]
        [TestCase("01 03 2021", TestName = "Invalid RFC format 'DD MM YYYY'")]
        [Category("QCOnlyTest-AIODisabled")][Category("SmokeTemp")]
        public async Task WhenICallTheApiWithInValidRFC1123DateTime_ThenABadRequestStatusIsReturned(string sinceDatetime)
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDatetime, accessToken: EssJwtToken);
            Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Exchange Set for datetime is returned {apiResponse.StatusCode}, instead of the expected 400.");

            var errorMessage = await apiResponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "sinceDateTime"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Provided sinceDateTime is either invalid or invalid format, the valid format is 'RFC1123 format' (e.g. 'Wed, 21 Oct 2020 07:28:00 GMT')."));
        }


        [TestCase("", TestName = "Provided Empty DateTime in query parameter")]
        [TestCase(null, TestName = "Provided Null DateTime in query parameter")]
        [Category("QCOnlyTest-AIODisabled")][Category("SmokeTemp")]
        public async Task WhenICallTheApiWithANullDateTime_ThenABadRequestStatusIsReturned(string sinceDatetime)
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDatetime, accessToken: EssJwtToken);
            Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Exchange Set for datetime is returned {apiResponse.StatusCode}, instead of the expected 400.");

            var errorMessage = await apiResponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "sinceDateTime"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Query parameter 'sinceDateTime' is required."));
        }

        [TestCase("fss.ukho.gov.uk", TestName = "Callback URL without https")]
        [TestCase("https:/fss.ukho.gov.uk", TestName = "Callback URL with wrong https request")]
        [TestCase("ftp://fss.ukho.gov.uk", TestName = "Callback URL with ftp request")]
        [TestCase("http://fss.ukho.gov.uk", TestName = "Callback URL with http request")]
        [TestCase("https://", TestName = "Callback URL with only https request")]
        [Category("QCOnlyTest-AIODisabled")][Category("SmokeTemp")]
        public async Task WhenICallTheApiWithInvalidCallbackURI_ThenABadRequestResponseIsReturned(string callBackUrl)
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(SinceDateTime, callBackUrl, accessToken: EssJwtToken);
            Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Exchange Set for datetime is returned {apiResponse.StatusCode}, instead of the expected 400.");

            var errorMessage = await apiResponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "callbackUri"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Invalid callbackUri format."));
        }

        [OneTimeTearDown]
        public async Task GlobalTeardown()
        {
            if (CleanUpBatchIdList != null && CleanUpBatchIdList.Count > 0)
            {
                //Clean up batches from local foldar 
                var apiResponse = await FssApiClient.CleanUpBatchesAsync(Config.FssConfig.BaseUrl, CleanUpBatchIdList, FssJwtToken);
                Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned for clean up batches, instead of the expected 200.");
            }
        }
    }
}