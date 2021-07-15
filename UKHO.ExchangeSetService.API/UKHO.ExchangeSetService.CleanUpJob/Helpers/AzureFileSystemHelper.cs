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
    public class AzureFileSystemHelper : IAzureFileSystemHelper
    {
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly ILogger<AzureFileSystemHelper> logger;

        public AzureFileSystemHelper(IAzureBlobStorageClient azureBlobStorageClient,
                                    ILogger<AzureFileSystemHelper> logger)
        {
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.logger = logger;
        }

        public async Task<bool> DeleteDirectoryAsync(int numberOfDays, string storageAccountConnectionString, string containerName, string filePath)
        {
            Boolean deleteStatus = false;
            try
            {
                var subFolder = Directory.GetDirectories(filePath);
                foreach (var subFolderName in subFolder)
                {
                    DateTime subFolderDateTime;
                    string dateFolder = new DirectoryInfo(subFolderName).Name;
                    bool isValidDate = DateTimeExtensions.IsValidDate(dateFolder, out subFolderDateTime);

                    if (isValidDate)
                    {
                        var historicDateTimeInUtc = DateTime.UtcNow.AddDays(-numberOfDays);
                        DateTime subFolderDate = new DateTime(subFolderDateTime.Year, subFolderDateTime.Month, subFolderDateTime.Day);
                        DateTime historicDate = new DateTime(historicDateTimeInUtc.Year, historicDateTimeInUtc.Month, historicDateTimeInUtc.Day);

                        if (subFolderDate <= historicDate)
                        {
                            DirectoryInfo di = new DirectoryInfo(subFolderName);

                            foreach (DirectoryInfo dir in di.GetDirectories())
                            {
                                var batchId = dir.Name;
                                string scsResponseFileName = new DirectoryInfo(batchId).Name + ".json";
                                var response = await azureBlobStorageClient.DeleteFileFromContainer(storageAccountConnectionString, containerName, scsResponseFileName);
                                if (response)
                                {
                                    dir.Delete(true);
                                    logger.LogInformation(EventIds.HistoricDateFolderDeleted.ToEventId(), "SCS response json file {ScsResponseFileName} deleted successfully from the container.", scsResponseFileName);
                                }
                                else
                                {
                                    logger.LogError(EventIds.HistoricSCSResponseFileNotFound.ToEventId(), "SCS response json file {ScsResponseFileName} not found in the container.", scsResponseFileName);
                                }
                            }

                            di.Delete(true);
                            logger.LogInformation(EventIds.HistoricDateFolderDeleted.ToEventId(), "Historic folder deleted successfully for Date:{dateFolder}.", dateFolder);
                        }
                        else
                        {
                            logger.LogError(EventIds.HistoricDateFolderNotFound.ToEventId(), "Historic folder not found for Date:{dateFolder}.", dateFolder);
                        }
                        deleteStatus = true;
                    }
                }
                return deleteStatus;
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.DeleteHistoricFoldersAndFilesException.ToEventId(), ex, "Exception while deleteing historic folders and files with error {Message}", ex.Message);
                return deleteStatus;
            }
        }
    }
}
