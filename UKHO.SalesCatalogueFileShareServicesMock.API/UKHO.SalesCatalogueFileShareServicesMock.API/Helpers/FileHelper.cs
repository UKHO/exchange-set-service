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
            bool validationFlag = GetPathValidation(outputFileName);
            if (validationFlag)
            {
                using (var output = File.OpenWrite(outputFileName))
                {
                    output.Write(content, 0, content.Length);
                }
            }
        }

        public static bool CheckBatchWithZipFileExist(string filePathWithFileName)
        {
            var path = filePathWithFileName; 
            if (!string.IsNullOrEmpty(path))
            {
                return File.Exists(path);
            }
            return false;
        }

        public static bool CheckFolderExists(string homeDirectoryPath, string folderName, string batchid)
        {
            string uploadBlockFilePath = Path.Combine(homeDirectoryPath, folderName, batchid);
            return Directory.Exists(uploadBlockFilePath);
        }

        public static bool GetPathValidation(string outputFileName)
        {
            bool possiblePath = outputFileName.IndexOfAny(Path.GetInvalidPathChars()) == -1;
            return possiblePath;
        }
    }
}
