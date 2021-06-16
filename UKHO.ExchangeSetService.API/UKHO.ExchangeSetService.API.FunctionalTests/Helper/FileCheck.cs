using NUnit.Framework;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public static class FileCheck
    {
        private static TestConfiguration Config { get; set; }
        private static bool fileContentCheck = false;
        private static bool fileExistCheck = false;

        static FileCheck()
        {
            Config = new TestConfiguration();
        }

        /// <summary>
        /// Checks if Directory contains the README File
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="readMeFileName">Read me File name</param>
        /// <returns></returns>
        public static async Task<bool> CheckIfFileExistAndVerify(string filePath, string readMeFileName)
        {
            var fullPath = filePath + @"\" + readMeFileName;

            //Added step to wait for file exist in specific folder
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < TimeSpan.FromMinutes(Config.FileDownloadWaitTime))
            {
                await Task.Delay(2000);
                if (File.Exists(fullPath))
                {
                    fileExistCheck = true;
                    break;
                }
            }


            if (fileExistCheck)
            {
                string[] lines = File.ReadAllLines(fullPath);
                var fileSecondLineContent = lines[1];

                string[] fileContents = fileSecondLineContent.Split("File date:");

                //Verifying file contents - second line of the readme file
                Assert.True(fileSecondLineContent.Contains(fileContents[0]),$"ReadMe file does not contains");

                var utcDateTime = fileContents[1].Remove(fileContents[1].Length - 1);

                Assert.True(DateTime.Parse(utcDateTime) <= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second), $"Response body returned ExpiryDateTime {utcDateTime} , greater than the expected value.");

                fileContentCheck = true;
            }
            else
            {
                fileContentCheck = false;
            }

            return fileContentCheck;

        }

        public static async Task<string> GetBatchId(this HttpResponseMessage apiresponse)
        {
            var apiResponseData = await apiresponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            string[] exchangeSetBatchStatusUri = apiResponseData.Links.ExchangeSetBatchStatusUri.Href.Split('/');
            var batchID = exchangeSetBatchStatusUri[exchangeSetBatchStatusUri.Length - 1];
            return batchID;
        }
    }
}
