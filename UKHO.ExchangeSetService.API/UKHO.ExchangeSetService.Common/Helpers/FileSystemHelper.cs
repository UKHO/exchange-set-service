using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class FileSystemHelper : IFileSystemHelper
    {
        private readonly IFileSystem _fileSystem;
        public FileSystemHelper(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

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
            FileInfo fileInfo = new FileInfo(filePath);
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

        public byte[] ReadAllBytes(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }

        public bool DownloadReadmeFile(string filePath, Stream stream, string lineToWrite)
        {
            if (stream != null)
            {
                var extendedAsciiEncoding = Encoding.GetEncoding("iso-8859-1");
                CreateFileCopy(filePath, stream);
                var text = File.ReadAllText(filePath, extendedAsciiEncoding);
                var secondLineText = GetLine(filePath);
                text = secondLineText.Length == 0 ? lineToWrite : text.Replace(secondLineText, lineToWrite);
                if (!string.IsNullOrWhiteSpace(text))
                    File.WriteAllText(filePath, text, extendedAsciiEncoding);
                return true;
            }
            return false;
        }

        public void CreateFileCopy(string filePath, Stream stream)
        {
            if (stream != null)
            {
                using (var outputFileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    stream.CopyTo(outputFileStream);
                }
            }
        }

        private static string GetLine(string filePath)
        {
            int lineFound = 2;
            string secondLine = string.Empty;
            using (var sr = new StreamReader(filePath))
            {
                for (int i = 1; i < lineFound; i++)
                    sr.ReadLine();
                secondLine = sr.ReadLine();
            }
            return secondLine ?? string.Empty;
        }

        public byte[] ConvertStreamToByteArray(Stream input)
        {
            var ms = new MemoryStream();
            input.CopyTo(ms);
            return ms.ToArray();
        }

        public async Task DownloadToFileAsync(CloudBlockBlob cloudBlockBlob, string path)
        {
            await cloudBlockBlob.DownloadToFileAsync(path, FileMode.Create);
        }

        public IDirectoryInfo[] GetDirectoryInfo(string path)
        {
            IDirectoryInfo rootDirectory = _fileSystem.DirectoryInfo.New(path);
            return rootDirectory.GetDirectories();
        }

        public string[] GetDirectories(string path)
        {
            return _fileSystem.Directory.GetDirectories(path);
        }

        public IDirectoryInfo GetParent(string path)
        {
            return _fileSystem.Directory.GetParent(path);
        }

        public string[] GetFiles(string path)
        {
            return _fileSystem.Directory.GetFiles(path);
        }

        public List<FileDetail> UploadLargeMediaCommitBatch(List<BatchCommitMetaData> batchCommitMetaDataList)
        {
            List<FileDetail> fileDetails = new List<FileDetail>();

            foreach (var item in batchCommitMetaDataList)
            {
                FileInfo fileInfo = new FileInfo(item.FullFileName);
                using var fs = fileInfo.OpenRead();
                var fileMd5Hash = CommonHelper.CalculateMD5(fs);

                FileDetail fileDetail = new FileDetail()
                {
                    FileName = fileInfo.Name,
                    Hash = Convert.ToBase64String(fileMd5Hash)
                };
                fileDetails.Add(fileDetail);
            }

            return fileDetails;
        }

        //Returns fileName from fullPath
        public string GetFileName(string fileFullPath)
        {
            return Path.GetFileName(fileFullPath);
        }

        public TextWriter WriteStream(string filePath)
        {
            return new StreamWriter(filePath);
        }

        public void CreateFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var file = _fileSystem.File.Create(filePath);
                file.Close();
            }    
        }

        public IDirectoryInfo[] GetSubDirectories(string folderPath)
        {
            IDirectoryInfo di = _fileSystem.DirectoryInfo.New(folderPath);
            IDirectoryInfo[] dir = di.GetDirectories();
            return dir;
        }

        public IFileInfo[] GetZipFiles(string folderPath)
        {
            IDirectoryInfo di = _fileSystem.DirectoryInfo.New(folderPath);
            return di.GetFiles("*.zip");
        }
    }
}