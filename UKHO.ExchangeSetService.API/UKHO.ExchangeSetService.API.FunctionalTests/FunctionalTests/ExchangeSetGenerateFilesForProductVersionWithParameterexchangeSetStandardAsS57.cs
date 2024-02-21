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
    public class ExchangeSetGenerateFilesForProductVersionWithParameterexchangeSetStandardAsS57 : ObjectStorage
    {
        private readonly List<string> cleanUpBatchIdList = new();
        private readonly List<string> downloadedFolderPathList = new();

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            DataHelper = new DataHelper();
            foreach (var exchangeSetStandard in Config.BESSConfig.S57ExchangeSetTestData)
            {
                ProductVersionData = new List<ProductVersionModel>
                {
                    DataHelper.GetProductVersionModelData("DE416040", 11, 0)
                };
                var apiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, null, accessToken: EssJwtToken, exchangeSetStandard);
                Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned  {apiResponse.StatusCode}, instead of the expected status 200.");
                var batchId = await apiResponse.GetBatchId();
                cleanUpBatchIdList.Add(batchId);
                var downloadFolderPath = await FileContentHelper.CreateExchangeSetFile(apiResponse, FssJwtToken);
                downloadedFolderPathList.Add(downloadFolderPath);
            }
        }

         //PBI 143370: Change related to additional param (From Boolean to String)
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallProductVersionApiWithS57ForParameterexchangeSetStandard_ThenAProductTxtFileIsGenerated()
        {
            foreach (var downloadedFolderPath in downloadedFolderPathList)
            {
                var checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(downloadedFolderPath, Config.ExchangeSetProductFilePath), Config.ExchangeSetProductFile);
                Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(downloadedFolderPath, Config.ExchangeSetProductFilePath)}");

                //Verify Product.txt file content
                var apiScsResponse = await ScsApiClient.GetScsCatalogueAsync(Config.ExchangeSetProductType, Config.ExchangeSetCatalogueType, ScsJwtToken);
                Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");
                var apiResponseDetails = await apiScsResponse.ReadAsStringAsync();
                dynamic apiScsResponseData = JsonConvert.DeserializeObject(apiResponseDetails);

                FileContentHelper.CheckProductFileContent(Path.Combine(downloadedFolderPath, Config.ExchangeSetProductFilePath, Config.ExchangeSetProductFile), apiScsResponseData);
            }
        }

         //PBI 143370: Change related to additional param (From Boolean to String)
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public void WhenICallProductVersionApiWithS57ForParameterexchangeSetStandard_ThenAReadMeTxtFileIsGenerated()
        {
            foreach (var downloadedFolderPath in downloadedFolderPathList)
            {
                var checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(downloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeReadMeFile);
                Assert.IsTrue(checkFile, $"{Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(downloadedFolderPath, Config.ExchangeSetEncRootFolder)}");

                //Verify the README.TXT file content
                FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(downloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeReadMeFile));
            }
        }

         //PBI 143370: Change related to additional param (From Boolean to String)
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallProductVersionApiWithS57ForParameterexchangeSetStandard_ThenACatalogFileIsGenerated()
        {
            foreach (var downloadedFolderPath in downloadedFolderPathList)
            {
                var checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(downloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeSetCatalogueFile);
                Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(downloadedFolderPath, Config.ExchangeSetEncRootFolder)}");

                //Verify the Catalog file content
                var apiScsResponse = await ScsApiClient.GetProductVersionsAsync(Config.ExchangeSetProductType, ProductVersionData, ScsJwtToken);
                Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");
                var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

                FileContentHelper.CheckCatalogueFileContent(Path.Combine(downloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeSetCatalogueFile), apiScsResponseData);
            }
        }

         //PBI 143370: Change related to additional param (From Boolean to String)
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public void WhenICallProductVersionApiWithS57ForParameterexchangeSetStandard_ThenASerialEncFileIsGenerated()
        {
            foreach (var downloadedFolderPath in downloadedFolderPathList)
            {
                var checkFile = FssBatchHelper.CheckforFileExist(downloadedFolderPath, Config.ExchangeSetSerialEncFile);
                Assert.IsTrue(checkFile, $"{Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {downloadedFolderPath}");

                //Verify Serial.Enc file content
                FileContentHelper.CheckSerialEncFileContent(Path.Combine(downloadedFolderPath, Config.ExchangeSetSerialEncFile));
            }
        }

         //PBI 143370: Change related to additional param (From Boolean to String)
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallProductVersionApiWithS57ForParameterexchangeSetStandard_ThenEncFilesAreDownloaded()
        {
            foreach (var downloadedFolderPath in downloadedFolderPathList)
            {
                //Get the product details form sales catalogue service
                var apiScsResponse = await ScsApiClient.GetProductVersionsAsync(Config.ExchangeSetProductType, ProductVersionData, ScsJwtToken);
                Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

                var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

                foreach (var product in apiScsResponseData.Products)
                {
                    var productName = product.ProductName;
                    var editionNumber = product.EditionNumber;

                    //Enc file downloaded verification
                    foreach (var updateNumber in product.UpdateNumbers)
                    {
                        await FileContentHelper.GetDownloadedEncFilesAsync(Config.FssConfig.BaseUrl, Path.Combine(downloadedFolderPath, Config.ExchangeSetEncRootFolder), productName, editionNumber, updateNumber, FssJwtToken);
                    }
                }
            }
        }

        [OneTimeTearDown]
        public async Task GlobalTeardown()
        {
            //Clean up downloaded files/folders   
            FileContentHelper.DeleteDirectory(Config.ExchangeSetFileName);

            if (cleanUpBatchIdList?.Count > 0)
            {
                //Clean up batches from local folder 
                var apiResponse = await FssApiClient.CleanUpBatchesAsync(Config.FssConfig.BaseUrl, cleanUpBatchIdList, FssJwtToken);
                Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned for clean up batches, instead of the expected 200.");
            }
        }
    }
}