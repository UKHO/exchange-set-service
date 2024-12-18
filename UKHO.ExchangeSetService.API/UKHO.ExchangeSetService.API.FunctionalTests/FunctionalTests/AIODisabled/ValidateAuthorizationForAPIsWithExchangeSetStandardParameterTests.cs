using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests.AIODisabled
{
    public class ValidateAuthorizationForAPIsWithExchangeSetStandardParameterTests : ObjectStorage
    {
        private readonly string sinceDateTime = DateTime.Now.AddDays(-5).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);

        [SetUp]
        public void SetupAsync()
        {
            DataHelper = new DataHelper();
        }

        #region Set DateTime Api
        // PBI 140109 : ESS API : Add authorization to allow only UKHO people to create unencrypted ES 
        //PBI 142740: ESS APIs : UKHO Authorization - Change Unauthorize(401) to Forbidden(403)
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallSinceDateTimeApiWithAValidAdb2cTokenAndEmailOutsideAllowedDomainAndS57AsExchangeSetStandardParameter_ThenAForbiddenResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDateTime, null, accessToken: EssB2CToken, "s57");
            Assert.AreEqual(403, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 403.");
        }

        // PBI 140109 : ESS API : Add authorization to allow only UKHO people to create unencrypted ES 
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallSinceDateTimeApiWithAValidADTokenAndS57AsExchangeSetStandardParameter_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDateTime, null, accessToken: EssJwtToken, "s57");
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();
        }
        #endregion

        #region ProductIdentifiers Api
        // PBI 140109 : ESS API : Add authorization to allow only UKHO people to create unencrypted ES 
        //PBI 142740: ESS APIs : UKHO Authorization - Change Unauthorize(401) to Forbidden(403)
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallProductIdentifiersApiWithAValidB2cTokenAnds57AsExchangeSetStandardParameter_ThenAForbiddenResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifierData(), null, accessToken: EssB2CToken, "s57");
            Assert.AreEqual(403, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 403.");
        }

        // PBI 140109 : ESS API : Add authorization to allow only UKHO people to create unencrypted ES 
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallProductIdentifiersApiWithAValidADTokenAnds57AsExchangeSetStandardParameter_ThenACorrectResponseIsReturned()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifierData(), null, accessToken: EssJwtToken, "s57");
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();
        }
        #endregion

        #region ProductVersions Api
        // PBI 140109 : ESS API : Add authorization to allow only UKHO people to create unencrypted ES 
        //PBI 142740: ESS APIs : UKHO Authorization - Change Unauthorize(401) to Forbidden(403)
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallProductVersionsApiWithAValidAdb2cTokenAndEmailOutsideAllowedDomainAndS57AsExchangeSetStandardParameter_ThenAForbiddenResponseIsReturned()
        {
            List<ProductVersionModel> productVersionData = new()
            {
                DataHelper.GetProductVersionModelData("DE4NO18Q", 1, 0)
            };

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(productVersionData, null, accessToken: EssB2CToken, "s57");
            Assert.AreEqual(403, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 403.");
        }

        // PBI 140109 : ESS API : Add authorization to allow only UKHO people to create unencrypted ES 
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallProductVersionsApiWithAValidADTokenAndS57AsExchangeSetStandardParameter_ThenACorrectResponseIsReturned()
        {
            List<ProductVersionModel> productVersionData = new()
            {
                DataHelper.GetProductVersionModelData("DE416080", 9, 1)
            };

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(productVersionData, null, accessToken: EssJwtToken, "s57");
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();
        }
        #endregion
    }
}
