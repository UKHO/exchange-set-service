using Newtonsoft.Json;
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
        static FileShareServiceConfiguration Config = new TestConfiguration().FssConfig;
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
                var batchStatusResponseObj = JsonConvert.DeserializeObject<ResponseBatchStatusModel>(await batchStatusResponse.Content.ReadAsStringAsync());
                batchStatus = batchStatusResponseObj.Status;

                if (batchStatus.Equals("Committed"))
                    break;
            }

            return batchStatus;
        }

        public static async Task<string> ExtractDownloadedFolder(string downloadFileUrl, string jwtToken)
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), EssConfig.ExchangeSetFileName);
            var response = await FssApiClient.GetFileDownloadAsync(downloadFileUrl, jwtToken);

            Stream stream = await response.Content.ReadAsStreamAsync();

            using (FileStream outputFileStream = new FileStream(tempFilePath, FileMode.Append))
            {
                stream.CopyTo(outputFileStream);
            }

            WriteToConsole($"Temp file {tempFilePath} has been created to download file contents.");
            
            string zipPath =tempFilePath;
            string extractPath = Path.GetTempPath();

            ZipFile.ExtractToDirectory(zipPath, extractPath);

            return Path.Combine(extractPath,"V01X01");
        }

        private static void WriteToConsole(string message)
        {
            Console.WriteLine($"{DateTime.Now} - {message}");
        }

    }
}
