using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.Torus.Enc.Core;

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

        public bool CreateProductFile(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse)
        {
            if (salesCatalogueDataResponse.ResponseCode == HttpStatusCode.OK)
            {
                string fileName = fileShareServiceConfig.Value.ProductFileName;
                string filePath = Path.Combine(exchangeSetInfoPath, fileName);

                var productsBuilder = new ProductListBuilder();
                foreach (var product in salesCatalogueDataResponse.ResponseBody.OrderBy(p => p.ProductName))
                    productsBuilder.Add(new ProductListEntry()
                    {
                        ProductName = product.ProductName + fileShareServiceConfig.Value.BaseCellExtension,
                        Compression = product.Compression,
                        Encryption = product.Encryption,
                        BaseCellIssueDate = product.BaseCellIssueDate,
                        BaseCellEdition = product.BaseCellEditionNumber,
                        IssueDateLatestUpdate = product.IssueDateLatestUpdate,
                        LatestUpdateNumber = product.LatestUpdateNumber,
                        FileSize = product.FileSize,
                        TenDataCoverageCoordinates = ",,,,,,,,,,,,,,,,,,,",
                        CellLimitSouthernmostLatitude = Convert.ToDecimal(product.CellLimitSouthernmostLatitude.ToString(Convert.ToString("f10"))),
                        CellLimitWesternmostLatitude = Convert.ToDecimal(product.CellLimitWesternmostLatitude.ToString(Convert.ToString("f10"))),
                        CellLimitNorthernmostLatitude = Convert.ToDecimal(product.CellLimitNorthernmostLatitude.ToString(Convert.ToString("f10"))),
                        CellLimitEasternmostLatitude = Convert.ToDecimal(product.CellLimitEasternmostLatitude.ToString(Convert.ToString("f10"))),
                        BaseCellUpdateNumber = product.BaseCellUpdateNumber,
                        LastUpdateNumberForPreviousEdition = product.LastUpdateNumberForPreviousEdition,
                        BaseCellLocation = product.BaseCellLocation,
                        CancelledCellReplacements = string.Empty,
                    });

                var content = productsBuilder.WriteProductsList(DateTime.UtcNow);
                fileSystemHelper.CheckAndCreateFolder(exchangeSetInfoPath);

                var response = fileSystemHelper.CreateFileContent(filePath, content);
                if (!response)
                {
                    logger.LogError(EventIds.ProductFileIsNotCreated.ToEventId(), "Error in creating sales catalogue data product.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", batchId, correlationId);
                    return false;
                }
                return true;
            }
            else
            {
                logger.LogError(EventIds.ProductFileIsNotCreated.ToEventId(), "Error in creating sales catalogue data product.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", batchId, correlationId);
                return false;
            }
        }
    }
}