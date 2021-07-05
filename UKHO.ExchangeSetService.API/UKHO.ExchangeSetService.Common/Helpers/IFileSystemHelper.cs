namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IFileSystemHelper
    {
        void CheckAndCreateFolder(string folderPath);
        bool CreateFileContent(string fileName, string content);
        void CreateFileContentWithBytes(string outputFileName, byte[] content);
        bool CheckFileExists(string filePath);
    }
}