using NUnit.Framework;
using System.Globalization;
using System;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests.S_100
{
    public class S100EssEndPointsAuthorizationScenarios : ObjectStorage
    {
        private string sinceDateTime;
        private UpdatesSinceModel sinceDateTimePayload;
        private List<string> productNames;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            sinceDateTime = DateTime.UtcNow.AddDays(-12).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            sinceDateTimePayload = DataHelper.GetSinceDateTime(sinceDateTime);
            ProductVersionData =
            [
                DataHelper.GetProductVersionModelData("101GB40079ABCDEFG", 7, 10),
                DataHelper.GetProductVersionModelData("102NO32904820801012", 36, 0),
                DataHelper.GetProductVersionModelData("104US00_CHES_TYPE1_20210630_0600", 7, 10),
                DataHelper.GetProductVersionModelData("111US00_ches_dcf8_20190703T00Z", 36, 0)
            ];
            productNames = DataHelper.GetProductNamesForS100();
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
        public async Task WhenICallS100UpdatesSinceEndPointWithInvalidToken_ThenResponseCodeReturnedIs401Unauthorized()
        {
            var apiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDateTime, null, "InvalidEssJwtToken", "s100", sinceDateTimePayload);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(401), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 401.");
        }

        //PBI 194403: Azure AD Authorization
        //PBI 194579: Integrate S-100 ESS producNames EP with stub
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100ProductNamesEndPointWithValidTokenAndOneOfTheProductWithdrawn_ThenResponseCodeReturnedIs202Accepted()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(productNames, null, EssJwtToken, "s100");
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 202.");
            var ExpectedRequestedProductsNotInExchangeSet = new Dictionary<string, string> { { "102NO32904820801012", "productWithdrawn" } };
            await apiResponse.VerifyEssS100ApiResponseBodyDetails(4, 4, 0, ExpectedRequestedProductsNotInExchangeSet);
        }

        //PBI 194403: Azure AD Authorization
        //PBI 194579: Integrate S-100 ESS producNames EP with stub
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100ProductNamesEndPointWithValidTokenAndDuplicateProducts_ThenResponseCodeReturnedIs202Accepted()
        {
            var duplicateProductNames = new List<string>() { "101GB40079ABCDEFG", "101GB40079ABCDEFG" };
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(duplicateProductNames, null, EssJwtToken, "s100");
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 202.");
            var ExpectedRequestedProductsNotInExchangeSet = new Dictionary<string, string> { { "101GB40079ABCDEFG", "duplicateProduct" } };
            await apiResponse.VerifyEssS100ApiResponseBodyDetails(2, 1, 0, ExpectedRequestedProductsNotInExchangeSet);
        }

        //PBI 194403: Azure AD Authorization
        //PBI 194579: Integrate S-100 ESS producNames EP with stub
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100ProductNamesEndPointWithValidTokenAndNonExisitingProduct_ThenResponseCodeReturnedIs500InternalServerError()
        {
            var nonExisitingProductNames = new List<string>() { "103GB40079ABCDEFG" };
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(nonExisitingProductNames, null, EssJwtToken, "s100");
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(500), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 500.");
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
