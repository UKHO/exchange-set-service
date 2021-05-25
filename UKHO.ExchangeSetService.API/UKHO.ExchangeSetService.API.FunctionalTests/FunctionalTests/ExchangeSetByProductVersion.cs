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
        public DataHelper DataHelper { get; set; }       
        private string EssJwtToken { get; set; }
        private string EssJwtTokenNoRole { get; set; }
        private string EssJwtCustomizedToken { get; set; }

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangesetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            DataHelper = new DataHelper();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            EssJwtTokenNoRole = await authTokenProvider.GetEssTokenNoAuth();
            EssJwtCustomizedToken = authTokenProvider.GenerateCustomToken();

        }

        [Test]
        public async Task WhenICallTheApiWithOutAuthToken_ThenAnUnauthorisedResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 6));           
                       
            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        public async Task WhenICallTheApiWithTamperedToken_ThenAnUnauthorisedResponseIsReturned()
        {
            string tamperedEssJwtToken = EssJwtToken.Remove(EssJwtToken.Length - 2);

            List<ProductVersionModel> ProductVersionData = new List<ProductVersionModel>();

            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 6));
            
            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersionData, accessToken: tamperedEssJwtToken);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }


        [Test]
        public async Task WhenICallTheApiWithCustomToken_ThenAnUnauthorisedResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData("DE4NO18Q", 1, 0));            

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtCustomizedToken);

            Assert.AreEqual(401, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 401.");
        }

        [Test]
        public async Task WhenICallTheApiWithNoRoleToken_ThenAForbiddenResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 6));
            
            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtTokenNoRole);

            Assert.AreEqual(403, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 403.");
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductVersion_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 6));
            ProductVersiondata.Add(DataHelper.GetProductVersionModelData("DE4NO18Q", 1, 0));

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

        }

        [Test]
        public async Task WhenICallTheApiWithValidAndInvalidProductVersion_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData("DE416080", 9, 6));
            ProductVersiondata.Add(DataHelper.GetProductVersionModelData("DE41AB80", 9, 2));           

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            Assert.AreEqual("DE41AB80", apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName, $"Exchange set returned Product Name {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().ProductName}, instead of expected Product Name 'DE41AB80'");
            Assert.AreEqual("invalidProduct", apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");
        }

        [Test]
        public async Task WhenICallTheApiWithValidDuplicateProductVersion_ThenTheCorrectResponseIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData("DE4NO18Q", 1, 0));
            ProductVersiondata.Add(DataHelper.GetProductVersionModelData("DE4NO18Q", 1, 0));

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

            //verify model structure
            await apiResponse.CheckModelStructureNotModifiedResponse();

        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductVersionWithCallbackURI_ThenASuccessStatusIsReturned()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();      

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData("DE4NO18Q",1,0));
                    
            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, "https://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272", accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

        }

        [TestCase("DE416080", 10, 6, TestName = "EditionNumber Unavailable")]
        [TestCase("DE416080", 9, 7, TestName = "UpdateNumber Unavailable")]
        public async Task WhenICallTheApiWithAProductVersionNotAvailable_ThenTheCorrectResponseIsReturned(string productname, int editionnumber, int updatenumber)
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();           

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData(productname, editionnumber, updatenumber));

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

        [TestCase(null, 9, 6, "ProductVersions[0].ProductName", "ProductName cannot be blank or null.", TestName = "When product name is null.")]
        [TestCase("", 9, 6, "ProductVersions[0].ProductName", "ProductName cannot be blank or null.", TestName = "When product name is blank.")]
        [TestCase("DE416080", null, 6, "ProductVersions[0].EditionNumber", "EditionNumber cannot be less than zero or null.", TestName = "When edition number is null.")]
        [TestCase("DE416080", 9, null, "ProductVersions[0].UpdateNumber", "UpdateNumber cannot be less than zero or null.", TestName = "When update number is null.")]
        [TestCase("DE416080", -1, 1, "ProductVersions[0].EditionNumber", "EditionNumber cannot be less than zero or null.", TestName = "When edition number is less than zero.")]
        [TestCase("DE416080", 4, -1, "ProductVersions[0].UpdateNumber", "UpdateNumber cannot be less than zero or null.", TestName = "When update number is less than zero.")]
        public async Task WhenICallTheApiWithNullProductVersion_ThenABadRequestStatusIsReturned(string productName, int? editionNumber, int? updateNumber, string sourceMessage, string descriptionMessage)
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData(productName, editionNumber, updateNumber));

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

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData("DE416080",9,6));           

            var apiResponse = await ExchangesetApiClient.GetProductVersionsAsync(ProductVersiondata, callBackUrl, accessToken: EssJwtToken);
            Assert.AreEqual(400, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode} is returned, instead of the expected 400.");

        }
    }
}
