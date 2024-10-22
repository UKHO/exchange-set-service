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
        private FssApiClient FssApiClient { get; set; }
        private readonly List<string> CleanUpBatchIdList = new List<string>();

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            FssApiClient = new FssApiClient();
            ProductIdentifierModel = new ProductIdentifierModel();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssJwtToken = await authTokenProvider.GetFssToken();
            DataHelper = new DataHelper();
            ScsApiClient = new SalesCatalogueApiClient(Config.ScsAuthConfig.BaseUrl);
            ScsJwtToken = await authTokenProvider.GetScsToken();
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallExchangeSetProductIdentifierApiWithACancelledProduct_ThenCatalogueFileUpdatedWithEditionNumberZero()
        {
            ProductIdentifierModel.ProductIdentifier = new List<string>() { "DE516510" };

            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(ProductIdentifierModel.ProductIdentifier, accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseData.Links.ExchangeSetBatchStatusUri.Href;

            var batchId = batchStatusUrl.Split('/')[5];

            CleanUpBatchIdList.Add(batchId);

            var finalBatchStatusUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/status";

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(finalBatchStatusUrl.ToString(), FssJwtToken);
            Assert.That(batchStatus,Is.EqualTo("Committed"), $"Incorrect batch status is returned {batchStatus} for url {batchStatusUrl}, instead of the expected status Committed.");

            var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{Config.ExchangeSetFileName}";

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);


            //Verify Cancellation details
            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, ProductIdentifierModel.ProductIdentifier, ScsJwtToken);
            Assert.That((int)apiScsResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<CancellationResponseModel>();

            foreach (var product in apiScsResponseData.Products)
            {
                var productName = product.ProductName;
                var editionNumber = product.Cancellation.EditionNumber;
                Assert.That(editionNumber, Is.EqualTo(0), $"Incorrect edition number is returned {editionNumber}, instead of 0.");

                var updateNumber = product.UpdateNumbers[product.UpdateNumbers.Count - 1];

                CancellationFileHelper.CheckCatalogueFileContent(Path.Combine(extractDownloadedFolder, Config.ExchangeSetEncRootFolder, Config.ExchangeSetCatalogueFile), editionNumber, updateNumber, batchId);
                CancellationFileHelper.CheckProductFileContent(Path.Combine(extractDownloadedFolder, Config.ExchangeSetProductFilePath, Config.ExchangeSetProductFile), productName, editionNumber);
            }
        }

        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallExchangeSetProductVersionsApiWithACancelledProduct_ThenCatalogueFileUpdatedWithEditionNumberZero()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData("DE516510", 2, 1));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code {apiResponse.StatusCode}  is  returned, instead of the expected 200.");

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseData.Links.ExchangeSetBatchStatusUri.Href;

            var batchId = batchStatusUrl.Split('/')[5];

            CleanUpBatchIdList.Add(batchId);

            var finalBatchStatusUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/status";

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(finalBatchStatusUrl.ToString(), FssJwtToken);
            Assert.That(batchStatus, Is.EqualTo("Committed"), $"Incorrect batch status is returned {batchStatus} for url {batchStatusUrl}, instead of the expected status Committed.");

            var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{Config.ExchangeSetFileName}";

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);

            //Verify Cancellation details
            var apiScsResponse = await ScsApiClient.GetProductVersionsAsync(Config.ExchangeSetProductType, ProductVersiondata, ScsJwtToken);
            Assert.That((int)apiScsResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<CancellationResponseModel>();

            foreach (var product in apiScsResponseData.Products)
            {
                var productName = product.ProductName;
                var editionNumber = product.Cancellation.EditionNumber;
                Assert.That(editionNumber, Is.EqualTo(0), $"Incorrect edition number is returned {editionNumber}, instead of 0.");

                var updateNumber = product.UpdateNumbers[product.UpdateNumbers.Count - 1];

                CancellationFileHelper.CheckCatalogueFileContent(Path.Combine(extractDownloadedFolder, Config.ExchangeSetEncRootFolder, Config.ExchangeSetCatalogueFile), editionNumber, updateNumber, batchId);
                CancellationFileHelper.CheckProductFileContent(Path.Combine(extractDownloadedFolder, Config.ExchangeSetProductFilePath, Config.ExchangeSetProductFile), productName, editionNumber);
            }
        }

        [TearDown]
        public async Task GlobalTeardown()
        {
            //Clean up downloaded files/folders   
            FileContentHelper.DeleteDirectory(Config.ExchangeSetFileName);

            if (CleanUpBatchIdList != null && CleanUpBatchIdList.Count > 0)
            {
                //Clean up batches from local foldar 
                var apiResponse = await FssApiClient.CleanUpBatchesAsync(Config.FssConfig.BaseUrl, CleanUpBatchIdList, FssJwtToken);
                Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code {apiResponse.StatusCode}  is  returned for clean up batches, instead of the expected 200.");
            }
        }
    }
}
