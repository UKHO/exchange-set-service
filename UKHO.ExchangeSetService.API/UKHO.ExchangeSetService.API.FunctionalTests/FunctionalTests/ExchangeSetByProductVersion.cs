using NUnit.Framework;
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
        public ProductVersionRequestModel ProductVersionRequestmodel { get; set; }

        [SetUp]
        public void Setup()
        {
            Config = new TestConfiguration();
            ExchangesetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductVersionmodel = new ProductVersionModel();
            Datahelper = new DataHelper();
            ProductVersionRequestmodel = new ProductVersionRequestModel();
        }

        [Test]
        public async Task WhenICallTheApiWithAValidProductVersion_ThenASuccessStatusIsReturned()
        {
            ProductVersionmodel.ProductName = "AU895561";
            ProductVersionmodel.EditionNumber = 4;
            ProductVersionmodel.UpdateNumber = 1;

            System.Collections.Generic.List<ProductVersionModel> data = new System.Collections.Generic.List<ProductVersionModel>();
            data.Add(Datahelper.GetProductVersionModelData(ProductVersionmodel.ProductName, ProductVersionmodel.EditionNumber, ProductVersionmodel.UpdateNumber));

            var apiresponse = await ExchangesetApiClient.GetProductVersionsAsync(new ProductVersionRequestModel
            {
                ProductVersions = data, CallbackUri = null
            });
            Assert.AreEqual(200, (int)apiresponse.StatusCode, $"Exchange Set for datetime is  returned {apiresponse.StatusCode}, instead of theEditionNumber expected 200.");

        }
    }
}
