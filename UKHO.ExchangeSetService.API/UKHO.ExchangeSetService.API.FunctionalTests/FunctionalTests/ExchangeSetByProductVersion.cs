using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class ExchangeSetByProductVersion
    {
        private ExchangeSetApiClient ExchangesetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper Datahelper { get; set; }
        public ProductVersionModel ProductVersionmodel { get; set; }
        private string EssJwtToken { get; set; }
        private string EssJwtTokenNoRole { get; set; }
        private string EssJwtCustomizedToken { get; set; }

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangesetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductVersionmodel = new ProductVersionModel();
            Datahelper = new DataHelper();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            EssJwtTokenNoRole = await authTokenProvider.GetEssTokenNoAuth();
            EssJwtCustomizedToken = authTokenProvider.GenerateCustomToken();

        }

        [Test]
        public async Task WhenICallTheApiWithOutAuthToken_ThenAnUnauthorisedResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersionmodel.ProductName = "GB50184F";
            ProductVersionmodel.EditionNumber = 5;
            ProductVersionmodel.UpdateNumber = 1;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));
                       
            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        public async Task WhenICallTheApiWithTamperedToken_ThenAnUnauthorisedResponseIsReturned()
        {
            string tamperedEssJwtToken = EssJwtToken.Remove(EssJwtToken.Length - 2);

            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersionmodel.ProductName = "GB50184F";
            ProductVersionmodel.EditionNumber = 5;
            ProductVersionmodel.UpdateNumber = 1;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: tamperedEssJwtToken);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }


        [Test]
        public async Task WhenICallTheApiWithCustomToken_ThenAnUnauthorisedResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersionmodel.ProductName = "GB50184F";
            ProductVersionmodel.EditionNumber = 5;
            ProductVersionmodel.UpdateNumber = 1;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtCustomizedToken);
           
            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        public async Task WhenICallTheApiWithNoRoleToken_ThenAForbiddenResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersionmodel.ProductName = "GB50184F";
            ProductVersionmodel.EditionNumber = 5;
            ProductVersionmodel.UpdateNumber = 1;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtTokenNoRole);

            Assert.AreEqual(403, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 403.");
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductVersion_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersionmodel.ProductName = "GR3CFZMG";
            ProductVersionmodel.EditionNumber = 2;
            ProductVersionmodel.UpdateNumber = 30;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            ProductVersionmodel.ProductName = "GB50184F";
            ProductVersionmodel.EditionNumber = 5;
            ProductVersionmodel.UpdateNumber = 1;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

        }

        [Test]
        public async Task WhenICallTheApiWithValidAndInvalidProductVersion_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersionmodel.ProductName = "GR3CFZMG";
            ProductVersionmodel.EditionNumber = 2;
            ProductVersionmodel.UpdateNumber = 30;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            ProductVersionmodel.ProductName = "GR3FZMG1";
            ProductVersionmodel.EditionNumber = 2;
            ProductVersionmodel.UpdateNumber = 30;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            var apiResponsedata = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            Assert.AreEqual("GR3FZMG1", apiResponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName, $"Exchange set returned Product Name {apiResponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName}, instead of expected Product Name 'GR3FZMG1'");
            Assert.AreEqual("invalidProduct", apiResponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, $"Exchange set returned Reason {apiResponsedata.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");
        }

        [Test]
        public async Task WhenICallTheApiWithValidDuplicateProductVersion_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersionmodel.ProductName = "GR3CFZMG";
            ProductVersionmodel.EditionNumber = 2;
            ProductVersionmodel.UpdateNumber = 30;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            ProductVersionmodel.ProductName = "GR3CFZMG";
            ProductVersionmodel.EditionNumber = 2;
            ProductVersionmodel.UpdateNumber = 30;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureNotModifiedResponse();

        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductVersionWithCallbackURI_ThenASuccessStatusIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersionmodel.ProductName = "GR3CFZMG";
            ProductVersionmodel.EditionNumber = 2;
            ProductVersionmodel.UpdateNumber = 20;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            ProductVersionmodel.ProductName = "GB50184F";
            ProductVersionmodel.EditionNumber = 5;
            ProductVersionmodel.UpdateNumber = 1;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272", accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

        }

        [TestCase("GB50184F", 6, 1, TestName = "EditionNumber Unavailable")]
        [TestCase("GB50184F", 5, 2, TestName = "UpdateNumber Unavailable")]
        public async Task WhenICallTheApiWithAProductVersionNotAvailable_ThenTheCorrectResponseIsReturned(string productname, int editionnumber, int updatenumber)
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersionmodel.ProductName = productname;
            ProductVersionmodel.EditionNumber = editionnumber;
            ProductVersionmodel.UpdateNumber = updatenumber;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272", accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");
        }

        [Test]
        public async Task WhenICallTheApiWithAnEmptyProductVersion_ThenABadRequestStatusIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();
        
            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 400.");

            var errorMessage = await apiResponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "RequestBody"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Either body is null or malformed."));
        }

        [TestCase(null, 4, 1, "ProductVersions[0].ProductName", "ProductName cannot be blank or null.", TestName = "When product name is null.")]
        [TestCase("", 4, 1, "ProductVersions[0].ProductName", "ProductName cannot be blank or null.", TestName = "When product name is blank.")]
        [TestCase("AU895561", null, 1, "ProductVersions[0].EditionNumber", "EditionNumber cannot be less than zero or null.", TestName = "When edition number is null.")]
        [TestCase("AU895561", 4, null, "ProductVersions[0].UpdateNumber", "UpdateNumber cannot be less than zero or null.", TestName = "When update number is null.")]
        [TestCase("AU895561", -1, 1, "ProductVersions[0].EditionNumber", "EditionNumber cannot be less than zero or null.", TestName = "When edition number is less than zero.")]
        [TestCase("AU895561", 4, -1, "ProductVersions[0].UpdateNumber", "UpdateNumber cannot be less than zero or null.", TestName = "When update number is less than zero.")]
        public async Task WhenICallTheApiWithNullProductVersion_ThenABadRequestStatusIsReturned(string productName, int? editionNumber, int? updateNumber, string sourceMessage, string descriptionMessage)
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(productName, editionNumber, updateNumber));

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272", accessToken: EssJwtToken);
            Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 400.");

            var errorMessage = await apiResponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == sourceMessage));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == descriptionMessage));
        }

        [TestCase("fss.ukho.gov.uk", TestName = "Callback URL without https")]
        [TestCase("https:/fss.ukho.gov.uk", TestName = "Callback URL with wrong https request")]
        [TestCase("ftp://fss.ukho.gov.uk", TestName = "Callback URL with ftp request")]
        [TestCase("https://", TestName = "Callback URL with only https request")]
        [TestCase("http://fss.ukho.gov.uk", TestName = "Callback URL with http request")]
        public async Task WhenICallTheApiWithAValidProductVersionWithInvalidCallbackURI_ThenABadRequestStatusIsReturned(string callBackUrl)
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersionmodel.ProductName = "GR3CFZMG";
            ProductVersionmodel.EditionNumber = 2;
            ProductVersionmodel.UpdateNumber = 30;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            ProductVersionmodel.ProductName = "GB50184F";
            ProductVersionmodel.EditionNumber = 5;
            ProductVersionmodel.UpdateNumber = 1;

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, callBackUrl, accessToken: EssJwtToken);
            Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 400.");

        }
    }
}