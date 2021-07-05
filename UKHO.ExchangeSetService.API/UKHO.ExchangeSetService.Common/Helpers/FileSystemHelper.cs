using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class FileSystemHelper : IFileSystemHelper
    {
        public void CheckAndCreateFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        public bool CreateFileContent(string fileName, string content)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                File.WriteAllText(fileName, content);
                return true;
            }
            return false;
        }
    }
}