using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;

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

        public bool CheckDirectoryAndFileExists(string rootPath, string zipFilePath)
        {
            return (Directory.Exists(rootPath) && !File.Exists(zipFilePath));
        }

        public void CreateZipFile(string rootPath, string zipFileName)
        {
            ZipFile.CreateFromDirectory(rootPath, zipFileName);
        }

        public CustomFileInfo GetFileInfo(string filePath)
        {
            FileInfo fileInfo =  new FileInfo(filePath);
            CustomFileInfo customFileInfo = new CustomFileInfo()
            {
                Name = fileInfo.Name,
                FullName = fileInfo.FullName,
                Length = fileInfo.Length
            };
            return customFileInfo; 
        }

        public byte[] UploadFileBlockMetaData(UploadBlockMetaData UploadBlockMetaData)
        {            
            var fileInfo = new FileInfo(UploadBlockMetaData.FullFileName);            
            Byte[] byteData = new Byte[UploadBlockMetaData.Length];
            using (var fs = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Seek(UploadBlockMetaData.Offset, SeekOrigin.Begin);
                fs.Read(byteData);
            }
            return byteData;
        }

        public List<FileDetail> UploadCommitBatch(BatchCommitMetaData batchCommitMetaData)
        {
            FileInfo fileInfo = new FileInfo(batchCommitMetaData.FullFileName);
            using var fs = fileInfo.OpenRead();
            var fileMd5Hash = CommonHelper.CalculateMD5(fs);
            List<FileDetail> fileDetails = new List<FileDetail>();
            FileDetail fileDetail = new FileDetail()
            {
                FileName = fileInfo.Name,
                Hash = Convert.ToBase64String(fileMd5Hash)
            };
            fileDetails.Add(fileDetail);
            return fileDetails;
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
