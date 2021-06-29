using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentAncillaryFiles : IFulfilmentAncillaryFiles
    {
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;
        private readonly IFileSystemHelper fileSystemHelper;

        public FulfilmentAncillaryFiles(IOptions<FileShareServiceConfiguration> fileShareServiceConfig, IFileSystemHelper fileSystemHelper)                                       
        {
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.fileSystemHelper = fileSystemHelper;
        }

        public async Task<bool> CreateSerialEncFile(string batchId, string exchangeSetPath, string correlationId)
        {
            if (!string.IsNullOrWhiteSpace(exchangeSetPath))
            {
                string serialFilePath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.SerialFileName);
                fileSystemHelper.CheckAndCreateFolder(exchangeSetPath);
                CultureInfo cultureInfo = CultureInfo.InvariantCulture;
                int weekNumber = cultureInfo.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                var serialFileContent = String.Format("GBWK{0:D2}-{1}   {2:D4}{3:D2}{4:D2}UPDATE    {5:D2}.00{6}\x0b\x0d\x0a",
                    weekNumber, DateTime.UtcNow.Year.ToString("D4").Substring(2), DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 2, "U01X01");

                fileSystemHelper.CreateFileContent(serialFilePath, serialFileContent);
                await Task.CompletedTask;
                return true;
            }
            else  
            {
                return false;
            }
        }
    }
}