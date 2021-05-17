using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class ExchangeSetByProductIdentifiers
    {
        private ExchangeSetApiClient ExchangesetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        private string EssJwtToken { get; set; }
        private string EssJwtTokenNoRole { get; set; }
        private string EssJwtCustomizedToken { get; set; }
        public ProductIdentifierModel ProductIdentifiermodel { get; set; }

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangesetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductIdentifiermodel = new ProductIdentifierModel();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            EssJwtTokenNoRole = await authTokenProvider.GetEssTokenNoAuth();
            EssJwtCustomizedToken = authTokenProvider.GenerateCustomToken();
        }

        [Test]
        public async Task WhenICallTheApiWithOutAuthToken_ThenAnUnauthorisedResponseIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "GB123456", "GB160060", "AU334550" };
            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier);
            
            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        public async Task WhenICallTheApiWithTamperedToken_ThenAnUnauthorisedResponseIsReturned()
        {
            string tamperedEssJwtToken = EssJwtToken.Remove(EssJwtToken.Length - 2);
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "GB123456", "GB160060", "AU334550" };
            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, accessToken: tamperedEssJwtToken);
                      
            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        public async Task WhenICallTheApiWithCustomToken_ThenAnUnauthorisedResponseIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "GB123456", "GB160060", "AU334550" };
            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, accessToken: EssJwtCustomizedToken);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        public async Task WhenICallTheApiWithNoRoleToken_ThenAForbiddenResponseIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "GB123456", "GB160060", "AU334550" };
            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, accessToken: EssJwtTokenNoRole);

            Assert.AreEqual(403, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 403.");
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductIdentifiers_ThenACorrectResponseIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "GB123456", "GB160060", "AU334550" };

            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var apiResponsedata = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            Assert.AreEqual("http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272", apiResponsedata.Links.ExchangeSetBatchStatusUri.Href, $"Exchange set returned batch status URI {apiResponsedata.Links.ExchangeSetBatchStatusUri.Href}, instead of expected batch status URI 'http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272'");
            Assert.AreEqual("http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip", apiResponsedata.Links.ExchangeSetFileUri.Href, $"Exchange set returned file URI {apiResponsedata.Links.ExchangeSetFileUri.Href}, instead of expected file URI 'http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip'");
            Assert.AreEqual("2021-02-17T16:19:32.269Z", apiResponsedata.ExchangeSetUrlExpiryDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffK", CultureInfo.InvariantCulture), $"Exchange set returned URL expiry date time {apiResponsedata.ExchangeSetUrlExpiryDateTime}, instead of expected URL expiry date time '2021 - 02 - 17T16: 19:32.269Z'");
            Assert.AreEqual(22, apiResponsedata.RequestedProductCount, $"Exchange set returned Requested Product Count {apiResponsedata.RequestedProductCount}, instead of expected Requested Product Count '22'");
            Assert.AreEqual(15, apiResponsedata.ExchangeSetCellCount, $"Exchange set returned Exchange Set Cell Count {apiResponsedata.ExchangeSetCellCount}, instead of expected Exchange Set Cell Count '15'");
            Assert.AreEqual(5, apiResponsedata.RequestedProductsAlreadyUpToDateCount, $"Exchange set returned Requested Products Already UpDate Count {apiResponsedata.RequestedProductsAlreadyUpToDateCount}, instead of expected Products Already UpDate Count '5'");
            Assert.AreEqual("GB123456", apiResponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName, $"Exchange set returned Product Name {apiResponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName}, instead of expected Product Name 'GB123456'");
            Assert.AreEqual("productWithdrawn", apiResponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, $"Exchange set returned Reason {apiResponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'productWithdrawn'");
            Assert.AreEqual("GB123789", apiResponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, $"Exchange set returned Product Name {apiResponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'GB123789'");
            Assert.AreEqual("invalidProduct", apiResponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().Reason, $"Exchange set returned Reason {apiResponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().Reason}, instead of expected Reason 'invalidProduct'");
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductIdentifierswithCallBackURI_ThenACorrectResponseIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "GB123456", "GB160060", "AU334550" };

            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272%22", accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned  {apiResponse.StatusCode}, instead of of the expected status 200.");
        }

        [Test]
        public async Task WhenICallTheApiWithAnEmptyProductIdentifier_ThenABadRequestStatusIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>();

            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272%22", accessToken: EssJwtToken);
            Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 400.");

            var errorMessage = await apiResponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
             Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "RequestBody"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Either body is null or malformed."));
        }

        [Test]
        public async Task WhenICallTheApiWithANullProductIdentifier_ThenABadRequestStatusIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { null};

            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272%22", accessToken: EssJwtToken);
            Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 400.");

            var errorMessage = await apiResponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "ProductIdentifier"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Product Identifiers cannot be null or empty."));
        }

        [TestCase("fss.ukho.gov.uk", TestName = "Callback URL without https")]
        [TestCase("https:/fss.ukho.gov.uk", TestName = "Callback URL with wrong https request")]
        [TestCase("ftp://fss.ukho.gov.uk", TestName = "Callback URL with ftp request")]
        [TestCase("https://", TestName = "Callback URL with only https request")]
        [TestCase("http://fss.ukho.gov.uk", TestName = "Callback URL with http request")]
        public async Task WhenICallTheApiWithAInvalidCallbackURIWithProductIdentifier_ThenABadRequestResponseIsReturned(string callbackurl)
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "GB123456", "GB160060", "AU334550" };

            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, callbackurl, accessToken: EssJwtToken);
            Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 400.");

            var errorMessage = await apiResponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "CallbackUri"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Invalid Callback Uri format."));
        }
    }
}