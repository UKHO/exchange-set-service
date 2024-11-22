using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Helper;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.FunctionalTests
{
    [TestFixture]
    public class ExchangeSetGenerateFilesForEssApisWithS63ExchangeSetStandardParameter : ObjectStorage
    {
        private readonly List<string> cleanUpBatchIdList = new();
        private readonly List<string> downloadedFolderPathList = new();
        private readonly string sinceDateTime = DateTime.Now.AddDays(-5).ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture);

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            DataHelper = new DataHelper();
            foreach (var exchangeSetStandard in Config.BESSConfig.S63ExchangeSetTestData)
            {
                //product identifiers
                var apiResponse = await ExchangeSetApiClient.GetProductIdentifiersDataAsync(DataHelper.GetProductIdentifiers(), null, accessToken: EssJwtToken, exchangeSetStandard);
                Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned  {apiResponse.StatusCode}, instead of the expected status 200.");
                var batchId = await apiResponse.GetBatchId();
                cleanUpBatchIdList.Add(batchId);
                var downloadFolderPath = await FileContentHelper.CreateExchangeSetFile(apiResponse, FssJwtToken);
                downloadedFolderPathList.Add(downloadFolderPath);

                ////product versions
                ProductVersionData = new List<ProductVersionModel>
                {
                    DataHelper.GetProductVersionModelData("DE416040", 11, 0),
                    DataHelper.GetProductVersionModelData("DE360010", 1, 0)
                };
                var productVersionsApiResponse = await ExchangeSetApiClient.GetProductVersionsAsync(ProductVersionData, null, accessToken: EssJwtToken, exchangeSetStandard);
                Assert.That((int)productVersionsApiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned  {productVersionsApiResponse.StatusCode}, instead of the expected status 200.");
                var productVersionsBatchId = await productVersionsApiResponse.GetBatchId();
                cleanUpBatchIdList.Add(productVersionsBatchId);
                var productVersionsDownloadedFolderPath = await FileContentHelper.CreateExchangeSetFile(productVersionsApiResponse, FssJwtToken);
                downloadedFolderPathList.Add(productVersionsDownloadedFolderPath);

                ////since dateTime
                var sinceDateTimeApiResponse = await ExchangeSetApiClient.GetExchangeSetBasedOnDateTimeAsync(sinceDateTime, null, accessToken: EssJwtToken, exchangeSetStandard);
                Assert.That((int)sinceDateTimeApiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned  {sinceDateTimeApiResponse.StatusCode}, instead of the expected status 200.");
                var sinceDateTimeBatchId = await sinceDateTimeApiResponse.GetBatchId();
                cleanUpBatchIdList.Add(sinceDateTimeBatchId);
                var sinceDateTimeDownloadFolderPath = await FileContentHelper.CreateExchangeSetFile(sinceDateTimeApiResponse, FssJwtToken);
                downloadedFolderPathList.Add(sinceDateTimeDownloadFolderPath);
            }
        }

        //PBI 143370: Change related to additional param (From Boolean to String)
        //PBI 140672: ESS : Fulfilment service(webjob) - Get data from new BU
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        [Ignore("rhz Test disabled")]
        public async Task WhenICallEssApisWithS63ExchangeSetStandardParameter_ThenAProductTxtFileIsGenerated()
        {
            foreach (var downloadedFolderPath in downloadedFolderPathList)
            {
                var checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(downloadedFolderPath, Config.ExchangeSetProductFilePath), Config.ExchangeSetProductFile);
                Assert.That(checkFile, Is.True, $"File not Exist in the specified folder path : {Path.Combine(downloadedFolderPath, Config.ExchangeSetProductFilePath)}");

                //Verify Product.txt file content
                var apiScsResponse = await ScsApiClient.GetScsCatalogueAsync(Config.ExchangeSetProductType, Config.ExchangeSetCatalogueType, ScsJwtToken);
                Assert.That((int)apiScsResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");
                var apiResponseDetails = await apiScsResponse.ReadAsStringAsync();
                dynamic apiScsResponseData = JsonConvert.DeserializeObject(apiResponseDetails);

                FileContentHelper.CheckProductFileContent(Path.Combine(downloadedFolderPath, Config.ExchangeSetProductFilePath, Config.ExchangeSetProductFile), apiScsResponseData);
            }
        }

        //PBI 143370: Change related to additional param (From Boolean to String)
        //PBI 140672: ESS : Fulfilment service(webjob) - Get data from new BU
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public void WhenICallEssApisWithS63ExchangeSetStandardParameter_ThenAReadMeTxtFileIsGenerated()
        {
            foreach (var downloadedFolderPath in downloadedFolderPathList)
            {
                var checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(downloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeReadMeFile);
                Assert.That(checkFile, Is.True, $"{Config.ExchangeReadMeFile} File not Exist in the specified folder path : {Path.Combine(downloadedFolderPath, Config.ExchangeSetEncRootFolder)}");

                //Verify README.TXT file content
                FileContentHelper.CheckReadMeTxtFileContent(Path.Combine(downloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeReadMeFile));
            }
        }

        //PBI 143370: Change related to additional param (From Boolean to String)
        //PBI 140672: ESS : Fulfilment service(webjob) - Get data from new BU
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallEssApisWithS63ExchangeSetStandardParameter_ThenACatalogFileIsGenerated()
        {
            foreach (var downloadedFolderPath in downloadedFolderPathList)
            {
                var checkFile = FssBatchHelper.CheckforFileExist(Path.Combine(downloadedFolderPath, Config.ExchangeSetEncRootFolder), Config.ExchangeSetCatalogueFile);
                Assert.That(checkFile, Is.True, $"File not Exist in the specified folder path : {Path.Combine(downloadedFolderPath, Config.ExchangeSetCatalogueFile)}");

                //Verify Catalog file content
                var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, DataHelper.GetProductIdentifiers(), ScsJwtToken);
                Assert.That((int)apiScsResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

                var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

                FileContentHelper.CheckCatalogueFileContent(Path.Combine(downloadedFolderPath, Config.ExchangeSetEncRootFolder, Config.ExchangeSetCatalogueFile), apiScsResponseData);
            }
        }

        //PBI 143370: Change related to additional param (From Boolean to String)
        //PBI 140672: ESS : Fulfilment service(webjob) - Get data from new BU
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        [Ignore("rhz Test disabled")]
        public void WhenICallEssApisWithS63ExchangeSetStandardParameter_ThenASerialEncFileIsGenerated()
        {
            foreach (var downloadedFolderPath in downloadedFolderPathList)
            {
                var checkFile = FssBatchHelper.CheckforFileExist(downloadedFolderPath, Config.ExchangeSetSerialEncFile);
                Assert.That(checkFile, Is.True, $"{Config.ExchangeSetSerialEncFile} File not Exist in the specified folder path : {downloadedFolderPath}");

                //Verify Serial.Enc file content
                FileContentHelper.CheckSerialEncFileContent(Path.Combine(downloadedFolderPath, Config.ExchangeSetSerialEncFile));
            }
        }

        //PBI 143370: Change related to additional param (From Boolean to String)
        //PBI 140672: ESS : Fulfilment service(webjob) - Get data from new BU
        [Test]
        [Category("QCOnlyTest-AIODisabled")]
        public async Task WhenICallEssApisWithS63ExchangeSetStandardParameter_ThenEncFilesAreDownloaded()
        {
            foreach (var downloadedFolderPath in downloadedFolderPathList)
            {
                //Get the product details form sales catalog service
                var apiScsResponse = await ScsApiClient.GetProductIdentifiersAsync(Config.ExchangeSetProductType, DataHelper.GetProductIdentifiers(), ScsJwtToken);
                Assert.That((int)apiScsResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiScsResponse.StatusCode}, instead of the expected status 200.");

                var apiScsResponseData = await apiScsResponse.ReadAsTypeAsync<ScsProductResponseModel>();

                foreach (var product in apiScsResponseData.Products)
                {
                    var productName = product.ProductName;
                    var editionNumber = product.EditionNumber;

                    //Enc file download verification
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
                Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code {apiResponse.StatusCode}  is  returned for clean up batches, instead of the expected 200.");
            }
        }
    }
}
