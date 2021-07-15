using NUnit.Framework;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using System.Collections.Generic;
using System.IO;
using System;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    class ExchangeSetDownloadEncFiles
    {
        private ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        private TestConfiguration Config { get; set; }
        public DataHelper DataHelper { get; set; }
        public ProductIdentifierModel ProductIdentifierModel { get; set; }
        private string EssJwtToken { get; set; }
        private FssApiClient FssApiClient { get; set; }
        private string FssJwtToken { get; set; }
        private string ScsJwtToken { get; set; }
        private SalesCatalogueApiClient ScsApiClient { get; set; }

        [SetUp]
        public async Task SetupAsync()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            ProductIdentifierModel = new ProductIdentifierModel();
            DataHelper = new DataHelper();
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssApiClient = new FssApiClient();
            FssJwtToken = await authTokenProvider.GetFssToken();
            ScsApiClient = new SalesCatalogueApiClient(Config.ScsBaseAddress);
            ScsJwtToken = await authTokenProvider.GetScsToken();
        }

        [Test]
        public async Task WhenICallExchangeSetApiWithAValidProductVersions_ThenEncFilesAreDownloaded()
        {
            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData("DE316004", 13, 0));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseDetails = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseDetails.Links.ExchangeSetBatchStatusUri.Href;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);

            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseDetails.Links.ExchangeSetFileUri.Href;

            //Get temp downloaded folder
            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);

            //Get the product details form sales catalogue service
            var apiScsResponse = await ScsApiClient.GetProductVersionsAsync(Config.ExchangeSetProductType, ProductVersiondata, ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            foreach (var product in apiScsResponseData.Products)
            {
                string productName = product.ProductName;
                int editionNumber = product.EditionNumber;

                //Enc file downloaded verification
                foreach (var updateNumber in product.UpdateNumbers)
                {
                    await FileContentHelper.GetDownloadedEncFilesAsync(Config.FssConfig.FssApiUrl, Path.Combine(extractDownloadedFolder, Config.ExchangeSetEncRootFolder), productName, editionNumber, updateNumber, FssJwtToken);

                }

            }
        }

        [Test]
        public async Task WhenICallExchangeSetApiWithAInValidEditionNumber_ThenNoEncFilesAreDownloaded()
        {
            string productName = "DE316004";
            int editionNumber = 15;
            int updateNumber = 0;

            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData(productName, editionNumber, updateNumber));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseDetails = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseDetails.Links.ExchangeSetBatchStatusUri.Href;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);

            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseDetails.Links.ExchangeSetFileUri.Href;

            //Get temp downloaded folder
            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);

            //Verify No folder available for the product             
            FileContentHelper.CheckNoEncFilesDownloadedAsync(Path.Combine(extractDownloadedFolder, Config.ExchangeSetEncRootFolder), productName);

        }

        [Test]
        public async Task WhenICallExchangeSetApiWithAInValidUpdateNumber_ThenNoEncFilesAreDownloaded()
        {
            string productName = "DE316004";
            int editionNumber = 13;
            int updateNumber = 50;

            List<ProductVersionModel> ProductVersiondata = new List<ProductVersionModel>();

            ProductVersiondata.Add(DataHelper.GetProductVersionModelData(productName, editionNumber, updateNumber));

            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersiondata, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseDetails = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseDetails.Links.ExchangeSetBatchStatusUri.Href;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);

            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseDetails.Links.ExchangeSetFileUri.Href;

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);

            //Verify No folder available for the product             
            FileContentHelper.CheckNoEncFilesDownloadedAsync(Path.Combine(extractDownloadedFolder, Config.ExchangeSetEncRootFolder), productName);

        }

        [Test]
        public async Task WhenICallExchangeSetApiWithAnInvalidValidProductIdentifier_ThenEncFilesAreDownloadedd()
        {
            string productName = "GB416080";
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(new List<string>() { productName }, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseDetails = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseDetails.Links.ExchangeSetBatchStatusUri.Href;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);

            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseDetails.Links.ExchangeSetFileUri.Href;

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);

            //Verify No folder available for the product             
            FileContentHelper.CheckNoEncFilesDownloadedAsync(Path.Combine(extractDownloadedFolder, Config.ExchangeSetEncRootFolder), productName);

        }

        [Test]
        public async Task WhenICallExchangeSetApiWithAvalidValidProductIdentifier_ThenEncFilesAreDownloadedd()
        { 
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(new List<string>() { "DE416080" }, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseDetails = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseDetails.Links.ExchangeSetBatchStatusUri.Href;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);

            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseDetails.Links.ExchangeSetFileUri.Href;

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);           

            //Get the product details form sales catalogue service

            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, new List<string> { "DE416080" }, ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            string productName = apiScsResponseData.Products[0].ProductName;
            int editionNumber = apiScsResponseData.Products[0].EditionNumber;
            List<int> updateNumbers = apiScsResponseData.Products[0].UpdateNumbers;

            foreach (var updateNumber in updateNumbers)
            {
                await FileContentHelper.GetDownloadedEncFilesAsync(Config.FssConfig.FssApiUrl, Path.Combine(extractDownloadedFolder, Config.ExchangeSetEncRootFolder), productName, editionNumber, updateNumber, FssJwtToken);
                
            }
        }

        [Test]
        public async Task WhenICallExchangeSetApiWithAvalidValidMultipleProductIdentifiers_ThenEncFilesAreDownloadedd()
        {
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(new List<string>() { "DE416080", "DE316004" }, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseDetails = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseDetails.Links.ExchangeSetBatchStatusUri.Href;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);

            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status is Committed.");

            var downloadFileUrl = apiResponseDetails.Links.ExchangeSetFileUri.Href;

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);

            //Get the product details form sales catalogue service

            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, new List<string> { "DE416080", "DE316004" }, ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            foreach(var product in apiScsResponseData.Products)
            {
                string productName = product.ProductName;
                int editionNumber = product.EditionNumber;

                //Enc file downloaded verification
                foreach (var updateNumber in product.UpdateNumbers)
                {
                    await FileContentHelper.GetDownloadedEncFilesAsync(Config.FssConfig.FssApiUrl, Path.Combine(extractDownloadedFolder, Config.ExchangeSetEncRootFolder), productName, editionNumber, updateNumber, FssJwtToken);

                }

            }                    
        }

    }
}
