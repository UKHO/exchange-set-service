using Newtonsoft.Json;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using System.Collections.Generic;
using System;
using System.Globalization;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    [TestFixture]
    public class ExchangeSetGenerateFilesForProductIdentifiersWithoutExchangeSetStandardParameter : ObjectStorage
    {
        private readonly List<string> cleanUpBatchIdList = new();
        private readonly List<string> downloadedFolderPathList = new();
        private readonly string sinceDateTime = DateTime.Now.AddDays(-5).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            DataHelper = new DataHelper();
            var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataWithoutExchangeSetStandardParameterAsync(DataHelper.GetProductIdentifiers(), accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned  {apiResponse.StatusCode}, instead of the expected status 200.");
            var batchId = await apiResponse.GetBatchId();
            cleanUpBatchIdList.Add(batchId);
            var downloadFolderPath = await FileContentHelper.CreateExchangeSetFile(apiResponse, FssJwtToken);
            downloadedFolderPathList.Add(downloadFolderPath);

            //product version
            ProductVersionData = new List<ProductVersionModel>
            {
                 DataHelper.GetProductVersionModelData("DE416040", 11, 0),
                 DataHelper.GetProductVersionModelData("DE360010", 1, 0)
            };
            var apiResponsepv = await ExchangeSetApiClient.GetProductVersionsWithoutExchangeSetStandardParameterAsync(ProductVersionData, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponsepv.StatusCode, $"Incorrect status code is returned  {apiResponsepv.StatusCode}, instead of the expected status 200.");
            var batchIdpv = await apiResponsepv.GetBatchId();
            cleanUpBatchIdList.Add(batchIdpv);
            var downloadedFolderPathpv = await FileContentHelper.CreateExchangeSetFile(apiResponsepv, FssJwtToken);
            downloadedFolderPathList.Add(downloadedFolderPathpv);

            //since dateTime
            var apiResponsesdt = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeWithoutExchangeSetStandardParameterAsync(sinceDateTime, accessToken: EssJwtToken);
            Assert.AreEqual(200, (int)apiResponsesdt.StatusCode, $"Incorrect status code is returned  {apiResponsesdt.StatusCode}, instead of the expected status 200.");
            var batchIdsdt = await apiResponsesdt.GetBatchId();
            cleanUpBatchIdList.Add(batchIdsdt);
            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFile(apiResponsesdt, FssJwtToken);
        }

        //PBI 143370: Change related to additional param (From Boolean to String)
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallProductIdentifiersApiWithS63ExchangeSetStandardParameterAndWithMultipleProductIdentifiers_ThenAProductTxtFileIsGenerated()
        {
            var checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath), Config.ExchangeSetProductFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath)}");

            //Verify Product.txt file content
            var apiScsResponse = await ScsApiClient.GetScsCatalogueAsync(Config.ExchangeSetProductType, Config.ExchangeSetCatalogueType, ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");
            var apiResponseDetails = await apiScsResponse.ReadAsStringAsync();
            dynamic apiScsResponseData = JsonConvert.DeserializeObject(apiResponseDetails);

            FileContentHelper.CheckProductFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetProductFilePath, Config.ExchangeSetProductFile), apiScsResponseData);
        }

         //PBI 143370: Change related to additional param (From Boolean to String)
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public void WhenICallProductIdentifiersApiWithS63ExchangeSetStandardParameterAndWithMultipleProductIdentifiers_ThenAReadMeTxtFileIsGenerated()
        {
            var checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeReadMeFile);
            Assert.IsTrue(checkFile, $"{Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder)}");

            //Verify README.TXT file content
            FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeReadMeFile));
        }

         //PBI 143370: Change related to additional param (From Boolean to String)
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallProductIdentifiersApiWithS63ExchangeSetStandardParameterAndWithMultipleProductIdentifiers_ThenACatalogFileIsGenerated()
        {
            var checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeSetCatalogueFile);
            Assert.IsTrue(checkFile, $"File not Exist in the specified folder path : {Path.Combine(DownloadedFolderPath, Config.ExchangeSetCatalogueFile)}");

            //Verify Catalog file content
            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, DataHelper.GetProductIdentifiers(), ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            FileContentHelper.CheckCatalogueFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeSetCatalogueFile), apiScsResponseData);
        }

         //PBI 143370: Change related to additional param (From Boolean to String)
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public void WhenICallProductIdentifiersApiWithS63ExchangeSetStandardParameterAndWithAValidProductIdentifiers_ThenASerialEncFileIsGenerated()
        {
            var checkFile = FssBatchHelper.CheckforFileExist(DownloadedFolderPath, Config.ExchangeSetSerialEncFile);
            Assert.IsTrue(checkFile, $"{Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {DownloadedFolderPath}");

            //Verify Serial.Enc file content
            FileContentHelper.CheckSerialEncFileContent(Path.Combine(DownloadedFolderPath, Config.ExchangeSetSerialEncFile));
        }

         //PBI 143370: Change related to additional param (From Boolean to String)
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallProductIdentifiersApiWithS63ExchangeSetStandardParameterAndWithMultipleProductIdentifiers_ThenEncFilesAreDownloaded()
        {
            //Get the product details form sales catalog service
            var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, DataHelper.GetProductIdentifiers(), ScsJwtToken);
            Assert.AreEqual(200, (int)apiScsResponse.StatusCode, $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

            var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

            foreach (var product in apiScsResponseData.Products)
            {
                var productName = product.ProductName;
                var editionNumber = product.EditionNumber;

                //Enc file download verification
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

            if (cleanUpBatchIdList?.Count > 0)
            {
                //Clean up batches from local folder 
                var apiResponse = await FssApiClient.CleanUpBatchesAsync(Config.FssConfig.BaseUrl, cleanUpBatchIdList, FssJwtToken);
                Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code {apiResponse.StatusCode}  is  returned for clean up batches, instead of the expected 200.");
            }
        }
    }
}