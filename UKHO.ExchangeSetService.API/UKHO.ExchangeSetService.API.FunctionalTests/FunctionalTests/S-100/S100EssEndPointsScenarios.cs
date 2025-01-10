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
                DataHelper.GetProductVersionModelData("101GB40079ABCDEFG",10, 0),
                DataHelper.GetProductVersionModelData("102NO32904820801012", 2, 0),
                DataHelper.GetProductVersionModelData("104US00_CHES_TYPE1_20210630_0600", 1, 0),
                DataHelper.GetProductVersionModelData("111US00_ches_dcf8_20190703T00Z", 5, 2)
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
        //PBI 194579: Integrate S-100 ESS API Endpoint /productNames with corresponding SCS Stub
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100ProductNamesEndPointWithValidTokenAndOneOfTheInvalidProduct_ThenResponseCodeReturnedIs202Accepted()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(productNames, null, EssJwtToken, "s100");
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 202.");
            var expectedRequestedProductsNotInExchangeSet = new Dictionary<string, string> { { "102NO32904820801012", "invalidProduct" } };
            await apiResponse.VerifyEssS100ApiResponseBodyDetails(4, 4, 0, expectedRequestedProductsNotInExchangeSet);
        }

        //PBI 194403: Azure AD Authorization
        //PBI 194579: Integrate S-100 ESS API Endpoint /productNames with corresponding SCS Stub
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100ProductNamesEndPointWithValidTokenAndDuplicateProducts_ThenResponseCodeReturnedIs202Accepted()
        {
            var duplicateProductNames = new List<string>() { "101GB40079ABCDEFG", "101GB40079ABCDEFG" };
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(duplicateProductNames, null, EssJwtToken, "s100");
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 202.");
            var expectedRequestedProductsNotInExchangeSet = new Dictionary<string, string> { { "101GB40079ABCDEFG", "duplicateProduct" } };
            await apiResponse.VerifyEssS100ApiResponseBodyDetails(2, 1, 0, expectedRequestedProductsNotInExchangeSet);
        }

        //PBI 194403: Azure AD Authorization
        //PBI 194579: Integrate S-100 ESS API Endpoint /productNames with corresponding SCS Stub
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100ProductNamesEndPointWithValidTokenAndNonExistingProduct_ThenResponseCodeReturnedIs500InternalServerError()
        {
            var nonExistingProduct = new List<string>() { "103GB40079ABCDEFG" };
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(nonExistingProduct, null, EssJwtToken, "s100");
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
        //PBI 194580: Integrate S-100 ESS API Endpoint /productVersions with corresponding SCS Stub
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100ProductVersionsEndPointWithValidTokenAndOneOfTheInvalidProductVersions_ThenResponseCodeReturnedIs202Accepted()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, null, EssJwtToken, "s100");
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 202.");
            var expectedRequestedProductsNotInExchangeSet = new Dictionary<string, string> { { "104US00_CHES_TYPE1_20210630_0600", "invalidProduct" } };
            await apiResponse.VerifyEssS100ApiResponseBodyDetails(4, 3, 0, expectedRequestedProductsNotInExchangeSet);
        }

        //PBI 194403: Azure AD Authorization
        //PBI 194580: Integrate S-100 ESS API Endpoint /productVersions with corresponding SCS Stub
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100ProductVersionsEndPointWithValidTokenAndNonExistingProductVersions_ThenResponseCodeReturnedIs500InternalServererror()
        {
            List<ProductVersionModel> nonExistingProductVersionData = [ DataHelper.GetProductVersionModelData("103GB40079ABCDEFG", 10, 0) ];
            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(nonExistingProductVersionData, null, EssJwtToken, "s100");
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(500), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 500.");
        }

        //PBI 194403: Azure AD Authorization
        //PBI 194580: Integrate S-100 ESS API Endpoint /productVersions with corresponding SCS Stub
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100ProductVersionsEndPointWithValidTokenAndProductVersionsWhichIsNotModified_ThenResponseCodeReturnedIs202Accepted()
        {
            List<ProductVersionModel> nonModifiedProductVersionData = [DataHelper.GetProductVersionModelData("101GB40079ABCDEFG", 4, 1)];
            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(nonModifiedProductVersionData, null, EssJwtToken, "s100");
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 202.");
            await apiResponse.VerifyEssS100ApiResponseBodyDetails(1, 0, 1, null);
        }

        //PBI 194403: Azure AD Authorization
        //PBI 194580: Integrate S-100 ESS API Endpoint /productVersions with corresponding SCS Stub
        [Test]
        [Category("QCOnlyTest-AIOEnabled")]
        public async Task WhenICallS100ProductVersionsEndPointWithValidTokenAndProductVersionsHavingInvalidEdition_ThenResponseCodeReturnedIs400BadRequest()
        {
            List<ProductVersionModel> invalidEditionProductVersionData = [DataHelper.GetProductVersionModelData("102NO32904820801012", -2, 0)];
            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(invalidEditionProductVersionData, null, EssJwtToken, "s100");
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(400), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected 400.");
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
