namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IFileSystemHelper
    {
        void CheckAndCreateFolder(string folderPath);
        bool CreateFileContent(string fileName, string content);
        bool CheckDirectoryAndFileExists(string rootPath, string zipFilePath);
        void CreateZipFile(string rootPath,string zipFileName);
    }
}