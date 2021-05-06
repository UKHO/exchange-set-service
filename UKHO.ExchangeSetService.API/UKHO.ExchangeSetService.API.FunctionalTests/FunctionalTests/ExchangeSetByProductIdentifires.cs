using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;



namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class ExchangeSetByProductIdentifires
    {
        private ExchangeSetApiClient ExchangesetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public ProductIdentifierModel ProductIdentifiermodel { get; set; }

        [SetUp]
        public void Setup()
        {
            Config = new TestConfiguration();
            ExchangesetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductIdentifiermodel = new ProductIdentifierModel();
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductIdentifiers_ThenASuccessStatusIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "GB123456", "GB160060", "AU334550" };

            var apiresponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier);
            Assert.AreEqual(200, (int)apiresponse.StatusCode, $"Exchange Set for Product version is  returned {apiresponse.StatusCode}, instead of the expected status 200.");

            var apiresponsedata = await apiresponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            Assert.AreEqual("http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272", apiresponsedata.Links.ExchangeSetBatchStatusUri.Href, $"Exchange set returned batch status URI {apiresponsedata.Links.ExchangeSetBatchStatusUri.Href}, instead of expected batch status URI 'http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272'");
            Assert.AreEqual("http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip", apiresponsedata.Links.ExchangeSetFileUri.Href, $"Exchange set returned file URI {apiresponsedata.Links.ExchangeSetFileUri.Href}, instead of expected file URI 'http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip'");
            Assert.AreEqual("2021-02-17T16:19:32.269Z", apiresponsedata.ExchangeSetUrlExpiryDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffK", CultureInfo.InvariantCulture), $"Exchange set returned URL expiry date time {apiresponsedata.ExchangeSetUrlExpiryDateTime}, instead of expected URL expiry date time '2021 - 02 - 17T16: 19:32.269Z'");
            Assert.AreEqual(22, apiresponsedata.RequestedProductCount, $"Exchange set returned Requested Product Count {apiresponsedata.RequestedProductCount}, instead of expected Requested Product Count '22'");
            Assert.AreEqual(15, apiresponsedata.ExchangeSetCellCount, $"Exchange set returned Exchange Set Cell Count {apiresponsedata.ExchangeSetCellCount}, instead of expected Exchange Set Cell Count '15'");
            Assert.AreEqual(5, apiresponsedata.RequestedProductsAlreadyUpToDateCount, $"Exchange set returned Requested Products Already UpDate Count {apiresponsedata.RequestedProductsAlreadyUpToDateCount}, instead of expected Products Already UpDate Count '5'");
            Assert.AreEqual("GB123456", apiresponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName, $"Exchange set returned Product Name {apiresponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName}, instead of expected Product Name 'GB123456'");
            Assert.AreEqual("productWithdrawn", apiresponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, $"Exchange set returned Reason {apiresponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'productWithdrawn'");
            Assert.AreEqual("GB123789", apiresponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, $"Exchange set returned Product Name {apiresponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'GB123789'");
            Assert.AreEqual("invalidProduct", apiresponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().Reason, $"Exchange set returned Reason {apiresponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductIdentifierswithCallBackURI_ThenASuccessStatusIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "GB123456", "GB160060", "AU334550" };

            var apiresponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, "http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272%22");
            Assert.AreEqual(200, (int)apiresponse.StatusCode, $"Exchange Set for Product identifier is  returned {apiresponse.StatusCode}, instead of of the expected status 200.");

        }

        [Test]
        public async Task WhenICallTheApiWithAnEmptyProductIdentifier_ThenABadRequestStatusIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>();

            var apiresponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, "http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272%22");
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Exchange Set for Product identifier is  returned {apiresponse.StatusCode}, instead of the expected status 400.");

            var errorMessage = await apiresponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
             Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "RequestBody"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Either body is null or malformed."));

        }

        [Test]
        public async Task WhenICallTheApiWithANullProductIdentifier_ThenABadRequestStatusIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { null};

            var apiresponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, "http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272%22");
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Exchange Set for Product identifier is  returned {apiresponse.StatusCode}, instead of the expected status 400.");

            var errorMessage = await apiresponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "ProductIdentifier"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Product Identifiers cannot be null or empty."));

        }

        [TestCase("fss.ukho.gov.uk", TestName = "Callback URL without http or https")]
        [TestCase("https:/fss.ukho.gov.uk", TestName = "Callback URL with wrong https parameter")]
        [TestCase("http:/fss.ukho.gov.uk", TestName = "Callback URL with wrong http parameter")]
        [TestCase("https://", TestName = "Callback URL with only https parameter")]
        public async Task WhenICallTheApiWithAInvalidCallbackURIWithProductIdentifier_ThenABadRequestStatusIsReturned(string callbackurl)
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "GB123456", "GB160060", "AU334550" };

            var apiresponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, callbackurl);
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Exchange Set for Product identifier is  returned {apiresponse.StatusCode}, instead of the expected status 400.");

            var errorMessage = await apiresponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "CallbackUri"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Invalid Callback Uri format."));
        }
    }
}