using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public static class FileContentHelper
    {
        private static FssApiClient FssApiClient = new FssApiClient();

        public static void CheckSerialEncFileContent(string inputfile)
        {
            string[] lines = File.ReadAllLines(inputfile);

            //Store file content
            string[] fileContent = lines[0].Split(" ");

            string dataServerAndWeek = fileContent[0];
            string dateAndCdType = fileContent[3];
            string formatVersionAndExchangeSetNumber = fileContent[7];

            int weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            string year = DateTime.UtcNow.Year.ToString().Substring(DateTime.UtcNow.Year.ToString().Length - 2);
            string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");

            Assert.AreEqual(dataServerAndWeek, $"GBWK{weekNumber}-{year}");

            Assert.AreEqual(dateAndCdType, $"{currentDate}UPDATE");

            Assert.IsTrue(formatVersionAndExchangeSetNumber.StartsWith("02.00U01X01"), $"Expected format version {formatVersionAndExchangeSetNumber}");

        }

        public static void CheckProductFileContent(string inputfile, dynamic scsResponse,string ScsJwtToken)
        {
            string[] fileContent = File.ReadAllLines(inputfile);
            int lastIndex = fileContent.Length-2;
            string[] contentFirstLine = fileContent[4].Split(',');
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
            Assert.True(contentLastLine[0].Contains(scsResponse[scsResponseLength-1].productName.ToString()));
            Assert.True(contentFirstLine[1].Contains(scsResponse[scsResponseLength - 1].baseCellIssueDate.ToString("yyyyMMdd")));
            Assert.True(contentFirstLine[2].Equals(scsResponse[scsResponseLength - 1].baseCellEditionNumber.ToString()));
            Assert.True(contentFirstLine[3].Contains(scsResponse[scsResponseLength - 1].issueDateLatestUpdate.ToString("yyyyMMdd")));
            Assert.True(contentFirstLine[4].Equals(scsResponse[scsResponseLength - 1].latestUpdateNumber.ToString()));
            Assert.True(contentFirstLine[5].Equals(scsResponse[scsResponseLength - 1].fileSize.ToString()));
            Assert.True(contentFirstLine[6].Contains(scsResponse[scsResponseLength - 1].cellLimitSouthernmostLatitude.ToString()));
            Assert.True(contentFirstLine[7].Contains(scsResponse[scsResponseLength - 1].cellLimitWesternmostLatitude.ToString()));
            Assert.True(contentFirstLine[8].Contains(scsResponse[scsResponseLength - 1].cellLimitNorthernmostLatitude.ToString()));
            Assert.True(contentFirstLine[9].Contains(scsResponse[scsResponseLength - 1].cellLimitEasternmostLatitude.ToString()));
        }

        public static void CheckReadMeTxtFileContent(string inputfile)
        {

            string[] lines = File.ReadAllLines(inputfile);
            var fileSecondLineContent = lines[1];

            string[] fileContents = fileSecondLineContent.Split("File date:");

            //Verifying file contents - second line of the readme file
            Assert.True(fileSecondLineContent.Contains(fileContents[0]));

            var utcDateTime = fileContents[1].Remove(fileContents[1].Length - 1);

            Assert.True(DateTime.Parse(utcDateTime) <= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second), $"Response body returned ExpiryDateTime {utcDateTime} , greater than the expected value.");
        }

        public static async Task CheckDownloadedEncFilesAsync(string fssbaseurl,string folderpath, string productname,int? editionnumber,string accesstoken)
        {
           
            //Get Countrycode
            string countryCode = productname.Substring(0, 2);

            //Get folder path
            string editionFolderPath = Path.Combine(folderpath, countryCode, productname, editionnumber.ToString());

            //Get list of directories
            List<string> listUpdateNumberPath = GetDirectories(editionFolderPath, "*");

            for(int counter=0; counter<listUpdateNumberPath.Count; counter++)
            {
                string updatenumber= new DirectoryInfo(listUpdateNumberPath[counter]).Name;
                int totalFileCount = FileCountInDirectories(listUpdateNumberPath[counter]);
                string[] fileNames = Directory.GetFiles(listUpdateNumberPath[counter]).Select(file => Path.GetFileName(file)).ToArray();
                
                var searchQueryString = CreateFssSearchQuery(productname, editionnumber.ToString(), updatenumber);

                var apiResponse = await FssApiClient.SearchBatchesAsync(fssbaseurl, searchQueryString, 100, 0, accesstoken);
                Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

                //Batch Search response
                var responseSearchDetails = await apiResponse.ReadAsTypeAsync<ResponseBatchSearchModel>();
                int fssFileCount = responseSearchDetails.Entries[0].Files.Count;

                Assert.AreEqual(totalFileCount, fssFileCount, $"Downloaded Enc files count {totalFileCount}, Instead of expected count {fssFileCount}");

                foreach(var filenanme in fileNames)
                {
                    Assert.IsTrue(responseSearchDetails.Entries[0].Files.Any(fn => fn.Filename.Contains(filenanme)));
                }                

            }
        }

        public static async Task CheckNoEncFilesDownloadedAsync(string fssbaseurl, string folderpath, string productname, int? editionnumber, int? updatenumber, string accesstoken)
        {
            //Get Countrycode
            string countryCode = productname.Substring(0, 2);
           
            //Get list of directories
            List<string> listUpdateNumberPath = GetDirectories(folderpath, countryCode);
            int folderCount = listUpdateNumberPath.Count;

            var searchQueryString = CreateFssSearchQuery(productname, editionnumber.ToString(), updatenumber.ToString());

            var apiResponse = await FssApiClient.SearchBatchesAsync(fssbaseurl, searchQueryString, 100, 0, accesstoken);
            Assert.AreEqual(200, (int)apiResponse.StatusCode, $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            //Batch Search response
            var responseSearchDetails = await apiResponse.ReadAsTypeAsync<ResponseBatchSearchModel>();

            Assert.AreEqual(responseSearchDetails.Entries.Count, folderCount, $"Downloaded Enc folder count {folderCount}, Instead of expected count {responseSearchDetails.Entries.Count}");

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

    }
}

