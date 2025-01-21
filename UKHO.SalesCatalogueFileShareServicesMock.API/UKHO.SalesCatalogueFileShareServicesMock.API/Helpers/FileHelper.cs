using System.IO;
using System.Linq;
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
        public static void CreateFileContentWithBytes(string uploadBlockFilePath, byte[] content)
        {
            if (ValidateFilePath(uploadBlockFilePath))
            {
                using (var output = File.OpenWrite(uploadBlockFilePath))
                {
                    output.Write(content, 0, content.Length);
                }
            }
        }

        public static bool CheckBatchWithZipFileExist(string filePathWithFileName)
        {
            if (ValidateFilePath(filePathWithFileName))
            {
                return File.Exists(filePathWithFileName);
            }
            return false;
        }

        public static bool CheckFolderExists(string filePath)
        {
            if (ValidateFilePath(filePath))
            {
                return Directory.Exists(filePath);
            }
            return false;
        }

        public static bool ValidateFilePath(string filePath)
        {
            return !string.IsNullOrEmpty(filePath) && filePath.IndexOfAny(Path.GetInvalidPathChars()) == -1;
        }

        public static bool CleanUp(string filePath)
        {
            if (CheckFolderExists(filePath))
            {
                DirectoryInfo di = new DirectoryInfo(filePath);
                di.Delete(true);
                return true;
            }
            return false;
        }

        public static bool CheckEmptyDirectory(string folderPath)
        {
            return Directory.EnumerateFileSystemEntries(folderPath).Any();
        }
    }
}
