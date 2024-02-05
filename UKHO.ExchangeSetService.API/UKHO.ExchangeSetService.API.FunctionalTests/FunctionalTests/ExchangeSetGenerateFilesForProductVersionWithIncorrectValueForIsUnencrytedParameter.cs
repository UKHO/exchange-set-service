using Newtonsoft.Json;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    [TestFixture]
    public class ExchangeSetGenerateFilesForProductVersionWithIncorrectValueForIsUnencrytedParameter : ObjectStorage
    {
        private readonly List<string> cleanUpBatchIdList = new List<string>();

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            ProductVersionData = new List<ProductVersionModel>();
            ProductVersionData.Add(DataHelper.GetProductVersionModelData("DE416040", 11, 0));
            var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, null, accessToken: EssJwtToken, "Test123");
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned  {apiResponse.StatusCode}, instead of of the expected status 200.");
            var batchId = await apiResponse.GetBatchId();
            cleanUpBatchIdList.Add(batchId);
            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFile(apiResponse, FssJwtToken);
        }

        //PBI:139801 : ESS API : Create and add optional parameter IsUnencrypted, add validation and Update Swagger Doc
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallProductVersionApiWithIncorrectValueForIsUnencrytedParameter_ThenAProductTxtFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath), Config.ExchangeSetProductFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath)}");

            //Verify Product.txt file content
            var apiScsResponse = await ScsApiClient.GetScsCatalogueAsync(Config.ExchangeSetProductType, Config.ExchangeSetCatalogueType, ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");
            var apiResponseDetails = await apiScsResponse.ReadAsStringAsync();
            dynamic apiScsResponseData = JsonConvert.DeserializeObject(apiResponseDetails);

            FileContentHelper.CheckProductFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath, Config.ExchangeSetProductFile), apiScsResponseData);
        }

        //PBI:139801 : ESS API : Create and add optional parameter IsUnencrypted, add validation and Update Swagger Doc
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public void WhenICallProductVersionApiWithIncorrectValueForIsUnencrytedParameter_ThenAReadMeTxtFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeReadMeFile);
            Assert.IsTrue(checkFile, $"{Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder)}");

            //Verify the README.TXT file content
            FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeReadMeFile));
        }

        //PBI:139801 : ESS API : Create and add optional parameter IsUnencrypted, add validation and Update Swagger Doc
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallProductVersionApiWithIncorrectValueForIsUnencrytedParameter_ThenACatalogueFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeSetCatalogueFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder)}");

            //Verify the Catalog file content
            var apiScsResponse = await ScsApiClient.GetProductVersionsAsync(Config.ExchangeSetProductType, ProductVersionData, ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");
            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            FileContentHelper.CheckCatalogueFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeSetCatalogueFile), apiScsResponseData);
        }

        //PBI:139801 : ESS API : Create and add optional parameter IsUnencrypted, add validation and Update Swagger Doc
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public void WhenICallProductVersionApiWithIncorrectValueForIsUnencrytedParameter_ThenASerialEncFileIsGenerated()
        {
            bool checkFile = FssBatchHelper.CheckforFileExist(DownloadedFolderPath, Config.ExchangeSetSerialEncFile);
            Assert.IsTrue(checkFile, $"{Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {DownloadedFolderPath}");

            //Verify Serial.Enc file content
            FileContentHelper.CheckSerialEncFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetSerialEncFile));
        }

        //PBI:139801 : ESS API : Create and add optional parameter IsUnencrypted, add validation and Update Swagger Doc
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallProductVersionApiWithIncorrectValueForIsUnencrytedParameter_ThenEncFilesAreDownloaded()
        {
            //Get the product details form sales catalogue service
            var apiScsResponse = await ScsApiClient.GetProductVersionsAsync(Config.ExchangeSetProductType, ProductVersionData, ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            foreach (var product in apiScsResponseData.Products)
            {
                string productName = product.ProductName;
                int editionNumber = product.EditionNumber;

                //Enc file downloaded verification
                foreach (var updateNumber in product.UpdateNumbers)
                {
                    await FileContentHelper.GetDownloadedEncFilesAsync(Config.FssConfig.BaseUrl, Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), productName, editionNumber, updateNumber, FssJwtToken);

                }
            }
        }

        [OneTimeTearDown]
        public async Task GlobalTeardown()
        {
            //Clean up downloaded files/folders   
            FileContentHelper.DeleteDirectory(Config.ExchangeSetFileName);

            if (cleanUpBatchIdList != null && cleanUpBatchIdList.Count > 0)
            {
                //Clean up batches from local foldar 
                var apiResponse = await FssApiClient.CleanUpBatchesAsync(Config.FssConfig.BaseUrl, cleanUpBatchIdList, FssJwtToken);
                Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned for clean up batches, instead of the expected 200.");
            }
        }
    }
}