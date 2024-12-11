using Azure.Storage.Blobs;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IFileSystemHelper
    {
        void CheckAndCreateFolder(string folderPath);
        bool CreateFileContent(string fileName, string content);
        bool CheckDirectoryAndFileExists(string rootPath, string zipFilePath);
        void CreateZipFile(string rootPath, string zipFileName);
        CustomFileInfo GetFileInfo(string filePath);
        byte[] UploadFileBlockMetaData(UploadBlockMetaData UploadBlockMetaData);
        void CreateFileContentWithBytes(string outputFileName, byte[] content);
        bool CheckFileExists(string filePath);
        byte[] ReadAllBytes(string filePath);
        bool DownloadReadmeFile(string filePath, Stream stream, string lineToWrite);
        void CreateFileCopy(string filePath, Stream stream);
        byte[] ConvertStreamToByteArray(Stream input);
        Task DownloadToFileAsync(BlobClient blobClient, string path);
        IDirectoryInfo[] GetDirectoryInfo(string path);
        string[] GetDirectories(string path);
        IDirectoryInfo GetParent(string path);
        string[] GetFiles(string path);
        List<FileDetail> UploadLargeMediaCommitBatch(List<BatchCommitMetaData> batchCommitMetaDataList);
        string GetFileName(string fileFullPath);
        TextWriter WriteStream(string filePath);
        void CreateFile(string filePath);
        IDirectoryInfo[] GetSubDirectories(string folderPath);
        IFileInfo[] GetZipFiles(string folderPath);
        bool DownloadFile(string filePath, Stream stream);
        //bool DownloadIhoCrtFile(string filePath, Stream stream, string lineToWrite);
    }
}
