using Microsoft.Extensions.Options;
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

        public SearchBatchResponse GetGetBatches(string filter)
        {
            if (!string.IsNullOrEmpty(filter))
            {
                return FileHelper.ReadJsonFile<SearchBatchResponse>(fileShareServiceConfiguration.Value.FileDirectoryPath + fileShareServiceConfiguration.Value.ScsResponseFile); 
            }
            return null;
        }
    }
}
