﻿using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    public class FakeFileHelper : IFileSystemHelper
    {
        public bool CheckAndCreateFolderIsCalled { get; set; } = false;
        public bool CreateFileContentWithBytesIsCalled { get; set; } = false;
        public bool DownloadReadmeFileIsCalled { get; set; } = false;
        public bool CreateFileCopyIsCalled { get; set; } = false;

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
            var byteContent = new byte[100];
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
            var byteContent = new byte[100];
            return byteContent;
        }

        public Task DownloadToFileAsync(BlobClient blobClient, string path)
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

        public string[] GetFiles(string path)
        {
            throw new System.NotImplementedException();
        }

        public List<FileDetail> UploadLargeMediaCommitBatch(List<BatchCommitMetaData> batchCommitMetaDataList)
        {
            throw new System.NotImplementedException();
        }

        public string GetFileName(string fileFullPath)
        {
            throw new System.NotImplementedException();
        }

        public TextWriter WriteStream(string filePath)
        {
            throw new System.NotImplementedException();
        }

        public void CreateFile(string filePath)
        {
            throw new System.NotImplementedException();
        }

        public IDirectoryInfo[] GetSubDirectories(string folderPath)
        {
            throw new System.NotImplementedException();
        }

        public IFileInfo[] GetZipFiles(string folderPath)
        {
            throw new System.NotImplementedException();
        }

        public bool DownloadFile(string filePath, Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public bool DownloadIhoCrtFile(string filePath, Stream stream, string lineToWrite)
        {
            throw new System.NotImplementedException();
        }
    }
}
