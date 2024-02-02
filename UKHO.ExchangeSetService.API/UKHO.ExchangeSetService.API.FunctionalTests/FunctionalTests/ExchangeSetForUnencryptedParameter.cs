using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    public class ExchangeSetForUnencryptedParameter : ObjectStorage
    {
        private readonly string SinceDateTime = DateTime.Now.AddDays(-5).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);

        [SetUp]
        public void SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            FssApiClient = new FssApiClient();
            Datahelper = new DataHelper();
        }

        //PBI:139801 : ESS API : Create and add optional parameter IsUnencrypted, add validation and Update Swagger Doc
        [Test]
        [Category("SmokeTest-AIODisabled")]
        [TestCase("abc123")]
        [TestCase("test")]
        [TestCase("1")]
        [TestCase("0")]
        public async Task WhenICallProductIdentifiersApiWithInCorrectValueOfIsUnencryptedParameter_ThenACorrectResponseIsReturned(string isUnencrypted)
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(Datahelper.GetProductIdentifierData(), "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272%22", accessToken: EssJwtToken, isUnencrypted);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned  {apiResponse.StatusCode}, instead of of the expected status 200.");
        }

        //PBI:139801 : ESS API : Create and add optional parameter IsUnencrypted, add validation and Update Swagger Doc
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        [TestCase("")]
        [TestCase("test")]
        [TestCase("1")]
        [TestCase("0")]
        public async Task WhenICallProductVersionsApiWithInCorrectValueOfIsUnencryptedParameter_ThenACorrectResponseIsReturned(string isUnencrypted)
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();
            ProductVersiondata.Add(DataHelper.GetProductVersionModelData("DE4NO18Q", 2, 0));
            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, null, accessToken: EssJwtToken, isUnencrypted);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        //PBI:139801 : ESS API : Create and add optional parameter IsUnencrypted, add validation and Update Swagger Doc
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        [TestCase("")]
        [TestCase("test")]
        [TestCase("1")]
        [TestCase("0")]
        public async Task WhenICallSinceDateTimeApiWithInCorrectValueOfIsUnencryptedParameter_ThenACorrectResponseIsReturned(string isUnencrypted)
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(SinceDateTime, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272", accessToken: EssJwtToken, isUnencrypted);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Exchange Set for datetime is returned {apiResponse.StatusCode}, instead of the expected 200.");
        }
    }
}
