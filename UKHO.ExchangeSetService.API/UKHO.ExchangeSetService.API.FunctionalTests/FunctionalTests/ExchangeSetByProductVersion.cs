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

        [SetUp]
        public void Setup()
        {
            Config = new TestConfiguration();
            ExchangesetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductVersionmodel = new ProductVersionModel();
            Datahelper = new DataHelper();

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

            var apiresponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata);
            Assert.AreEqual(200, (int)apiresponse.StatusCode, $"Incorrect status code {apiresponse.StatusCode}  is  returned, instead of the expected 200.");

            //verify model structure
            await apiresponse.CheckModelStructureForSuccessResponse();

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

            var apiresponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272");
            Assert.AreEqual(200, (int)apiresponse.StatusCode, $"Incorrect status code {apiresponse.StatusCode}  is  returned, instead of the expected 200.");

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

            var apiresponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata);
            Assert.AreEqual(200, (int)apiresponse.StatusCode, $"Incorrect status code {apiresponse.StatusCode}  is  returned, instead of the expected 200.");

            //verify model structure
            await apiresponse.CheckModelStructureNotModifiedResponse();

        }

        [Test]
        public async Task WhenICallTheApiWithAnEmptyProductVersion_ThenABadRequestStatusIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            var apiresponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata);
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Incorrect status code {apiresponse.StatusCode}  is  returned, instead of the expected 400.");

            var errorMessage = await apiresponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == "RequestBody"));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == "Either body is null or malformed."));
        }

        [TestCase(null, 4, 1, "ProductVersions[0].ProductName", "ProductName cannot be blank or null.", TestName = "When product name is null.")]
        [TestCase("", 4, 1, "ProductVersions[0].ProductName", "ProductName cannot be blank or null.", TestName = "When product name is blank.")]
        [TestCase("GB50184F", null, 1, "ProductVersions[0].EditionNumber", "EditionNumber cannot be less than zero or null.", TestName = "When edition number is null.")]
        [TestCase("GB50184F", 4, null, "ProductVersions[0].UpdateNumber", "UpdateNumber cannot be less than zero or null.", TestName = "When update number is null.")]
        [TestCase("GB50184F", -1, 1, "ProductVersions[0].EditionNumber", "EditionNumber cannot be less than zero or null.", TestName = "When edition number is less than zero.")]
        [TestCase("GB50184F", 4, -1, "ProductVersions[0].UpdateNumber", "UpdateNumber cannot be less than zero or null.", TestName = "When update number is less than zero.")]
        public async Task WhenICallTheApiWithNullProductVersion_ThenABadRequestStatusIsReturned(string productname, int? editionnumber, int? updatenumber, string sourcemessage, string descriptionmessage)
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(Datahelper.GetProductVersionModelData(productname, editionnumber, updatenumber));

            var apiresponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272");
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Incorrect status code {apiresponse.StatusCode}  is  returned, instead of the expected 400.");

            var errorMessage = await apiresponse.ReadAsTypeAsync<ErrorDescriptionResponseModel>();
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Source == sourcemessage));
            Assert.IsTrue(errorMessage.Errors.Any(e => e.Description == descriptionmessage));
        }

        [TestCase("fss.ukho.gov.uk", TestName = "Callback URL without https")]
        [TestCase("https:/fss.ukho.gov.uk", TestName = "Callback URL with wrong https request")]
        [TestCase("ftp://fss.ukho.gov.uk", TestName = "Callback URL with ftp request")]
        [TestCase("https://", TestName = "Callback URL with only https request")]
        [TestCase("http://fss.ukho.gov.uk", TestName = "Callback URL with http request")]
        public async Task WhenICallTheApiWithAValidProductVersionWithInvalidCallbackURI_ThenABadRequestStatusIsReturned(string callbackurl)
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

            var apiresponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, callbackurl);
            Assert.AreEqual(400, (int)apiresponse.StatusCode, $"Incorrect status code {apiresponse.StatusCode}  is  returned, instead of the expected 400.");
        }
    }
}