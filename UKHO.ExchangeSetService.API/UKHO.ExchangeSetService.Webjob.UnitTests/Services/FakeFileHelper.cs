using System.Collections.Generic;

using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    public class FakeFileHelper : IFileSystemHelper
    {
        public bool CheckAndCreateFolderIsCalled = false;
        public bool CreateFileContentWithBytesIsCalled = false;

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
    }
}
