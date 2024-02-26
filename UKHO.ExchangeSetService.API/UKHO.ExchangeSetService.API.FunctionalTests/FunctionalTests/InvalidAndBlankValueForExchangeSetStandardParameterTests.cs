using NUnit.Framework;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using System.Collections.Generic;
using System;
using System.Globalization;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    [TestFixture]
    public class InvalidAndBlankValueForExchangeSetStandardParameterTests : ObjectStorage
    {
        private readonly string sinceDateTime = DateTime.Now.AddDays(-5).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);

        [SetUp]
        public void SetupAsync()
        {
            DataHelper = new DataHelper();
        }

        #region Set DateTime Api
        //PBI 143370: Change related to additional param (From Boolean to String)
        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallSinceDateTimeApiWithInvalidValueExchangeSetStandardParameter_ThenABadRequestIsReturned()
        {
            foreach (var exchangeSetStandard in Config.BESSConfig.InvalidExchangeSetTestData)
            {
                var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDateTime, null, EssJwtToken, exchangeSetStandard);
                Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 400.");
            }
        }
        #endregion

        #region ProductIdentifiers Api
        //PBI 143370: Change related to additional param (From Boolean to String)
        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallProductIdentifiersApiWithInvalidValueExchangeSetStandardParameter_ThenABadRequestIsReturned()
        {
            foreach (var exchangeSetStandard in Config.BESSConfig.InvalidExchangeSetTestData)
            {
                var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifierData(), null, EssJwtToken, exchangeSetStandard);
                Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 400.");
            }
        }

        //PBI 143370: Change related to additional param (From Boolean to String)
        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallProductIdentifiersApiWithInvalidkeyExchangeSetStandardParameter_ThenABadRequestIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataWithIncorrectOptionalParameterAsync(DataHelper.GetProductIdentifierData(), null, EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");
        }
        #endregion

        #region ProductVersion Api
        //PBI 143370: Change related to additional param (From Boolean to String)
        [Test]
        [Category("SmokeTest-AIODisabled")]
        public async Task WhenICallProductVersionsApiWithInvalidValueExchangeSetStandardParameter_ThenABadRequestIsReturned()
        {
            foreach (var exchangeSetStandard in Config.BESSConfig.InvalidExchangeSetTestData)
            {
                List<ProductVersionModel> productVersionData = new()
                {
                DataHelper.GetProductVersionModelData("DE4NO18Q", 1, 0)
                };

                var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(productVersionData, null, EssJwtToken, exchangeSetStandard);
                Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 400.");
            }
        }
        #endregion
    }
}