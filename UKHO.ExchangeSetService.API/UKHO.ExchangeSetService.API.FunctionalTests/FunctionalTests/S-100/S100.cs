using NUnit.Framework;
using System.Globalization;
using System;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests.S_100
{
    public class ExchangeSetServiceS100 : ObjectStorage
    {
        private readonly string SinceDateTime = DateTime.UtcNow.AddDays(-12).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);


        [Test]
        public async Task WhenICallS100UpdatesSinceEndPointWithValidToken_ThenResponseCodeReturnedIs202Accepted()
        {
            UpdatesSinceModel SinceDateTimePayload = DataHelper.GetSinceDateTime(SinceDateTime);
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(SinceDateTime, null, EssJwtToken, "s100", SinceDateTimePayload);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 202.");
        }

        [Test]
        public async Task WhenICallS100UpdatesSinceEndPointWithInValidToken_ThenResponseCodeReturnedIs401Unauthorized()
        {
            var InvalidEssJwtToken = "InvalidToken";
            UpdatesSinceModel SinceDateTimePayload = DataHelper.GetSinceDateTime(SinceDateTime);
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(SinceDateTime, null, InvalidEssJwtToken, "s100", SinceDateTimePayload);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(401), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 401.");
        }
    }

}
