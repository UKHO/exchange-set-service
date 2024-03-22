using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using static UKHO.ExchangeSetService.API.FunctionalTests.Helper.TestConfiguration;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public static class FssBatchHelper
    {
        private static FssApiClient FssApiClient { get; set; }
        static FileShareService Config = new TestConfiguration().FssConfig;
        static BessConfiguration bessConfig = new TestConfiguration().BESSConfig;
        static TestConfiguration EssConfig { get; set; }

        static FssBatchHelper()
        {
            FssApiClient = new FssApiClient();
            EssConfig = new TestConfiguration();
        }

        public static async Task<string> CheckBatchIsCommitted(string batchStatusUri, string jwtToken)
        {
            string batchStatus = "";
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < TimeSpan.FromMinutes(Config.BatchCommitWaitTime))
            {
                await Task.Delay(5000);
                var batchStatusResponse = await FssApiClient.GetBatchStatusAsync(batchStatusUri, jwtToken);
                Console.WriteLine("CheckBatchIsCommitted method " + batchStatusResponse.StatusCode);
                Assert.AreEqual(200, (int)batchStatusResponse.StatusCode, $"Incorrect status code is returned {batchStatusResponse.StatusCode}, instead of the expected status 200 for url {batchStatusUri}.");

                var batchStatusResponseObj = JsonConvert.DeserializeObject<ResponseBatchStatusModel>(await batchStatusResponse.Content.ReadAsStringAsync());
                batchStatus = batchStatusResponseObj.Status;

                if (batchStatus.Equals("Committed"))
                    break;
            }

            return batchStatus;
        }

        public static async Task<string> ExtractDownloadedFolder(string downloadFileUrl, string jwtToken)
        {
            //Mock api fullfillment process takes more time to upload file for the cancellation product and tests are intermittently failing,therefore we have added delay 'Task.Delay()' to avoid intermittent failure in the pipe.
            await Task.Delay(40000);
            string batchId = downloadFileUrl.Split('/')[4];
            string fileName = downloadFileUrl.Split('/')[6];
            string tempFilePath = Path.Combine(Path.GetTempPath(), bessConfig.TempFolderName);
            if (!Directory.Exists(tempFilePath))
            {
                Directory.CreateDirectory(tempFilePath);
            }

            string batchFolderPath = Path.Combine(tempFilePath, batchId);
            if (!Directory.Exists(batchFolderPath))
            {
                Directory.CreateDirectory(batchFolderPath);
            }

            var response = await FssApiClient.GetFileDownloadAsync(downloadFileUrl, accessToken: jwtToken);
            Console.WriteLine("ExtractDownloadedFolder method==> "+ response.StatusCode);
            Assert.AreEqual(200, (int)response.StatusCode, $"Incorrect status code File Download api returned {response.StatusCode} for the url {downloadFileUrl}, instead of the expected 200.");

            Stream stream = await response.Content.ReadAsStreamAsync();

            using (FileStream outputFileStream = new(Path.Combine(batchFolderPath, fileName), FileMode.Create))
            {
                stream.CopyTo(outputFileStream);
            }

            string zipPath = Path.Combine(batchFolderPath, fileName);
            string extractPath = Path.Combine(batchFolderPath, RenameFolder(zipPath)); 

            ZipFile.ExtractToDirectory(zipPath, extractPath);

            return extractPath;
        }

        private static void WriteToConsole(string message)
        {
            Console.WriteLine($"{DateTime.Now} - {message}");
        }

        public static string RenameFolder(string pathInput)
        {
            string fileName = Path.GetFileName(pathInput);
            if (fileName.Contains(".zip"))
            {
                fileName = fileName.Replace(".zip", "");
            }

            return fileName;
        }

        public static bool CheckforFileExist(string filePath, string fileName)
        {
            return (Directory.Exists(filePath) && File.Exists(Path.Combine(filePath, fileName)));
        }

        public static bool CheckforFolderExist(string filePath, string folderName)
        {
            return Directory.Exists(Path.Combine(filePath, folderName));
        }

        public static async Task<string> ExtractDownloadedFolderForLargeFiles(string downloadFileUrl, string jwtToken, string folderName)
        {
            //Mock api fullfillment process takes more time to upload file and tests are intermittently failing,therefore we have added delay 'Task.Delay()' to avoid intermittent failure in the pipe.
            await Task.Delay(5000);
            string LargeFolderName = folderName + ".zip";
            string tempFilePath = Path.Combine(Path.GetTempPath(), LargeFolderName);

            var response = await FssApiClient.GetFileDownloadAsync(downloadFileUrl, accessToken: jwtToken);
            Assert.AreEqual(200, (int)response.StatusCode, $"Incorrect status code File Download api returned {response.StatusCode} for the url {downloadFileUrl}, instead of the expected 200.");

            Stream stream = await response.Content.ReadAsStreamAsync();

            using (FileStream outputFileStream = new FileStream(tempFilePath, FileMode.Create))
            {
                stream.CopyTo(outputFileStream);
            }
            string zipPath = tempFilePath;
            string extractPath = Path.GetTempPath() + RenameFolder(tempFilePath);

            ZipFile.ExtractToDirectory(zipPath, extractPath);

            WriteToConsole($"File has been extracted to {extractPath}");

            return extractPath;

        }

        public static string[] CheckforDirectories(string filePath)
        {
            return (Directory.GetDirectories(filePath));
        }

        public static async Task<string> ExtractDownloadedAioFolder(string downloadFileUrl, string jwtToken)
        {
            //Mock api fullfillment process takes more time to upload file for the cancellation product and tests are intermittently failing,therefore we have added delay 'Task.Delay()' to avoid intermittent failure in the pipe.
            await Task.Delay(40000);
            string tempFilePath = Path.Combine(Path.GetTempPath(), EssConfig.AIOConfig.AioExchangeSetFileName);

            var response = await FssApiClient.GetFileDownloadAsync(downloadFileUrl, accessToken: jwtToken);
            Console.WriteLine("ExtractDownloadedAioFolder method ==> "+ response.StatusCode);
            Assert.AreEqual(200, (int)response.StatusCode, $"Incorrect status code File Download api returned {response.StatusCode} for the url {downloadFileUrl}, instead of the expected 200.");

            Stream stream = await response.Content.ReadAsStreamAsync();

            using (FileStream outputFileStream = new FileStream(tempFilePath, FileMode.Create))
            {
                stream.CopyTo(outputFileStream);
            }

            string zipPath = tempFilePath;
            string extractPath = Path.GetTempPath() + RenameFolder(tempFilePath);

            ZipFile.ExtractToDirectory(zipPath, extractPath);

            return extractPath;
        }
    }
}
