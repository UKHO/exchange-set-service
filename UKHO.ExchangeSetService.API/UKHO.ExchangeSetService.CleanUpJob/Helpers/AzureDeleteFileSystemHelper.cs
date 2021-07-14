using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.CleanUpJob.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureDeleteFileSystemHelper : IAzureDeleteFileSystemHelper
    {
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly ILogger<AzureDeleteFileSystemHelper> logger;

        public AzureDeleteFileSystemHelper(IAzureBlobStorageClient azureBlobStorageClient,
                                    ILogger<AzureDeleteFileSystemHelper> logger)
        {
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.logger = logger;
        }

        public async Task<bool> DeleteDirectoryAsync(int numberOfDays, string storageAccountConnectionString, string containerName, string filePath)
        {
            Boolean deleteStatus = false;
            if (Directory.Exists(filePath))
            {
                var subFolder = Directory.GetDirectories(filePath);
                foreach (var subFolderName in subFolder)
                {
                    string dateFolder = new DirectoryInfo(subFolderName).Name;
                    bool isValid = DateTimeExtensions.IsValidDate(dateFolder);
                    if (isValid)
                    {
                        var subFolderDateTime = Convert.ToDateTime(dateFolder);
                        var historicDateTimeInUtc = DateTime.UtcNow.AddDays(-numberOfDays);
                        DateTime subFolderDate = new DateTime(subFolderDateTime.Year, subFolderDateTime.Month, subFolderDateTime.Day);
                        DateTime historicDate = new DateTime(historicDateTimeInUtc.Year, historicDateTimeInUtc.Month, historicDateTimeInUtc.Day);
                        if (subFolderDate <= historicDate)
                        {
                            DirectoryInfo di = new DirectoryInfo(subFolderName);
                            foreach (DirectoryInfo dir in di.GetDirectories())
                            {
                                var batchidFile = dir.Name;
                                string batchId = new DirectoryInfo(batchidFile).Name + ".json";
                                var response =await azureBlobStorageClient.DeleteContainerFile(storageAccountConnectionString, containerName, batchId);
                                if (response)
                                {
                                    dir.Delete(true);
                                    logger.LogInformation(EventIds.DeleteHistoricContainerFile.ToEventId(), "SCS response from the container file deleted successfully for BatchId:{BatchId}.", batchId);
                                }
                                else
                                {
                                    logger.LogInformation(EventIds.DeleteHistoricContainerFileNotFound.ToEventId(), "SCS response from the container file not found for BatchId:{BatchId}.", batchId);
                                }
                            }
                            di.Delete(true);
                            deleteStatus = true;
                            logger.LogInformation(EventIds.DeleteHistoricFolder.ToEventId(), "Historic folder deleted successfully for DateFolder:{dateFolder}.", dateFolder);
                        }
                    }
                }
                return deleteStatus;
            }
            else
            {
                return deleteStatus;
            }
        }
    }
}
