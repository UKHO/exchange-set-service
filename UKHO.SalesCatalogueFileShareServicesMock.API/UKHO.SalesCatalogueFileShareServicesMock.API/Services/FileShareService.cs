using Microsoft.Extensions.Options;
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

        public string GetENCFilePath(string filesName)
        {
            string filePath; 
            string fileType = filesName.Substring(filesName.IndexOf(".") + 1, 3);

            if (Directory.Exists(fileShareServiceConfiguration.Value.FileDirectoryPathForENC) && fileType == "TXT")
            {
                string[] filePaths = Directory.GetFiles(fileShareServiceConfiguration.Value.FileDirectoryPathForENC, "*.TXT");
                filePath = filePaths[0];
                return filePath;
            }
            else
            {
                if (Directory.Exists(fileShareServiceConfiguration.Value.FileDirectoryPathForENC))
                {
                    string[] filePaths = Directory.GetFiles(fileShareServiceConfiguration.Value.FileDirectoryPathForENC, "*.000");
                    filePath = filePaths[0];
                    return filePath;
                }
            }

            return null;
        }
    }
}