using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.Torus.Enc.Core;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentAncillaryFiles : IFulfilmentAncillaryFiles
    {
        private readonly IFulfilmentSalesCatalogueService fulfilmentSalesCatalogueService;
        private readonly ILogger<FulfilmentDataService> logger;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;

        public FulfilmentAncillaryFiles(IFulfilmentSalesCatalogueService fulfilmentSalesCatalogueService,
                                        ILogger<FulfilmentDataService> logger,
                                        IOptions<FileShareServiceConfiguration> fileShareServiceConfig
                                        )
        {
            this.fulfilmentSalesCatalogueService = fulfilmentSalesCatalogueService;
            this.logger = logger;
            this.fileShareServiceConfig = fileShareServiceConfig;
        }
        public async Task<bool> CreateSalesCatalogueDataProductFile(string batchId, string exchangeSetInfoPath, string correlationId)
        {
            SalesCatalogueDataResponse salesCatalogueTypeResponse = await fulfilmentSalesCatalogueService.CreateSalesCatalogueDataResponse();
            if (salesCatalogueTypeResponse.ResponseCode == HttpStatusCode.OK)
            {
                string fileName = fileShareServiceConfig.Value.ProductFileName;
                string file = Path.Combine(exchangeSetInfoPath, fileName);
                CheckCreateFolderPath(exchangeSetInfoPath);

                var productsBuilder = new ProductListBuilder();
                foreach (var product in salesCatalogueTypeResponse.ResponseBody.OrderBy(p => p.ProductName))
                    productsBuilder.Add(new ProductListEntry()
                    {
                        ProductName = product.ProductName +".000",
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
                if (File.Exists(productFileName))
                    File.Delete(productFileName);
                File.WriteAllText(productFileName, content);

                return true;
            }
            else
            {
                logger.LogInformation(EventIds.SalesCatalogueDataProductFileIsNotCreated.ToEventId(), "Error in creating sales catalogue data product.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", batchId, correlationId);
                return false;
            }
        }

        private static void CheckCreateFolderPath(string exchangeSetInfoPath)
        {
            if (!Directory.Exists(exchangeSetInfoPath))
            {
                Directory.CreateDirectory(exchangeSetInfoPath);
            }
        }
    }
}
