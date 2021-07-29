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
        private static FssApiClient FssApiClient = new FssApiClient();


        public static async Task<string> CreateExchangeSetFile(HttpResponseMessage apiEssResponse, string FssJwtToken)
        {
            Assert.AreEqual(200, (int)apiEssResponse.StatusCode, $"Incorrect status code is returned {apiEssResponse.StatusCode}, instead of the expected status 200.");

            var apiResponseData = await apiEssResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var batchStatusUrl = apiResponseData.Links.ExchangeSetBatchStatusUri.Href;

            var batchStatus = await FssBatchHelper.CheckBatchIsCommitted(batchStatusUrl.ToString(), FssJwtToken);
            Assert.AreEqual("Committed", batchStatus, $"Incorrect batch status is returned {batchStatus}, instead of the expected status Committed.");

            var downloadFileUrl = apiResponseData.Links.ExchangeSetFileUri.Href;

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl.ToString(), FssJwtToken);

            var downloadFolder = FssBatchHelper.RenameFolder(extractDownloadedFolder);
            var downloadFolderPath = Path.Combine(Path.GetTempPath(), downloadFolder);

            return downloadFolderPath;
        }


        public static void CheckSerialEncFileContent(string inputFile)
        {
            string[] lines = File.ReadAllLines(inputFile);

            //Store file content
            string[] fileContent = lines[0].Split(" ");

            string dataServerAndWeek = fileContent[0];
            string dateAndCdType = fileContent[3];
            string formatVersionAndExchangeSetNumber = fileContent[7];

            int weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday);
            string year = DateTime.UtcNow.Year.ToString().Substring(DateTime.UtcNow.Year.ToString().Length - 2);
            string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");

            Assert.AreEqual(dataServerAndWeek, $"GBWK{weekNumber}-{year}");

            Assert.AreEqual(dateAndCdType, $"{currentDate}UPDATE");

            Assert.IsTrue(formatVersionAndExchangeSetNumber.StartsWith("02.00U01X01"), $"Expected format version {formatVersionAndExchangeSetNumber}");

        }

        public static void CheckProductFileContent(string inputFile, dynamic scsResponse)
        {
            string[] contentFirstLine = null;
            string[] fileContent = File.ReadAllLines(inputFile);
            int lastIndex = fileContent.Length - 2;

            if(fileContent[4] == "")
            {
                contentFirstLine = fileContent[5].Split(',');
            }
            else
            {
                contentFirstLine = fileContent[4].Split(',');
            }
            string[] contentLastLine = fileContent[lastIndex].Split(',');
            int scsResponseLength = scsResponse.Count;
            string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");

            Assert.True(fileContent[0].Contains(currentDate));
            Assert.True(fileContent[1].Contains("VERSION"));
            Assert.True(fileContent[3].Contains("ENC"));
            //verfying first product details
            Assert.True(contentFirstLine[0].Contains(scsResponse[0].productName.ToString()));
            Assert.True(contentFirstLine[1].Contains(scsResponse[0].baseCellIssueDate.ToString("yyyyMMdd")));
            Assert.True(contentFirstLine[2].Equals(scsResponse[0].baseCellEditionNumber.ToString()));
            Assert.True(contentFirstLine[3].Contains(scsResponse[0].issueDateLatestUpdate.ToString("yyyyMMdd")));
            Assert.True(contentFirstLine[4].Equals(scsResponse[0].latestUpdateNumber.ToString()));
            Assert.True(contentFirstLine[5].Equals(scsResponse[0].fileSize.ToString()));
            Assert.True(contentFirstLine[6].Contains(scsResponse[0].cellLimitSouthernmostLatitude.ToString()));
            Assert.True(contentFirstLine[7].Contains(scsResponse[0].cellLimitWesternmostLatitude.ToString()));
            Assert.True(contentFirstLine[8].Contains(scsResponse[0].cellLimitNorthernmostLatitude.ToString()));
            Assert.True(contentFirstLine[9].Contains(scsResponse[0].cellLimitEasternmostLatitude.ToString()));
            //verfying last product details
            Assert.True(contentLastLine[0].Contains(scsResponse[scsResponseLength - 1].productName.ToString()));
            Assert.True(contentLastLine[1].Contains(scsResponse[scsResponseLength - 1].baseCellIssueDate.ToString("yyyyMMdd")));
            Assert.True(contentLastLine[2].Equals(scsResponse[scsResponseLength - 1].baseCellEditionNumber.ToString()));
            Assert.True(contentLastLine[3].Contains(scsResponse[scsResponseLength - 1].issueDateLatestUpdate.ToString("yyyyMMdd")));
            Assert.True(contentLastLine[4].Equals(scsResponse[scsResponseLength - 1].latestUpdateNumber.ToString()));
            Assert.True(contentLastLine[5].Equals(scsResponse[scsResponseLength - 1].fileSize.ToString()));
            Assert.True(contentLastLine[6].Contains(scsResponse[scsResponseLength - 1].cellLimitSouthernmostLatitude.ToString()));
            Assert.True(contentLastLine[7].Contains(scsResponse[scsResponseLength - 1].cellLimitWesternmostLatitude.ToString()));
            Assert.True(contentLastLine[8].Contains(scsResponse[scsResponseLength - 1].cellLimitNorthernmostLatitude.ToString()));
            Assert.True(contentLastLine[9].Contains(scsResponse[scsResponseLength - 1].cellLimitEasternmostLatitude.ToString()));
        }

        public static void CheckReadMeTxtFileContent(string inputFile)
        {

            string[] lines = File.ReadAllLines(inputFile);
            var fileSecondLineContent = lines[1];

            string[] fileContents = fileSecondLineContent.Split("File date:");

            //Verifying file contents - second line of the readme file
            Assert.True(fileSecondLineContent.Contains(fileContents[0]));

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

                Assert.AreEqual(totalFileCount, fssFileCount, $"Downloaded Enc files count {totalFileCount}, Instead of expected count {fssFileCount}");

                foreach (var fileName in fileNames)
                {
                    Assert.IsTrue(responseSearchDetails.Entries[0].Files.Any(fn => fn.Filename.Contains(fileName)));
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
            string downloadedEncFolderPath = Path.Combine(folderPath, countryCode, productName, editionNumber.ToString(),updateNumber.ToString());

            var searchQueryString = CreateFssSearchQuery(productName, editionNumber.ToString(), updateNumber.ToString());

            var apiResponse = await FssApiClient.SearchBatchesAsync(fssBaseUrl, searchQueryString, 100, 0, accessToken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var responseSearchDetails = await apiResponse.ReadAsTypeAsync<ResponseBatchSearchModel>();


            if (Directory.Exists(downloadedEncFolderPath) && responseSearchDetails.Entries.Count>0)
            {
                totalFileCount = FileCountInDirectories(downloadedEncFolderPath);
                string[] fileNames = Directory.GetFiles(downloadedEncFolderPath).Select(file => Path.GetFileName(file)).ToArray();
                int fssFileCount = responseSearchDetails.Entries[0].Files.Count;
                Assert.AreEqual(totalFileCount, fssFileCount, $"Downloaded Enc files count {totalFileCount}, Instead of expected count {fssFileCount}");

                foreach (var fileName in fileNames)
                {
                    Assert.IsTrue(responseSearchDetails.Entries[0].Files.Any(fn => fn.Filename.Contains(fileName)));
                }

            }
            else
            {
                Assert.AreEqual(totalFileCount, responseSearchDetails.Count, $"Downloaded Enc files count {responseSearchDetails.Count}, Instead of expected count {totalFileCount}");
            }        

            
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
                        scsCatalogueFilesPath.Add(Path.Combine(productName, editionNumber,updateNumber.ToString()));
                    }
                }
            }

            foreach(var catalogueFilePath in scsCatalogueFilesPath)
            {
                Assert.True(catalogueFileContent.Contains(catalogueFilePath));
            }
           
        }

        public static void CheckCatalogueFileNoContent(string inputFile, List<ProductVersionModel> ProductVersionData)
        {
            string catalogueFileContent = File.ReadAllText(inputFile);
            foreach (var product in ProductVersionData)
            {
                Assert.False(catalogueFileContent.Contains(product.ProductName));
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

                //Delete V01XO1 Directory and sub directories from temp Directory
                Directory.Delete(Path.Combine(path, folder),true);

                //Delete V01X01.zip file from temp Directory
                if (File.Exists(Path.Combine(path, fileName)))
                {
                    File.Delete(Path.Combine(path, fileName));
                }              

               
            }

        }

    }
}

