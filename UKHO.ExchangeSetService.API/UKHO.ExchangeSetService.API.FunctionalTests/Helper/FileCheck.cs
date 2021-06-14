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
        /// <summary>
        /// Checks if Directory contains the README File
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="readMeFileName">Read me File name</param>
        /// <returns></returns>
        public static bool CheckIfFileExistAndVerify(string filePath, string readMeFileName)
        {
            bool fileCheck = false;
            var fullPath = filePath + @"\" + readMeFileName;
            if (File.Exists(fullPath))
            {
                string[] lines = File.ReadAllLines(fullPath);
                var secondLinecontent = lines[2];

                secondLinecontent.Split("File date:");
                var utcDateFormat =  secondLinecontent.Split("yyyy-mm-dd hh:mm:ssZ");

                var utcDateFormat = utcDateFormat.Remove(utcDateFormat.Length - 1, 1);



                var utcDate = DateTime.UtcNow.ToString("yyyy-mm-dd");


                var secondLine = $"";
                Assert.AreEqual(secondLine, lines[2]);
                fileCheck = true;
            }
            else
            {
                fileCheck = false;
            }

            return fileCheck;

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
