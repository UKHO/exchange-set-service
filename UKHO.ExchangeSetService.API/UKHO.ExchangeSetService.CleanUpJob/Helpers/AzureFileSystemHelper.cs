using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.CleanUpJob.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureFileSystemHelper : IAzureFileSystemHelper
    {
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly ILogger<AzureFileSystemHelper> logger;
        private bool deteleFolderStatus = false;

        public AzureFileSystemHelper(IAzureBlobStorageClient azureBlobStorageClient,
                                    ILogger<AzureFileSystemHelper> logger)
        {
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.logger = logger;
        }

        public async Task<bool> DeleteDirectoryAsync(int numberOfDays, string storageAccountConnectionString, string containerName, string filePath)
        {
            DateTime subFolderDateTime;
            try
            {
                var subFolder = Directory.GetDirectories(filePath);
                var historicDateTimeInUtc = DateTime.UtcNow.AddDays(-numberOfDays);
                DateTime historicDate = new DateTime(historicDateTimeInUtc.Year, historicDateTimeInUtc.Month, historicDateTimeInUtc.Day);

                foreach (var subFolderItem in subFolder)
                {
                    string subFolderName = new DirectoryInfo(subFolderItem).Name;
                    bool isSubFolderDateFolder = DateTimeExtensions.IsValidDate(subFolderName, out subFolderDateTime);
                    
                    if (isSubFolderDateFolder)
                    {
                        DateTime subFolderDate = new DateTime(subFolderDateTime.Year, subFolderDateTime.Month, subFolderDateTime.Day);
                        
                        if (subFolderDate <= historicDate)
                        {
                            await DeleteHistoricDateFolder(subFolderItem, storageAccountConnectionString, containerName, subFolderName);
                        }
                    }
                }

                if (!deteleFolderStatus)
                {
                    logger.LogError(EventIds.HistoricDateFolderNotFound.ToEventId(), "Historic folder not found for Date:{historicDate}.", historicDate);
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.DeleteHistoricFoldersAndFilesException.ToEventId(), ex, "Exception while deleteing historic folders and files with error {Message}", ex.Message);
                return false;
            }
        }

        public async Task DeleteHistoricDateFolder(string subFolderItem, string storageAccountConnectionString, string containerName, string subFolderName)
        {
            DirectoryInfo di = new DirectoryInfo(subFolderItem);

            foreach (DirectoryInfo subDirectory in di.GetDirectories())
            {
                await DeleteHistoricScsResponseFile(subDirectory, storageAccountConnectionString, containerName);
            }

            di.Delete(true);
            deteleFolderStatus = true;
            logger.LogInformation(EventIds.HistoricDateFolderDeleted.ToEventId(), "Historic folder deleted successfully for Date:{subFolderName}.", subFolderName);
        }

        public async Task DeleteHistoricScsResponseFile(DirectoryInfo subDirectory, string storageAccountConnectionString, string containerName)
        {
            var batchId = subDirectory.Name;
            string scsResponseFileName = new DirectoryInfo(batchId).Name + ".json";

            var blobClient = await azureBlobStorageClient.GetBlobClient(scsResponseFileName, storageAccountConnectionString, containerName);
            var response = await blobClient.DeleteIfExistsAsync();

            if (response)
            {
                subDirectory.Delete(true);
                logger.LogInformation(EventIds.HistoricSCSResponseFileDeleted.ToEventId(), "SCS response json file {ScsResponseFileName} deleted successfully from the container.", scsResponseFileName);
            }
            else
            {
                logger.LogError(EventIds.HistoricSCSResponseFileNotFound.ToEventId(), "SCS response json file {ScsResponseFileName} not found in the container.", scsResponseFileName);
            }
        }
    }
}