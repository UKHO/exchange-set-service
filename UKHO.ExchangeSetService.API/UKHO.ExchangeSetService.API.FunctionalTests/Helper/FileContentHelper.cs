using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using System.Net.Http;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public static class FileContentHelper
    {
        private const string FileContent_avcs = "AVCS";
        private const string FileContent_base = "Base";
        private const string FileContent_dvd = "Media','DVD_SERVICE'";
        private static TestConfiguration Config = new TestConfiguration();
        private static FssApiClient FssApiClient = new FssApiClient();
        public static async Task<string> CreateExchangeSetFile(HttpResponseMessage apiEssResponse, string FssJwtToken)
        {
            Assert.AreEqual(200, (int)apiEssResponse.StatusCode, $"Incorrect status code is returned {apiEssResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseData = await apiEssResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseData.Links.ExchangeSetBatchStatusUri.Href;
            var batchId = batchStatusUrl.Split('/')[5];

            var finalBatchStatusUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/status";

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(finalBatchStatusUrl, FssJwtToken);
            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus} for url {batchStatusUrl}, instead of the expected status Committed.");

            var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{Config.ExchangeSetFileName}";

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);

            var downloadFolder = FssBatchHelper.RenameFolder(extractDownloadedFolder);
            var downloadFolderPath = Path.Combine(Path.GetTempPath(), downloadFolder);

            return downloadFolderPath;
        }

        public static void CheckSerialEncFileContent(string inputFile)
        {
            string[] lines = File.ReadAllLines(inputFile);

            //Store file content here
            string[] fileContent = lines[0].Split(" ");

            string dataServerAndWeek = fileContent[0];
            string dateAndCdType = fileContent[3];
            string formatVersionAndExchangeSetNumber = fileContent[7];

            string weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday).ToString().PadLeft(2, '0');
            string year = DateTime.UtcNow.Year.ToString().Substring(DateTime.UtcNow.Year.ToString().Length - 2);
            string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");

            Assert.AreEqual(dataServerAndWeek, $"GBWK{weekNumber}-{year}", $"Incorrect weeknumber and year is returned 'GBWK{weekNumber}-{year}', instead of the expected {dataServerAndWeek}.");
            Assert.AreEqual(dateAndCdType, $"{currentDate}UPDATE", $"Incorrect date is returned '{currentDate}UPDATE', instead of the expected {dateAndCdType}.");
            Assert.IsTrue(formatVersionAndExchangeSetNumber.StartsWith("02.00U01X01"), $"Expected format version {formatVersionAndExchangeSetNumber}");
        }

        public static void CheckProductFileContent(string inputFile, dynamic scsResponse)
        {
            string[] fileContent = File.ReadAllLines(inputFile);

            string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");

            Assert.True(fileContent[0].Contains(currentDate), $"Product File returned {fileContent[0]}, which does not contain expected {currentDate}");
            Assert.True(fileContent[1].Contains("VERSION"), $"Product File returned {fileContent[1]}, which does not contain expected VERSION.");
            Assert.True(fileContent[3].Contains("ENC"), $"Product File returned {fileContent[3]}, which does not contain expected ENC.");
        }

        public static void CheckReadMeTxtFileContent(string inputFile)
        {

            string[] lines = File.ReadAllLines(inputFile);
            var fileSecondLineContent = lines[1];

            string[] fileContents = fileSecondLineContent.Split("File date:");

            //Verifying file contents - second line of the readme file
            Assert.True(fileSecondLineContent.Contains(fileContents[0]), $"{fileSecondLineContent} does not contain the expected {fileContents[0]}.");

            var utcDateTime = fileContents[1].Remove(fileContents[1].Length - 1);

            Assert.True(DateTime.Parse(utcDateTime) <= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second), $"Response body returned ExpiryDateTime {utcDateTime} , greater than the expected value.");
        }

        public static async Task CheckDownloadedEncFilesAsync(string fssBaseUrl, string folderPath, string productName, int? editionNumber, string accessToken)
        {
            //Get Countrycode
            string countryCode = productName.Substring(0, 2);

            //Get folder path
            string editionFolderPath = Path.Combine(folderPath, countryCode, productName, editionNumber.ToString());

            //Get list of directories
            List<string> listUpdateNumberPath = GetDirectories(editionFolderPath, "*");

            for (int counter = 0; counter < listUpdateNumberPath.Count; counter++)
            {
                string updateNumber = new DirectoryInfo(listUpdateNumberPath[counter]).Name;
                int totalFileCount = FileCountInDirectories(listUpdateNumberPath[counter]);
                string[] fileNames = Directory.GetFiles(listUpdateNumberPath[counter]).Select(file => Path.GetFileName(file)).ToArray();

                var searchQueryString = CreateFssSearchQuery(productName, editionNumber.ToString(), updateNumber);

                var apiResponse = await FssApiClient.SearchBatchesAsync(fssBaseUrl, searchQueryString, 100, 0, accessToken);
                Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

                //Batch Search response
                var responseSearchDetails = await apiResponse.ReadAsTypeAsync<ResponseBatchSearchModel>();
                int fssFileCount = responseSearchDetails.Entries[0].Files.Count;

                Assert.AreEqual(totalFileCount, fssFileCount, $"Downloaded Enc files count is {totalFileCount}, Instead of expected count {fssFileCount}");

                foreach (var fileName in fileNames)
                {
                    Assert.IsTrue(responseSearchDetails.Entries[0].Files.Any(fn => fn.Filename.Contains(fileName)), $"The expected file name {fileName} does not exist.");
                }
            }
        }

        public static void CheckNoEncFilesDownloadedAsync(string folderPath, string productName)
        {
            //Get Countrycode
            string countryCode = productName.Substring(0, 2);

            //Get list of directories
            List<string> listUpdateNumberPath = GetDirectories(folderPath, countryCode);
            int folderCount = listUpdateNumberPath.Count;

            Assert.AreEqual(0, folderCount, $"Downloaded Enc folder count {folderCount}, Instead of expected count 0");
        }

        public static async Task GetDownloadedEncFilesAsync(string fssBaseUrl, string folderPath, string productName, int? editionNumber, int? updateNumber, string accessToken)
        {
            int totalFileCount = 0;
            //Get Countrycode
            string countryCode = productName.Substring(0, 2);

            //Get folder path
            string downloadedEncFolderPath = Path.Combine(folderPath, countryCode, productName, editionNumber.ToString(), updateNumber.ToString());

            var searchQueryString = CreateFssSearchQuery(productName, editionNumber.ToString(), updateNumber.ToString());

            var apiResponse = await FssApiClient.SearchBatchesAsync(fssBaseUrl, searchQueryString, 100, 0, accessToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var responseSearchDetails = await apiResponse.ReadAsTypeAsync<ResponseBatchSearchModel>();
            if (Directory.Exists(downloadedEncFolderPath) && responseSearchDetails.Entries.Count > 0)
            {
                totalFileCount = FileCountInDirectories(downloadedEncFolderPath);
                string[] fileNames = Directory.GetFiles(downloadedEncFolderPath).Select(file => Path.GetFileName(file)).ToArray();
                int fssFileCount = 0;
                ResponseBatchDetailsModel responseBatchDetailsModel = new ResponseBatchDetailsModel();
                ExtractEncFileCount(productName, editionNumber, updateNumber, responseSearchDetails, ref fssFileCount, ref responseBatchDetailsModel);
                Assert.AreEqual(totalFileCount, fssFileCount, $"Downloaded Enc files count {totalFileCount}, Instead of expected count {fssFileCount}");

                foreach (var fileName in fileNames)
                {
                    Assert.IsTrue(responseBatchDetailsModel.Files.Any(fn => fn.Filename.Contains(fileName)), $"The expected file name {fileName} does not exist.");
                }
            }
            else
            {
                Assert.AreEqual(totalFileCount, responseSearchDetails.Count, $"Downloaded Enc files count {responseSearchDetails.Count}, Instead of expected count {totalFileCount}");
            }
        }

        private static void ExtractEncFileCount(string productName, int? editionNumber, int? updateNumber, ResponseBatchSearchModel responseSearchDetails, ref int fssFileCount, ref ResponseBatchDetailsModel responseBatchDetailsModel)
        {
            foreach (var item in responseSearchDetails.Entries)
            {
                if (fssFileCount == 0 && CheckProductDoesExistInSearchResponse(item, productName, editionNumber.ToString(), updateNumber.ToString()))
                {
                    fssFileCount = item.Files.Count;
                    responseBatchDetailsModel = item;
                    break;
                }
            }
        }

        public static bool CheckProductDoesExistInSearchResponse(ResponseBatchDetailsModel batchDetail, string productName, string editionNumber, string updateNumber)
        {
            return batchDetail.Attributes.Any(a => a.Key == "CellName" && a.Value == productName) &&
                batchDetail.Attributes.Any(a => a.Key == "EditionNumber" && a.Value == editionNumber) &&
                batchDetail.Attributes.Any(a => a.Key == "UpdateNumber" && a.Value == updateNumber);
        }

        public static string CreateFssSearchQuery(string productName, string editionNumber, string updateNumber)
        {
            string searchQuery = $"$batch(ProductCode) eq 'AVCS' and $batch(cellname) eq '{productName}' and $batch(editionnumber) eq '{editionNumber}' and $batch(updatenumber) eq '{updateNumber}'";
            return searchQuery;
        }

        public static List<string> GetDirectories(string path, string searchPattern)
        {
            return Directory.GetDirectories(path, searchPattern).ToList();
        }

        public static int FileCountInDirectories(string path)
        {
            return Directory.GetFiles(path).Length;
        }

        public static void CheckCatalogueFileContent(string inputFile, ScsProductResponseModel scsResponse)
        {
            List<string> scsCatalogueFilesPath = new List<string>();
            string catalogueFileContent = File.ReadAllText(inputFile);

            foreach (var item in scsResponse.Products)
            {
                string productName = item.ProductName;
                string editionNumber = item.EditionNumber.ToString();
                //Get Countrycode
                string countryCode = productName.Substring(0, 2);

                //Get folder path
                string editionFolderPath = Path.Combine(Path.GetDirectoryName(inputFile), countryCode, productName, editionNumber);

                foreach (var updateNumber in item.UpdateNumbers)
                {
                    if (Directory.Exists(Path.Combine(editionFolderPath, updateNumber.ToString())))
                    {
                        scsCatalogueFilesPath.Add(productName + "\\" + editionNumber + "\\" + updateNumber.ToString());
                    }
                }
            }

            foreach (var catalogueFilePath in scsCatalogueFilesPath)
            {
                Assert.True(catalogueFileContent.Contains(catalogueFilePath), $"{catalogueFileContent} does not contain {catalogueFilePath}.");
            }
        }

        public static void CheckCatalogueFileNoContent(string inputFile, List<ProductVersionModel> ProductVersionData)
        {
            string catalogueFileContent = File.ReadAllText(inputFile);
            foreach (var product in ProductVersionData)
            {
                Assert.False(catalogueFileContent.Contains(product.ProductName), $"{catalogueFileContent} contains {product.ProductName}, which is incorrect.");
            }
        }

        public static void DeleteDirectory(string fileName)
        {
            string path = Path.GetTempPath();

            if (Directory.Exists(path) && File.Exists(Path.Combine(path, fileName)))
            {
                string folder = Path.GetFileName(Path.Combine(path, fileName));
                if (folder.Contains(".zip"))
                {
                    folder = folder.Replace(".zip", "");
                }

                //Delete V01XO1/M01XO2/M02XO2 Directory and sub directories from temp Directory
                Directory.Delete(Path.Combine(path, folder), true);

                //Delete V01X01.zip/M01XO2.zip/M02XO2.zip file from temp Directory
                if (File.Exists(Path.Combine(path, fileName)))
                {
                    File.Delete(Path.Combine(path, fileName));
                }
            }

        }

        public static void CheckMediaTxtFileContent(string inputFile, int folderNumber)
        {
            string[] lines = File.ReadAllLines(inputFile);

            //Store file content for the 1st line of the Media.txt here
            string[] fileContent = lines[0].Split(" ");

            string dataServerAndWeek = fileContent[0];
            string dateAndCdType = fileContent[3];
            string formatVersionAndExchangeSetNumber = fileContent[9];

            ////string weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday).ToString().PadLeft(2, '0');
            ////string year = DateTime.UtcNow.Year.ToString().Substring(DateTime.UtcNow.Year.ToString().Length - 2);
            ////string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");
            ////Below static values are used temporarily as respective functionality will be developed and deployed in future sprints. Once that's implemented the above code will be used
            string weekNumber = "25";
            string year = "22";
            string currentDate = "20220623";

            Assert.AreEqual(dataServerAndWeek, $"GBWK{weekNumber}_{year}", $"Incorrect weeknumber and year is returned 'GBWK{weekNumber}-{year}', instead of the expected {dataServerAndWeek}.");
            Assert.AreEqual(dateAndCdType, $"{currentDate}BASE", $"Incorrect date is returned '{currentDate}UPDATE', instead of the expected {dateAndCdType}.");
            Assert.AreEqual(formatVersionAndExchangeSetNumber, $"M0{folderNumber}X02", $"Expected format {formatVersionAndExchangeSetNumber}");

            //Store file content for the 2nd line of the Media.txt here
            string[] fileContent_1 = lines[1].Split(" ");
            string FolderInitial = fileContent_1[0];
            string Avcs = fileContent_1[1];
            string WeekNumber_Year = fileContent_1[2];
            string baseContent = fileContent_1[3];
            string dvd_service = fileContent_1[4];

            Assert.AreEqual(FolderInitial, $"M{folderNumber},'UKHO", $"Incorrect FolderInitial is returned '{FolderInitial}'.");
            Assert.AreEqual(Avcs, FileContent_avcs, $"Incorrect file content is returned 'M{Avcs}'.");
            Assert.AreEqual(WeekNumber_Year, $"Week{weekNumber}_{year}", $"Incorrect weeknumber and year is returned 'GBWK{weekNumber}-{year}', instead of the expected {dataServerAndWeek}.");
            Assert.AreEqual(baseContent, FileContent_base, $"Incorrect file content is returned 'M{baseContent}'.");
            Assert.AreEqual(dvd_service, FileContent_dvd, $"Incorrect file content is returned 'M{dvd_service}'.");
        }

        public static void CheckReadMeTxtFileContentForLargeMediaExchangeSet(string inputFile)
        {
            string[] lines = File.ReadAllLines(inputFile);
            var fileSecondLineContent = lines[1];

            string[] fileContents = fileSecondLineContent.Split("File date:");

            //Verifying file contents - second line of the readme file
            Assert.True(fileSecondLineContent.Contains(fileContents[0]), $"{fileSecondLineContent} does not contain the expected {fileContents[0]}.");

            var utcDateTime = fileContents[1].Remove(fileContents[1].Length - 1);
            var expectedUtcDateTime = "2022-06-17 15:00:00";

            Assert.AreEqual(DateTime.Parse(expectedUtcDateTime), DateTime.Parse(utcDateTime), $"Response body returned ExpiryDateTime {utcDateTime}, different than the expected value.");
        }

        public static void CheckSerialEncFileContentForLargeMediaExchangeSet(string inputFile, int baseNumber)
        {
            string[] lines = File.ReadAllLines(inputFile);

            //Store file content here
            string[] fileContent = lines[0].Split(" ");

            string dataServerAndWeek = fileContent[0];
            string dateAndCdType = fileContent[3];
            string formatVersionAndExchangeSetNumber = fileContent[9];

            ////string weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday).ToString().PadLeft(2, '0');
            ////string year = DateTime.UtcNow.Year.ToString().Substring(DateTime.UtcNow.Year.ToString().Length - 2);
            ////string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");
            string weekNumber = "25";
            string year = "22";
            string currentDate = "20220623";

            Assert.AreEqual(dataServerAndWeek, $"GBWK{weekNumber}-{year}", $"Incorrect weeknumber and year is returned 'GBWK{weekNumber}-{year}', instead of the expected {dataServerAndWeek}.");
            Assert.AreEqual(dateAndCdType, $"{currentDate}BASE", $"Incorrect date is returned '{currentDate}UPDATE', instead of the expected {dateAndCdType}.");
            Assert.IsTrue(formatVersionAndExchangeSetNumber.StartsWith($"02.00B0{baseNumber}X09"), $"Expected format version {formatVersionAndExchangeSetNumber}");
        }

        public static async Task<List<string>> ExchangeSetLargeFile(HttpResponseMessage apiEssResponse, string FssJwtToken)
        {
            List<string> downloadFolderPath = new List<string>();
            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var FolderName = $"M0{mediaNumber}X02";
                Assert.AreEqual(200, (int)apiEssResponse.StatusCode, $"Incorrect status code is returned {apiEssResponse.StatusCode}, instead of the expected status 200.");

                var finalBatchStatusUrl = $"{Config.FssConfig.BaseUrl}/batch/62713adc-6999-40f6-86b1-ca08ab693d44/status"; //here BatchId is hardcoded and will be made dynamic in future

                var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(finalBatchStatusUrl, FssJwtToken);
                Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus} for url {finalBatchStatusUrl}, instead of the expected status Committed.");

                var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/62713adc-6999-40f6-86b1-ca08ab693d44/files/{FolderName}.zip"; //here BatchId is hardcoded and will be made dynamic in future

                var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolderForLargeFiles(downloadFileUrl, FssJwtToken, FolderName);

                var downloadFolder = FssBatchHelper.RenameFolder(extractDownloadedFolder);
                var downloadFolderPath1 = Path.Combine(Path.GetTempPath(), downloadFolder);
                downloadFolderPath.Add(downloadFolderPath1);
            }

            return downloadFolderPath;
        }

        public static void CheckProductFileContentLargeFile(string inputFile)
        {
            string[] fileContent = File.ReadAllLines(inputFile);

            ////string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");

            string currentDate = "20220623"; //// The date has been hardcoded as we are using a static batch id. This line will be removed in the future.
            Assert.True(fileContent[0].Contains(currentDate), $"Product File returned {fileContent[0]}, which does not contain expected {currentDate}");
            Assert.True(fileContent[1].Contains("VERSION"), $"Product File returned {fileContent[1]}, which does not contain expected VERSION.");
            Assert.True(fileContent[3].Contains("ENC"), $"Product File returned {fileContent[3]}, which does not contain expected ENC.");
        }
    }
}