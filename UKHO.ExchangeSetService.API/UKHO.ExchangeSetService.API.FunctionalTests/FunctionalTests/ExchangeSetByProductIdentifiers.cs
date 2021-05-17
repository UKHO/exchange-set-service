using NUnit.Framework;
using System.Collections.Generic;
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
        public ProductIdentifierModel ProductIdentifiermodel { get; set; }

        [SetUp]
        public void Setup()
        {
            Config = new TestConfiguration();
            ExchangesetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductIdentifiermodel = new ProductIdentifierModel();
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductIdentifiers_ThenACorrectResponseIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "PE62263A", "FR373890", "CN584233" };

            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

        }

        [Test]
        public async Task WhenICallTheApiWithInvalidProductIdentifiers_ThenACorrectResponseIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "GB123789" };

            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();
            var apiResponsedata = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
         
            Assert.AreEqual("GB123789", apiResponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName, $"Exchange set returned Product Name {apiResponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName}, instead of expected Product Name 'GB123789'");
            Assert.AreEqual("invalidProduct", apiResponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, $"Exchange set returned Reason {apiResponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");

        }

        [Test]
        public async Task WhenICallTheApiWithDuplicateProductIdentifiers_ThenACorrectResponseIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "PE62263A", "PE62263A" };

            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureNotModifiedResponse();

        }

        [Test]
        public async Task WhenICallTheApiWithValidAndInvalidProductIdentifiers_ThenACorrectResponseIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "PE62263A", "GB123789" };

            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();
            var apiResponsedata = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            Assert.AreEqual("GB123789", apiResponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName, $"Exchange set returned Product Name {apiResponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().ProductName}, instead of expected Product Name 'GB123789'");
            Assert.AreEqual("invalidProduct", apiResponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().Reason, $"Exchange set returned Reason {apiResponsedata.RequestedProductsNotInExchangeSet.LastOrDefault().Reason}, instead of expected Reason 'invalidProduct'");
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductIdentifierswithCallBackURI_ThenACorrectResponseIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "GB123456", "GB160060", "AU334550" };

            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272%22");
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned  {apiResponse.StatusCode}, instead of of the expected status 200.");

        }

        [Test]
        public async Task WhenICallTheApiWithAnEmptyProductIdentifier_ThenABadRequestStatusIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>();

            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272%22");
            Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 400.");

            var errorMessage = await apiResponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "RequestBody"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Either body is null or malformed."));

        }

        [Test]
        public async Task WhenICallTheApiWithANullProductIdentifier_ThenABadRequestStatusIsReturned()
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { null};

            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272%22");
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
        public async Task WhenICallTheApiWithAInvalidCallbackURIWithProductIdentifier_ThenABadRequestResponseIsReturned(string callBackUrl)
        {
            ProductIdentifiermodel.ProductIdentifier = new List<string>() { "GB123456", "GB160060", "AU334550" };

            var apiResponse = await ExchangesetApiClient.GetProductIdentifiresDataAsync(ProductIdentifiermodel.ProductIdentifier, callBackUrl);
            Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 400.");

            var errorMessage = await apiResponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
           
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "CallbackUri"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Invalid Callback Uri format."));
        }
    }
}