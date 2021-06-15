using NUnit.Framework;
using System;
using System.Globalization;
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
                await Task.Delay(5000);
                if (File.Exists(fullPath))
                {
                    fileExistCheck = true;
                    break;
                }
            }


            if (fileExistCheck)
            {
                string[] lines = File.ReadAllLines(fullPath);
                var weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);
                var currentDate = DateTime.Now.ToString("dd-MM-yyyy");
                var year = currentDate.Substring((currentDate.Length - 2), 2);

                var secondLine = $"Version: Published Week {weekNumber}/{year} dated {currentDate}";
                Assert.AreEqual(secondLine, lines[1]);
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
