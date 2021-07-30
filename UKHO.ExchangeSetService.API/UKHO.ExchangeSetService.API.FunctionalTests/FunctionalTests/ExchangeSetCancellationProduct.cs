using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class ExchangeSetCancellationProduct
    {
        private string EssJwtToken { get; set; }
        private string FssJwtToken { get; set; }
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper DataHelper { get; set; }
        private SalesCatalogueApiClient ScsApiClient { get; set; }
        private string ScsJwtToken { get; set; }
        public ProductIdentifierModel ProductIdentifierModel { get; set; }
        
        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductIdentifierModel = new ProductIdentifierModel();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssJwtToken = await authTokenProvider.GetFssToken();
            DataHelper = new DataHelper();
            ScsApiClient = new SalesCatalogueApiClient(Config.ScsAuthConfig.BaseUrl);
            ScsJwtToken = await authTokenProvider.GetScsToken();

        }     

        [Test]
        public async Task WhenICallExchangeSetProductIdentifierApiWithACancelledProduct_ThenCatalogueFileUpdatedWithEditionNumberZero()
        {
            ProductIdentifierModel.ProductIdentifier = new List<string>() { "DE516510" };

            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(ProductIdentifierModel.ProductIdentifier, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseData.Links.ExchangeSetBatchStatusUri.Href;

            string[] batchUri = batchStatusUrl.Split("/");
            var batchId = batchUri[batchUri.Length - 1];

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);
            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus} for url {batchStatusUrl}, instead of the expected status Committed.");

            var downloadFileUrl = apiResponseData.Links.ExchangeSetFileUri.Href;

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);

            var downloadFolder = FssBatchHelper.RenameFolder(extractDownloadedFolder);
            var downloadFolderPath = Path.Combine(Path.GetTempPath(), downloadFolder);

            
            //Verify Cancellation details
            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, ProductIdentifierModel.ProductIdentifier, ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<CancellationResponseModel>();

            foreach (var product in apiScsResponseData.Products)
            {
                var productName = product.ProductName;
                var editionNumber = product.Cancellation.EditionNumber;
                Assert.AreEqual(0, editionNumber, $"Incorrect edition number is returned {editionNumber}, instead of 0.");

                var updateNumber = product.UpdateNumbers[product.UpdateNumbers.Count-1];

                CancellationFileHelper.CheckCatalogueFileContent(Path.Combine(downloadFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeSetCatalogueFile), editionNumber, updateNumber, batchId);
                CancellationFileHelper.CheckProductFileContent(Path.Combine(downloadFolderPath, Config.ExchangeSetProductFilePath, Config.ExchangeSetProductFile), productName, editionNumber);
           
            }
        }

        [Test]
        public async Task WhenICallExchangeSetProductVersionsApiWithACancelledProduct_ThenCatalogueFileUpdatedWithEditionNumberZero()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData("DE516510", 1, 1));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseData.Links.ExchangeSetBatchStatusUri.Href;

            string[] batchUri = batchStatusUrl.Split("/");
            var batchId = batchUri[batchUri.Length - 1];

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);
            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus} for url {batchStatusUrl}, instead of the expected status Committed.");

            var downloadFileUrl = apiResponseData.Links.ExchangeSetFileUri.Href;

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);

            var downloadFolder = FssBatchHelper.RenameFolder(extractDownloadedFolder);
            var downloadFolderPath = Path.Combine(Path.GetTempPath(), downloadFolder);

            //Verify Cancellation details
            var apiScsResponse = await ScsApiClient.GetProductVersionsAsync(Config.ExchangeSetProductType, ProductVersiondata, ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<CancellationResponseModel>();

            foreach (var product in apiScsResponseData.Products)
            {
                var productName = product.ProductName;
                var editionNumber = product.Cancellation.EditionNumber;
                Assert.AreEqual(0, editionNumber, $"Incorrect edition number is returned {editionNumber}, instead of 0.");

                var updateNumber = product.UpdateNumbers[product.UpdateNumbers.Count - 1];

                CancellationFileHelper.CheckCatalogueFileContent(Path.Combine(downloadFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeSetCatalogueFile), editionNumber, updateNumber, batchId);
                CancellationFileHelper.CheckProductFileContent(Path.Combine(downloadFolderPath, Config.ExchangeSetProductFilePath, Config.ExchangeSetProductFile), productName, editionNumber);

            }
        }

        [TearDown]
        public void CleanUpExchangeSetTeardown()
        {
            //Clean up downloaded files/folders   
            CancellationFileHelper.DeleteDirectory(Config.ExchangeSetFileName);
        }
    }
}
