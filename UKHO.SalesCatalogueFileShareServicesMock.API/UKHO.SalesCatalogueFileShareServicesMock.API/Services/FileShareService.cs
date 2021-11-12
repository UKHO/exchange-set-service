using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using UKHO.SalesCatalogueFileShareServicesMock.API.Common;
using UKHO.SalesCatalogueFileShareServicesMock.API.Helpers; 
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Services
{
    public class FileShareService
    {
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfiguration;

        public FileShareService(IOptions<FileShareServiceConfiguration> fileShareServiceConfiguration)
        {
            this.fileShareServiceConfiguration = fileShareServiceConfiguration;
        }

        public BatchResponse CreateBatch(string homeDirectoryPath)
        {
            string folderName = fileShareServiceConfiguration.Value.FolderDirectoryName;
            Guid batchId = Guid.NewGuid();
            string batchFolderPath = Path.Combine(homeDirectoryPath, folderName, batchId.ToString());
            
            FileHelper.CheckAndCreateFolder(batchFolderPath);
            return new BatchResponse() { BatchId= batchId };
        }

        public SearchBatchResponse GetBatches(string filter)
        {
            var response = FileHelper.ReadJsonFile<SearchBatchResponse>(fileShareServiceConfiguration.Value.FileDirectoryPath + fileShareServiceConfiguration.Value.ScsResponseFile);
            if (filter.Contains("README.TXT", StringComparison.OrdinalIgnoreCase))
            {
                response.Entries.RemoveRange(1, response.Entries.Count - 1);
            }
            return response;
        }

        public byte[] GetFileData(string filesName)
        {
            string fileType = Path.GetExtension(filesName);
            string[] filePaths;
            byte[] bytes = null;

            if (Directory.Exists(fileShareServiceConfiguration.Value.FileDirectoryPathForENC) && !string.Equals("README.TXT", filesName, StringComparison.OrdinalIgnoreCase))
            {
                filePaths = Directory.GetFiles(fileShareServiceConfiguration.Value.FileDirectoryPathForENC, string.Equals(fileType, ".TXT", StringComparison.OrdinalIgnoreCase) ? "*.TXT" : "*.000");                
            }
            else
            {
                filePaths = Directory.GetFiles(fileShareServiceConfiguration.Value.FileDirectoryPathForReadme, filesName);
            }
            if (filePaths != null && filePaths.Any())
            {
                string filePath = filePaths[0];
                bytes = File.ReadAllBytes(filePath); 
            }
            return bytes;
        }

        public bool UploadBlockOfFile(string batchid, string fileName, object data, string homeDirectoryPath)
        {
            string folderName = fileShareServiceConfiguration.Value.FolderDirectoryName;
            string uploadBlockFilePath = Path.Combine(homeDirectoryPath, folderName, batchid, fileName);

            if (FileHelper.CheckFolderExists(homeDirectoryPath, folderName, batchid))
            {
                FileHelper.CreateFileContentWithBytes(uploadBlockFilePath, (byte[])data);
                return true;
            }
            return false;
        }

        public bool CheckBatchWithZipFileExist(string batchid, string fileName, string homeDirectoryPath)
        {
            string folderName = fileShareServiceConfiguration.Value.FolderDirectoryName;
            string batchFolderPath = Path.Combine(homeDirectoryPath, folderName, batchid, fileName);

            return FileHelper.CheckBatchWithZipFileExist(batchFolderPath);
        }
    }
}