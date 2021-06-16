using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public static class FolderCheck
    {
        private static TestConfiguration Config { get; set; }
        private static bool folderExistCheck = false;
        private static int FilesCount { get; set; }

        static FolderCheck()
        {
            Config = new TestConfiguration();
        }

        /// <summary>
        /// Checks if Downloaded folder exists and return files count
        /// </summary>
        /// <param name="folderPath">Folder path</param>
        /// <returns></returns>
        public static async Task<int> CheckIfDownloadFolderExistAndFileCount(string folderPath)
        {

            //Added step to wait for file exist in specific folder
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < TimeSpan.FromMinutes(Config.FileDownloadWaitTime))
            {
                await Task.Delay(5000);
                if (File.Exists(folderPath))
                {
                    folderExistCheck = true;
                    break;
                }
            }

            // if folder exists
            if (folderExistCheck)
            {
                // to get the file count in a folder
                FilesCount = Directory.GetFiles(folderPath).Length;
            }
            else
            {
                FilesCount = 0;
            }

            return FilesCount;

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

