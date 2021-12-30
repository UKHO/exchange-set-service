using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IFileSystemHelper
    {
        void CheckAndCreateFolder(string folderPath);
        bool CreateFileContent(string fileName, string content);
        bool CheckDirectoryAndFileExists(string rootPath, string zipFilePath);
        void CreateZipFile(string rootPath,string zipFileName);
        CustomFileInfo GetFileInfo(string filePath);
        byte[] UploadFileBlockMetaData(UploadBlockMetaData UploadBlockMetaData);
        List<FileDetail> UploadCommitBatch(BatchCommitMetaData batchCommitMetaData);
        void CreateFileContentWithBytes(string outputFileName, byte[] content);
        bool CheckFileExists(string filePath);
        byte[] ReadAllBytes(string filePath);
        bool DownloadReadmeFile(string filePath, Stream stream, string lineToWrite);
        void CreateFileCopy(string filePath, Stream stream);
        byte[] ConvertStreamToByteArray(Stream input);
        Task DownloadToFileAsync(CloudBlockBlob cloudBlockBlob, string path);
    }
}