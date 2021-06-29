namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IFileSystemHelper
    {
        void CheckAndCreateFolder(string folderPath);
        void CreateFileContentWithBytes(string outputFileName, byte[] content);
        bool CheckFileExists(string filePath);
    }
}
