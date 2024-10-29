using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Azure;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public static class FileContentHelper
    {
        private const string FileContent_avcs = "AVCS";
        private const string FileContent_base = "Base";
        private const string FileContent_dvd = "Media','DVD_SERVICE'";
        private static TestConfiguration Config = new();
        private static FssApiClient FssApiClient = new();
        public static async Task<string> CreateExchangeSetFile(HttpResponseMessage apiEssResponse, string FssJwtToken)
        {
            Assert.That((int)apiEssResponse.StatusCode,Is.EqualTo(200), $"Incorrect status code is returned {apiEssResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseData = await apiEssResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            // rhz debug start
            Console.WriteLine("Rhz Status check");
            string apiScsResponseDataJson = JsonConvert.SerializeObject(apiResponseData, Formatting.Indented);
            Console.WriteLine("State of ExchangeSetResponseModel: " + apiScsResponseDataJson);
            // rhz debug end

            var batchStatusUrl = apiResponseData.Links.ExchangeSetBatchStatusUri.Href;
            var batchId = batchStatusUrl.Split('/')[5];

            var finalBatchStatusUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/status";

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(finalBatchStatusUrl, FssJwtToken);
            Assert.That( batchStatus,Is.EqualTo("Committed"), $"Incorrect batch status is returned {batchStatus} for url {batchStatusUrl}, instead of the expected status Committed.");

            var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{Config.ExchangeSetFileName}";

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);

            return extractDownloadedFolder;
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

            Assert.That($"GBWK{weekNumber}-{year}", Is.EqualTo(dataServerAndWeek), $"Incorrect weeknumber and year is returned 'GBWK{weekNumber}-{year}', instead of the expected {dataServerAndWeek}.");
            Assert.That($"{currentDate}UPDATE", Is.EqualTo(dateAndCdType), $"Incorrect date is returned '{currentDate}UPDATE', instead of the expected {dateAndCdType}.");
            Assert.That(formatVersionAndExchangeSetNumber.StartsWith("02.00U01X01"),Is.True, $"Expected format version {formatVersionAndExchangeSetNumber}");
        }

        public static void CheckProductFileContent(string inputFile, dynamic scsResponse, string exchangeSetStandard = "s63")
        {
            string[] fileContent = File.ReadAllLines(inputFile);

            string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");

            Assert.That(fileContent[0].Contains(currentDate),Is.True, $"Product File returned {fileContent[0]}, which does not contain expected {currentDate}");
            Assert.That(fileContent[1].Contains("VERSION"), Is.True, $"Product File returned {fileContent[1]}, which does not contain expected VERSION.");
            Assert.That(fileContent[3].Contains("ENC"), Is.True, $"Product File returned {fileContent[3]}, which does not contain expected ENC.");
          
            int rowNumber = new Random().Next(4, fileContent.Length-1);
            var productData = fileContent[rowNumber].Split(",").Reverse();
            var encryptionFlag = productData.ToList()[4];
            string expectedEncryptionFlag = "1";
            if (exchangeSetStandard == "s57") 
                { 
                    expectedEncryptionFlag = "0"; 
                }

            Assert.That(encryptionFlag.Equals(expectedEncryptionFlag), Is.True, $"Product File returned {encryptionFlag}, which is not expected encryptionFlag.");
        }

        public static void CheckReadMeTxtFileContent(string inputFile)
        {

            string[] lines = File.ReadAllLines(inputFile);
            var fileSecondLineContent = lines[1];

            string[] fileContents = fileSecondLineContent.Split("File date:");

            //Verifying file contents - second line of the readme file
            Assert.That(fileSecondLineContent.Contains(fileContents[0]), Is.True, $"{fileSecondLineContent} does not contain the expected {fileContents[0]}.");

            var utcDateTime = fileContents[1].Remove(fileContents[1].Length - 1);

            Assert.That(DateTime.Parse(utcDateTime) <= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second),Is.True, $"Response body returned ExpiryDateTime {utcDateTime} , greater than the expected value.");
        }

        public static void CheckNoEncFilesDownloadedAsync(string folderPath, string productName)
        {
            //Get Countrycode
            string countryCode = productName.Substring(0, 2);

            //Get list of directories
            List<string> listUpdateNumberPath = GetDirectories(folderPath, countryCode);
            int folderCount = listUpdateNumberPath.Count;

            Assert.That(folderCount, Is.EqualTo(0), $"Downloaded Enc folder count {folderCount}, Instead of expected count 0");
        }

        public static async Task GetDownloadedEncFilesAsync(string fssBaseUrl, string folderPath, string productName, int? editionNumber, int? updateNumber, string accessToken, string businessUnit = "ADDS")
        {
            int totalFileCount = 0;
            //Get Countrycode
            string countryCode = productName.Substring(0, 2);

            //Get folder path
            string downloadedEncFolderPath = Path.Combine(folderPath, countryCode, productName, editionNumber.ToString(), updateNumber.ToString());

            var searchQueryString = CreateFssSearchQuery(businessUnit, productName, editionNumber.ToString(), updateNumber.ToString());

            var apiResponse = await FssApiClient.SearchBatchesAsync(fssBaseUrl, searchQueryString, 100, 0, accessToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var responseSearchDetails = await apiResponse.ReadAsTypeAsync<ResponseBatchSearchModel>();
            if (Directory.Exists(downloadedEncFolderPath) && responseSearchDetails.Entries.Count > 0)
            {
                totalFileCount = FileCountInDirectories(downloadedEncFolderPath);
                string[] fileNames = Directory.GetFiles(downloadedEncFolderPath).Select(file => Path.GetFileName(file)).ToArray();
                int fssFileCount = 0;
                ResponseBatchDetailsModel responseBatchDetailsModel = new ResponseBatchDetailsModel();
                ExtractEncFileCount(productName, editionNumber, updateNumber, responseSearchDetails, ref fssFileCount, ref responseBatchDetailsModel);
                Assert.That(fssFileCount, Is.EqualTo(totalFileCount), $"Downloaded Enc files count {totalFileCount}, Instead of expected count {fssFileCount}");

                foreach (var fileName in fileNames)
                {
                    Assert.That(responseBatchDetailsModel.Files.Any(fn => fn.Filename.Contains(fileName)), Is.True, $"The expected file name {fileName} does not exist.");
                }
            }
            else
            {
                Assert.That(responseSearchDetails.Count, Is.EqualTo(totalFileCount), $"Downloaded Enc files count {responseSearchDetails.Count}, Instead of expected count {totalFileCount}");
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

        public static string CreateFssSearchQuery(string businessUnit, string productName, string editionNumber, string updateNumber)
        {
            string searchQuery = $"businessUnit eq '{businessUnit}' and $batch(ProductCode) eq 'AVCS' and $batch(cellname) eq '{productName}' and $batch(editionnumber) eq '{editionNumber}' and $batch(updatenumber) eq '{updateNumber}'";
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
                Assert.That(catalogueFileContent.Contains(catalogueFilePath), Is.True, $"{catalogueFileContent} does not contain {catalogueFilePath}.");
            }
        }

        public static void CheckCatalogueFileNoContent(string inputFile, List<ProductVersionModel> ProductVersionData)
        {
            string catalogueFileContent = File.ReadAllText(inputFile);
            foreach (var product in ProductVersionData)
            {
                Assert.That(catalogueFileContent.Contains(product.ProductName), Is.False, $"{catalogueFileContent} contains {product.ProductName}, which is incorrect.");
            }
        }

        public static void DeleteDirectory(string fileName)
        {
            string path = Path.GetTempPath();
            if(Directory.Exists(Path.Combine(path, Config.BESSConfig.TempFolderName)))
            {
                Directory.Delete(Path.Combine(path, Config.BESSConfig.TempFolderName), true);
            }

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

            string weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday).ToString().PadLeft(2, '0');
            string year = DateTime.UtcNow.ToString("yy");
            string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");

            Assert.That($"GBWK{weekNumber}_{year}", Is.EqualTo(dataServerAndWeek), $"Incorrect weeknumber and year is returned 'GBWK{weekNumber}-{year}', instead of the expected {dataServerAndWeek}.");
            Assert.That($"{currentDate}BASE", Is.EqualTo(dateAndCdType), $"Incorrect date is returned '{currentDate}UPDATE', instead of the expected {dateAndCdType}.");
            Assert.That($"M0{folderNumber}X02",Is.EqualTo(formatVersionAndExchangeSetNumber), $"Expected format {formatVersionAndExchangeSetNumber}");

            //Store file content for the 2nd line of the Media.txt here
            string[] fileContent_1 = lines[1].Split(" ");
            string FolderInitial = fileContent_1[0];
            string Avcs = fileContent_1[1];
            string WeekNumber_Year = fileContent_1[2];
            string baseContent = fileContent_1[3];
            string dvd_service = fileContent_1[4];

            Assert.That($"M{folderNumber},'UKHO", Is.EqualTo(FolderInitial), $"Incorrect FolderInitial is returned '{FolderInitial}'.");
            Assert.That(Avcs, Is.EqualTo(FileContent_avcs), $"Incorrect file content is returned 'M{Avcs}'.");
            Assert.That($"Week{weekNumber}_{year}", Is.EqualTo(WeekNumber_Year), $"Incorrect weeknumber and year is returned 'GBWK{weekNumber}-{year}', instead of the expected {dataServerAndWeek}.");
            Assert.That(baseContent, Is.EqualTo(FileContent_base), $"Incorrect file content is returned 'M{baseContent}'.");
            Assert.That(dvd_service, Is.EqualTo(FileContent_dvd), $"Incorrect file content is returned 'M{dvd_service}'.");

            //Verification of the lines describing folders and country code(s) of the Media.txt here
            string[] checkDirectories = FssBatchHelper.CheckforDirectories(Path.Combine(Path.GetTempPath(), $"M0{folderNumber}X02"));
            Array.Sort(checkDirectories);
            Array.Resize(ref checkDirectories, checkDirectories.Length - 1);
            List<string> countryCodes = new List<string>();

            int lineNumber = 2;
            foreach (string codes in checkDirectories)
            {
                string actualfileContent = lines[lineNumber];
                string baseFolderNumber = new DirectoryInfo(codes).Name;
                string count = baseFolderNumber.Substring(1, 1);
                string encRootFolder = Path.Combine(codes, Config.ExchangeSetEncRootFolder);
                string[] addDirectory = FssBatchHelper.CheckforDirectories(encRootFolder);

                foreach (string countryName in addDirectory)
                {
                    string dirName = new DirectoryInfo(countryName).Name;
                    countryCodes.Add(dirName);
                    countryCodes.Sort();
                }
                Assert.That(actualfileContent, Is.EqualTo($"M{folderNumber};{baseFolderNumber},{currentDate},'AVCS Volume{count}','ENC data for producers {string.Join(", ", countryCodes)}',,"));
                countryCodes.Clear();
                lineNumber++;
            }
        }

        public static void CheckSerialEncFileContentForLargeMediaExchangeSet(string inputFile, int baseNumber)
        {
            string[] lines = File.ReadAllLines(inputFile);

            //Store file content here
            string[] fileContent = lines[0].Split(" ");

            string dataServerAndWeek = fileContent[0];
            string dateAndCdType = fileContent[3];
            string formatVersionAndExchangeSetNumber = fileContent[9];

            string weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday).ToString().PadLeft(2, '0');
            string year = DateTime.UtcNow.ToString("yy");
            string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");

            Assert.That($"GBWK{weekNumber}-{year}", Is.EqualTo(dataServerAndWeek), $"Incorrect weeknumber and year is returned 'GBWK{weekNumber}-{year}', instead of the expected {dataServerAndWeek}.");
            Assert.That($"{currentDate}BASE", Is.EqualTo(dateAndCdType), $"Incorrect date is returned '{currentDate}UPDATE', instead of the expected {dateAndCdType}.");
            Assert.That(formatVersionAndExchangeSetNumber.StartsWith($"02.00B0{baseNumber}X09"),Is.True, $"Expected format version {formatVersionAndExchangeSetNumber}");
        }

        public static async Task<List<string>> CreateExchangeSetFileForLargeMedia(HttpResponseMessage apiEssResponse, string FssJwtToken)
        {
            List<string> downloadFolderPath = new List<string>();
            Assert.That((int)apiEssResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiEssResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseData = await apiEssResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseData.Links.ExchangeSetBatchStatusUri.Href;
            var batchId = batchStatusUrl.Split('/')[5];

            var finalBatchStatusUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/status";

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(finalBatchStatusUrl, FssJwtToken);
            Assert.That(batchStatus, Is.EqualTo("Committed"), $"Incorrect batch status is returned {batchStatus} for url {finalBatchStatusUrl}, instead of the expected status Committed.");

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var folderName = $"M0{mediaNumber}X02";
                var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{folderName}.zip";

                var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolderForLargeFiles(downloadFileUrl, FssJwtToken, folderName);

                var downloadFolder = FssBatchHelper.RenameFolder(extractDownloadedFolder);
                var downloadFolderPath1 = Path.Combine(Path.GetTempPath(), downloadFolder);
                downloadFolderPath.Add(downloadFolderPath1);
            }
            return downloadFolderPath;
        }

        public static void CheckProductFileContentLargeFile(string inputFile)
        {
            string[] fileContent = File.ReadAllLines(inputFile);
            string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");

            Assert.That(fileContent[0].Contains(currentDate), Is.True, $"Product File returned {fileContent[0]}, which does not contain expected {currentDate}");
            Assert.That(fileContent[1].Contains("VERSION"), Is.True, $"Product File returned {fileContent[1]}, which does not contain expected VERSION.");
            Assert.That(fileContent[3].Contains("ENC"), Is.True, $"Product File returned {fileContent[3]}, which does not contain expected ENC.");
        }

        public static void CheckCatalogueFileContentForLargeMedia(string inputFile, ScsProductResponseModel scsResponse)
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
                string editionFolderPath = Path.Combine(Path.GetDirectoryName(inputFile), countryCode, productName);

                if (Directory.Exists(Path.Combine(editionFolderPath, editionNumber.ToString())))
                {
                    scsCatalogueFilesPath.Add(productName + "\\" + editionNumber.ToString());
                }
            }

            foreach (var catalogueFilePath in scsCatalogueFilesPath)
            {
                Assert.That(catalogueFileContent.Contains(catalogueFilePath), Is.True, $"{catalogueFileContent} does not contain {catalogueFilePath}.");
            }
        }

        public static async Task<HttpResponseMessage> CreateErrorFileValidation(HttpResponseMessage apiEssResponse, string FssJwtToken)
        {
            Assert.That((int)apiEssResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiEssResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseData = await apiEssResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseData.Links.ExchangeSetBatchStatusUri.Href;
            var batchId = batchStatusUrl.Split('/')[5];

            var finalBatchStatusUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/status";

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(finalBatchStatusUrl, FssJwtToken);
            Assert.That(batchStatus, Is.EqualTo("Committed"), $"Incorrect batch status is returned {batchStatus} for url {batchStatusUrl}, instead of the expected status Committed.");

            var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{Config.POSConfig.ErrorFileName}";

            var response = await FssApiClient.GetFileDownloadAsync(downloadFileUrl, accessToken: FssJwtToken);

            return response;
        }

        public static async Task<string> DownloadAndExtractAioZip(HttpResponseMessage apiEssResponse, string FssJwtToken)
        {
            Assert.That((int)apiEssResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiEssResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseData = await apiEssResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchId = apiResponseData.BatchId;

            var finalBatchStatusUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/status";

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(finalBatchStatusUrl, FssJwtToken);
            Assert.That(batchStatus, Is.EqualTo("Committed"), $"Incorrect batch status is returned {batchStatus} for url {finalBatchStatusUrl}, instead of the expected status Committed.");

            var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{Config.AIOConfig.AioExchangeSetFileName}";

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedAioFolder(downloadFileUrl.ToString(), FssJwtToken);

            var downloadFolder = FssBatchHelper.RenameFolder(extractDownloadedFolder);
            return Path.Combine(Path.GetTempPath(), downloadFolder);

        }

        public static void CheckAioProductFileContent(string inputFile, dynamic scsResponse)
        {
            string[] fileContent = File.ReadAllLines(inputFile);

            string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");

            Assert.That(fileContent[0].Contains(currentDate), Is.True, $"Product File returned {fileContent[0]}, which does not contain expected {currentDate}");
            Assert.That(fileContent[1].Contains("VERSION"), Is.True, $"Product File returned {fileContent[1]}, which does not contain expected VERSION.");
            Assert.That(fileContent[3].Contains("ENC"), Is.True, $"Product File returned {fileContent[3]}, which does not contain expected ENC.");
            Assert.That(fileContent[4].Contains("GB800001"), Is.True, $"Product File returned {fileContent[4]}, which does not contain expected GB800001.");
        }

        public static void CheckSerialAioFileContentForAioBase(string inputFile)
        {
            string[] lines = File.ReadAllLines(inputFile);

            //Store file content here
            string[] fileContent = lines[0].Split(" ");

            string dataServerAndWeek = fileContent[0];
            string dateAndCdType = fileContent[3];
            string formatVersionAndExchangeSetNumber = fileContent[9];

            string weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday).ToString().PadLeft(2, '0');
            string year = DateTime.UtcNow.Year.ToString().Substring(DateTime.UtcNow.Year.ToString().Length - 2);
            string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");

            Assert.That($"GBWK{weekNumber}-{year}", Is.EqualTo(dataServerAndWeek), $"Incorrect weeknumber and year is returned 'GBWK{weekNumber}-{year}', instead of the expected {dataServerAndWeek}.");
            Assert.That($"{currentDate}BASE", Is.EqualTo(dateAndCdType), $"Incorrect date is returned '{currentDate}UPDATE', instead of the expected {dateAndCdType}.");
            Assert.That(formatVersionAndExchangeSetNumber.StartsWith("02.00"), Is.True, $"Expected format version {formatVersionAndExchangeSetNumber}");
        }

        public static void CheckSerialAioFileContentForAioUpdate(string inputFile)
        {
            string[] lines = File.ReadAllLines(inputFile);

            //Store file content here
            string[] fileContent = lines[0].Split(" ");

            string dataServerAndWeek = fileContent[0];
            string dateAndCdType = fileContent[3];
            string formatVersionAndExchangeSetNumber = fileContent[9];

            string weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday).ToString().PadLeft(2, '0');
            string year = DateTime.UtcNow.Year.ToString().Substring(DateTime.UtcNow.Year.ToString().Length - 2);
            string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");

            Assert.That($"GBWK{weekNumber}-{year}", Is.EqualTo(dataServerAndWeek), $"Incorrect weeknumber and year is returned 'GBWK{weekNumber}-{year}', instead of the expected {dataServerAndWeek}.");
            Assert.That($"{currentDate}UPDATE", Is.EqualTo(dateAndCdType), $"Incorrect date is returned '{currentDate}UPDATE', instead of the expected {dateAndCdType}.");
            Assert.That(formatVersionAndExchangeSetNumber.StartsWith("02.00"), Is.True, $"Expected format version {formatVersionAndExchangeSetNumber}");
        }

        public static async Task<bool> WaitForContainerAsync(BlobServiceClient blobServiceClient, string containerName, int maxAttempts, int delayInMilliSeconds)
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    await containerClient.GetPropertiesAsync();
                    return true;
                }
                catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.ContainerNotFound)
                {
                    await Task.Delay(delayInMilliSeconds);
                }
            }

            return false;
        }
    }
}
