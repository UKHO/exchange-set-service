using NUnit.Framework;
using System.Globalization;
using System;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests.S_100
{
    public class S100EssEndPointsScenarios : ObjectStorage
    {
        private UpdatesSinceModel sinceDateTimePayload;
        private List<string> productNames;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            ProductVersionData =
            [
                DataHelper.GetProductVersionModelData("101GB40079ABCDEFG", 7, 10),
                DataHelper.GetProductVersionModelData("102NO32904820801012", 36, 0),
                DataHelper.GetProductVersionModelData("104US00_CHES_TYPE1_20210630_0600", 7, 10),
                DataHelper.GetProductVersionModelData("111US00_ches_dcf8_20190703T00Z", 36, 0)
            ];
            productNames = DataHelper.GetProductNamesForS100();
        }

        [SetUp]
        public void SetUp()
        {
            sinceDateTimePayload = DataHelper.GetSinceDateTime(DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)); // Default value of -1
        }

        //PBI 194403: Azure AD Authorization
        //PBI 194581: Integrate S-100 ESS API Endpoint /updatesSince with corresponding SCS Stub
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100UpdatesSinceEndPointWithValidTokenAndValidDate_ThenResponseCodeReturnedIs202Accepted()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(null, null, EssJwtToken, "s100", sinceDateTimePayload);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 202.");
            await apiResponse.VerifyEssS100ApiResponseBodyDetails(0, 7, 0);
        }

        //PBI 194403: Azure AD Authorization
        //PBI 194581: Integrate S-100 ESS API Endpoint /updatesSince with corresponding SCS Stub
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100UpdatesSinceEndPointWithValidTokenAndValidDateWithProductIdentifierParam_ThenResponseCodeReturnedIs202Accepted()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(null, null, EssJwtToken, "s100", sinceDateTimePayload, "s102");
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 202.");
            await apiResponse.VerifyEssS100ApiResponseBodyDetails(0, 8, 0);
        }

        //PBI 194403: Azure AD Authorization
        //PBI 194581: Integrate S-100 ESS API Endpoint /updatesSince with corresponding SCS Stub
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100UpdatesSinceEndPointWithValidTokenForADateWhichHasNoUpdateWithProductIdentifierParam_ThenResponseCodeReturnedIs304Accepted()
        {
            sinceDateTimePayload = DataHelper.GetSinceDateTime(DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)); // Set a date that has no updates
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(null, null, EssJwtToken, "s100", sinceDateTimePayload, "s111");
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(304), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 304.");
            Assert.That(await apiResponse.Content.ReadAsStringAsync(), Is.Empty, "Response body is not empty.");
        }

        //PBI 194403: Azure AD Authorization
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100UpdatesSinceEndPointWithInvalidToken_ThenResponseCodeReturnedIs401Unauthorized()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(null, null, "InvalidEssJwtToken", "s100", sinceDateTimePayload);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(401), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 401.");
        }

        //PBI 194403: Azure AD Authorization
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100ProductNamesEndPointWithValidToken_ThenResponseCodeReturnedIs202Accepted()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(productNames, null, EssJwtToken, "s100");
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 202.");
        }

        //PBI 194403: Azure AD Authorization
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100ProductNamesEndPointWithInvalidToken_ThenResponseCodeReturnedIs401Unauthorized()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(productNames, null, "InvalidEssJwtToken", "s100");
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(401), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 401.");
        }

        //PBI 194403: Azure AD Authorization
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100ProductVersionsEndPointWithValidToken_ThenResponseCodeReturnedIs202Accepted()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, null, EssJwtToken, "s100");
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 202.");
        }

        //PBI 194403: Azure AD Authorization
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100ProductVersionsEndPointWithInvalidToken_ThenResponseCodeReturnedIs401unauthorized()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, null, "InvalidEssJwtToken", "s100");
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(401), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 401.");
        }
    }
}
