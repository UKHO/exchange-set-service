using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentAncillaryFiles : IFulfilmentAncillaryFiles
    {
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;
        private readonly IFileSystemHelper fileSystemHelper;
        private readonly ILogger<FulfilmentDataService> logger;

        public FulfilmentAncillaryFiles(IOptions<FileShareServiceConfiguration> fileShareServiceConfig, IFileSystemHelper fileSystemHelper, ILogger<FulfilmentDataService> logger)
        {
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.fileSystemHelper = fileSystemHelper;
            this.logger = logger;
        }

        public async Task<bool> CreateSerialEncFile(string batchId, string exchangeSetPath, string correlationId)
        {
            if (!string.IsNullOrWhiteSpace(exchangeSetPath))
            {
                string serialFilePath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.SerialFileName);
                fileSystemHelper.CheckAndCreateFolder(exchangeSetPath);
                int weekNumber = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow);
                var serialFileContent = String.Format("GBWK{0:D2}-{1}   {2:D4}{3:D2}{4:D2}UPDATE    {5:D2}.00{6}\x0b\x0d\x0a",
                    weekNumber, DateTime.UtcNow.Year.ToString("D4").Substring(2), DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 2, "U01X01");

                fileSystemHelper.CreateFileContent(serialFilePath, serialFileContent);
                await Task.CompletedTask;
                return true;
            }
            else
            {
                logger.LogError(EventIds.SerialFileIsNotCreated.ToEventId(), "Error in creating serial.enc file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} - Invalid Exchange Set Path", batchId, correlationId);
                return false;
            }
        }
    }
}