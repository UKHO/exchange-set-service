using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    public class FakeFileHelper : IFileSystemHelper
    {
        public bool CheckAndCreateFolderIsCalled = false;
        public bool CreateFileContentWithBytesIsCalled = false;
        public bool DownloadReadmeFileIsCalled = false;
        public bool CreateFileCopyIsCalled = false;

        public void CheckAndCreateFolder(string folderPath)
        {
            CheckAndCreateFolderIsCalled = true;
        }

        public void CreateFileContentWithBytes(string outputFileName, byte[] content)
        {
            CreateFileContentWithBytesIsCalled = true;
        }

        public bool CheckDirectoryAndFileExists(string rootPath, string zipFilePath)
        {
            throw new System.NotImplementedException();
        }

        public bool CheckFileExists(string filePath)
        {
            throw new System.NotImplementedException();
        }

        public bool CreateFileContent(string fileName, string content)
        {
            throw new System.NotImplementedException();
        }

        public void CreateZipFile(string rootPath, string zipFileName)
        {
            throw new System.NotImplementedException();
        }

        public CustomFileInfo GetFileInfo(string filePath)
        {
            throw new System.NotImplementedException();
        }

        public List<FileDetail> UploadCommitBatch(BatchCommitMetaData batchCommitMetaData)
        {
            throw new System.NotImplementedException();
        }

        public byte[] UploadFileBlockMetaData(UploadBlockMetaData UploadBlockMetaData)
        {
            throw new System.NotImplementedException();
        }

        public byte[] ReadAllBytes(string filePath)
        {
            byte[] byteContent = new byte[100];
            return byteContent;
        }

        public bool DownloadReadmeFile(string filePath, Stream stream, string lineToWrite)
        {
            DownloadReadmeFileIsCalled = true;
            return DownloadReadmeFileIsCalled;
        }

        public void CreateFileCopy(string filePath, Stream stream)
        {
            CreateFileCopyIsCalled = true;
        }

        public byte[] ConvertStreamToByteArray(Stream input)
        {
            byte[] byteContent = new byte[100];
            return byteContent;
        }

        public Task DownloadToFileAsync(CloudBlockBlob cloudBlockBlob, string path)
        {
            throw new System.NotImplementedException();
        }

        public IDirectoryInfo[] GetDirectoryInfo(string path)
        {
            throw new System.NotImplementedException();
        }
        public string[] GetDirectories(string path)
        {
             throw new System.NotImplementedException();
        }

        public IDirectoryInfo GetParent(string path)
        {
            throw new System.NotImplementedException();
        }
    }
}
