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

        public void CreateFileContentWithBytes(string outputFileName, byte[] content)
        {
            using (var output = File.OpenWrite(outputFileName))
            {
                output.Write(content, 0, content.Length);
            }
        }

        public bool CheckFileExists(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}
