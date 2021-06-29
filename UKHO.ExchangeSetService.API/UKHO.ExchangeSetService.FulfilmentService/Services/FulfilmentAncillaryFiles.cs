using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Net;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.Torus.Enc.Core;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentAncillaryFiles : IFulfilmentAncillaryFiles
    {
        private readonly ILogger<FulfilmentDataService> logger;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;
        private readonly IFileSystemHelper fileSystemHelper;

        public FulfilmentAncillaryFiles(ILogger<FulfilmentDataService> logger,
                                        IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                        IFileSystemHelper fileSystemHelper)
        {
            this.logger = logger;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.fileSystemHelper = fileSystemHelper;
        }
        public bool CreateProductFile(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse)
        {
            if (salesCatalogueDataResponse.ResponseCode == HttpStatusCode.OK)
            {
                string fileName = fileShareServiceConfig.Value.ProductFileName;
                string file = Path.Combine(exchangeSetInfoPath, fileName);

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
                        CellLimitSouthernmostLatitude = product.CellLimitSouthernmostLatitude,
                        CellLimitWesternmostLatitude = product.CellLimitWesternmostLatitude,
                        CellLimitNorthernmostLatitude = product.CellLimitNorthernmostLatitude,
                        CellLimitEasternmostLatitude = product.CellLimitEasternmostLatitude,
                        BaseCellUpdateNumber = product.BaseCellUpdateNumber,
                        LastUpdateNumberForPreviousEdition = product.LastUpdateNumberForPreviousEdition,
                        BaseCellLocation = product.BaseCellLocation,
                        CancelledCellReplacements = string.Empty,
                    });

                var content = productsBuilder.WriteProductsList(DateTime.UtcNow);
                var productFileName = file;
                fileSystemHelper.CheckAndCreateFolder(exchangeSetInfoPath);

                var response = fileSystemHelper.CreateFileContent(productFileName, content);
                if (!response)
                {
                    logger.LogInformation(EventIds.ProductFileIsNotCreated.ToEventId(), "Error in creating sales catalogue data product.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", batchId, correlationId);
                    return false;
                }
                return true;
            }
            else
            {
                logger.LogInformation(EventIds.ProductFileIsNotCreated.ToEventId(), "Error in creating sales catalogue data product.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", batchId, correlationId);
                return false;
            }
        }
    }
}
