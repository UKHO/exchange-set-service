using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
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
            return new BatchResponse() { BatchId = batchId };
        }

        public SearchBatchResponse GetBatches(string filter)
        {
            string fileName;
            if (filter.ToUpper().Contains("DVD INFO"))
            {
                fileName = fileShareServiceConfiguration.Value.FssInfoResponseFile;
            }
            else if (filter.ToUpper().Contains("ADC"))
            {
                fileName = fileShareServiceConfiguration.Value.FssAdcResponseFile;
            }
            else
            {
                fileName = fileShareServiceConfiguration.Value.ScsResponseFile;
            }
            var response = FileHelper.ReadJsonFile<SearchBatchResponse>(fileShareServiceConfiguration.Value.FileDirectoryPath + fileName);
            if (filter.ToUpper().Contains("README.TXT", StringComparison.OrdinalIgnoreCase))
            {
                response.Entries.RemoveRange(1, response.Entries.Count - 1);
            }
            return response;
        }

        public byte[] GetFileData(string homeDirectoryPath, string batchId, string filesName)
        {
            string fileType = Path.GetExtension(filesName);
            string[] filePaths;
            byte[] bytes = null;
            var setZipPath = Path.Combine(homeDirectoryPath, fileShareServiceConfiguration.Value.FolderDirectoryName, batchId);
            switch (string.IsNullOrEmpty(setZipPath))
            {
                case false when FileHelper.ValidateFilePath(setZipPath) && Directory.Exists(setZipPath) && FileHelper.ValidateFilePath(Directory.GetFiles(setZipPath, filesName).FirstOrDefault()) && string.Equals("V01X01.zip", filesName, StringComparison.OrdinalIgnoreCase):
                case false when FileHelper.ValidateFilePath(setZipPath) && Directory.Exists(setZipPath) && FileHelper.ValidateFilePath(Directory.GetFiles(setZipPath, filesName).FirstOrDefault()) && (string.Equals("M01X02.zip", filesName, StringComparison.OrdinalIgnoreCase) || string.Equals("M02X02.zip", filesName, StringComparison.OrdinalIgnoreCase)):
                    filePaths = Directory.GetFiles(setZipPath, filesName);
                    break;
                default:
                    {
                        string[] fileDirectorys = Directory.GetFiles(fileShareServiceConfiguration.Value.FileDirectoryPath, $"*{Path.GetExtension(filesName)}*", SearchOption.AllDirectories).Where(i => i.Split("\\").Last().Equals(filesName)).ToArray();
                       
                        bool isEnc = (fileDirectorys == null || fileDirectorys.Length == 0) || (fileDirectorys.Length > 0 &&  fileDirectorys[0].Contains(fileShareServiceConfiguration.Value.FileDirectoryPathForENC));
                        
                        if (FileHelper.ValidateFilePath(fileShareServiceConfiguration.Value.FileDirectoryPathForENC) && Directory.Exists(fileShareServiceConfiguration.Value.FileDirectoryPathForENC) && isEnc)
                        {
                            filePaths = Directory.GetFiles(fileShareServiceConfiguration.Value.FileDirectoryPathForENC, string.Equals(fileType, ".TXT", StringComparison.OrdinalIgnoreCase) ? "*.TXT" : "*.000");
                        }
                        else if(fileDirectorys != null && fileDirectorys.Length > 0)
                        {
                            filePaths = fileDirectorys;
                        }
                        else
                        {
                            filePaths = Directory.GetFiles(fileShareServiceConfiguration.Value.FileDirectoryPathForENC, "*.TXT");
                        }
                        break;
                    }
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
            string uploadBlockFolderPath = Path.Combine(homeDirectoryPath, folderName, batchid);
            string uploadBlockFilePath = Path.Combine(homeDirectoryPath, folderName, batchid, fileName);

            if (FileHelper.CheckFolderExists(uploadBlockFolderPath))
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

            return FileHelper.ValidateFilePath(batchFolderPath) && FileHelper.CheckBatchWithZipFileExist(batchFolderPath);
        }

        public bool CheckBatchFolderExists(string batchid, string homeDirectoryPath)
        {
            string folderName = fileShareServiceConfiguration.Value.FolderDirectoryName;
            string batchFolderPath = Path.Combine(homeDirectoryPath, folderName, batchid);

            return FileHelper.CheckFolderExists(batchFolderPath);
        }

        public BatchStatusResponse GetBatchStatus(string batchId, string homeDirectoryPath)
        {
            BatchStatusResponse batchStatusResponse = new BatchStatusResponse();
            string folderName = fileShareServiceConfiguration.Value.FolderDirectoryName;
            string batchFolderPath = Path.Combine(homeDirectoryPath, folderName, batchId);

            if (FileHelper.ValidateFilePath(batchFolderPath) && FileHelper.CheckFolderExists(batchFolderPath))
            {
                batchStatusResponse.BatchId = batchId;
                batchStatusResponse.Status = "Committed";
            }
            return batchStatusResponse;
        }

        public bool CleanUp(List<string> batchId, string homeDirectoryPath)
        {
            string folderName = fileShareServiceConfiguration.Value.FolderDirectoryName;
            bool deleteFlag = false;
            foreach (var item in batchId)
            {
                string exchangeSetZipFolderPath = Path.Combine(homeDirectoryPath, folderName, item);
                var response = FileHelper.CleanUp(exchangeSetZipFolderPath);
                if (response)
                {
                    deleteFlag = true;
                }
            }
            return deleteFlag;
        }
    }
}