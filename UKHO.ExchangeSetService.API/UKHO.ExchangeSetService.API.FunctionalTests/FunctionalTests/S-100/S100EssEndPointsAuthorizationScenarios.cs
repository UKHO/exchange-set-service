using NUnit.Framework;
using System.Globalization;
using System;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests.S_100
{
    public class S100EssEndPointsAuthorizationScenarios : ObjectStorage
    {
        private string sinceDateTime;
        private UpdatesSinceModel sinceDateTimePayload;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            sinceDateTime = DateTime.UtcNow.AddDays(-12).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            sinceDateTimePayload = DataHelper.GetSinceDateTime(sinceDateTime);
        }

        //PBI 194403: Azure AD Authorization
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100UpdatesSinceEndPointWithValidToken_ThenResponseCodeReturnedIs202Accepted()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDateTime, null, EssJwtToken, "s100", sinceDateTimePayload);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 202.");
        }

        //PBI 194403: Azure AD Authorization
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100UpdatesSinceEndPointWithInValidToken_ThenResponseCodeReturnedIs401Unauthorized()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDateTime, null, "InvalidEssJwtToken", "s100", sinceDateTimePayload);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(401), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 401.");
        }
    }
}
