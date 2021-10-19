using Microsoft.Extensions.Options;
using System;
using System.IO;
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

        public SearchBatchResponse GetBatches(string filter)
        {
            if (!string.IsNullOrEmpty(filter))
            {
                return FileHelper.ReadJsonFile<SearchBatchResponse>(fileShareServiceConfiguration.Value.FileDirectoryPath + fileShareServiceConfiguration.Value.ScsResponseFile); 
            }
            return null;
        }

        public byte[] GetEncFileData(string filesName)
        {
            string filePath, fileType = Path.GetExtension(filesName);
            string[] filePaths;
            byte[] bytes = null;

            if (Directory.Exists(fileShareServiceConfiguration.Value.FileDirectoryPathForENC))
            {
                filePaths = Directory.GetFiles(fileShareServiceConfiguration.Value.FileDirectoryPathForENC, string.Equals(fileType, ".TXT", StringComparison.OrdinalIgnoreCase) ? "*.TXT" : "*.000");
                filePath = filePaths[0];
                bytes = File.ReadAllBytes(filePath);
                return bytes;
            }
            return bytes;
        }
    }
}