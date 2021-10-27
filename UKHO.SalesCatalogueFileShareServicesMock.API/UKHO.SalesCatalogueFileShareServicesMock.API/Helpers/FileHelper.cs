using System.IO;
using System.Text.Json;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Helpers
{
    public static class FileHelper
    {
        public static T ReadJsonFile<T>(string filePathWithFileName)
        {
            JsonSerializerOptions Options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            var folderDetails = Path.Combine(Directory.GetCurrentDirectory(), filePathWithFileName);
            var myJsonString = File.ReadAllText(folderDetails);
            return JsonSerializer.Deserialize<T>(myJsonString, Options);
        }
        public static void CheckAndCreateFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }
        public static void CreateFileContentWithBytes(string outputFileName, byte[] content)
        {
            using (var output = File.OpenWrite(outputFileName))
            {
                output.Write(content, 0, content.Length);
            }
        }

        public static bool CheckBatchWithZipFileExist(string filePathWithFileName)
        {
            if (!string.IsNullOrEmpty(filePathWithFileName))
            {
                return File.Exists(filePathWithFileName);
            }
            return false;
        }

    }
}
