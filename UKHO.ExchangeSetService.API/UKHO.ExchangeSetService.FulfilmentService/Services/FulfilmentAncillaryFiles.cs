using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.Torus.Enc.Core.EncCatalogue;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.Torus.Enc.Core;
using UKHO.Torus.Core;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentAncillaryFiles : IFulfilmentAncillaryFiles
    {
        private readonly ILogger<FulfilmentAncillaryFiles> logger;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;
        private readonly IFileSystemHelper fileSystemHelper;
        private readonly int crcLength = 8;

        public FulfilmentAncillaryFiles(ILogger<FulfilmentAncillaryFiles> logger, IOptions<FileShareServiceConfiguration> fileShareServiceConfig, IFileSystemHelper fileSystemHelper)
        {
            this.logger = logger;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.fileSystemHelper = fileSystemHelper;
        }

        public async Task<bool> CreateSerialEncFile(string batchId, string exchangeSetPath, string correlationId)
        {
            bool checkSerialEncFileCreated = false;
            if (!string.IsNullOrWhiteSpace(exchangeSetPath))
            {
                string serialFilePath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.SerialFileName);
                fileSystemHelper.CheckAndCreateFolder(exchangeSetPath);
                int weekNumber = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow);
                var serialFileContent = String.Format("GBWK{0:D2}-{1}   {2:D4}{3:D2}{4:D2}UPDATE    {5:D2}.00{6}\x0b\x0d\x0a",
                    weekNumber, DateTime.UtcNow.Year.ToString("D4").Substring(2), DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 2, "U01X01");

                fileSystemHelper.CreateFileContent(serialFilePath, serialFileContent);
                await Task.CompletedTask;

                if (fileSystemHelper.CheckFileExists(serialFilePath))
                    checkSerialEncFileCreated = true;
                else
                {
                    logger.LogError(EventIds.SerialFileIsNotCreated.ToEventId(), "Error in creating serial.enc file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} - Invalid Exchange Set Path", batchId, correlationId);
                    throw new FulfilmentException(EventIds.SerialFileIsNotCreated.ToEventId());
                }
            }
            return checkSerialEncFileCreated;
        }

        public async Task<bool> CreateCatalogFile(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse)
        {
            var catBuilder = new Catalog031BuilderFactory().Create();
            var readMeFileName = Path.Combine(exchangeSetRootPath, fileShareServiceConfig.Value.ReadMeFileName);
            var outputFileName = Path.Combine(exchangeSetRootPath, fileShareServiceConfig.Value.CatalogFileName);

            if (fileSystemHelper.CheckFileExists(readMeFileName))
            {
                catBuilder.Add(new CatalogEntry()
                {
                    FileLocation = fileShareServiceConfig.Value.ReadMeFileName,
                    Implementation = "TXT"
                });
            }

            if (listFulfilmentData != null && listFulfilmentData.Any())
            {
                listFulfilmentData = listFulfilmentData.OrderBy(a => a.ProductName).ThenBy(b => b.EditionNumber).ThenBy(c => c.UpdateNumber).ToList();

                List<Tuple<string, string>> orderPreference = new List<Tuple<string, string>> { new Tuple<string, string>("application/s63", "BIN"),
                    new Tuple<string, string>("text/plain", "ASC"), new Tuple<string, string>("text/plain", "TXT"), new Tuple<string, string>("image/tiff", "TIF") };

                foreach (var listItem in listFulfilmentData)
                {
                    CreateCatalogEntry(listItem, orderPreference, catBuilder, salesCatalogueDataResponse, salesCatalogueProductResponse, exchangeSetRootPath, batchId, correlationId);
                }
            }

            var cat031Bytes = catBuilder.WriteCatalog(fileShareServiceConfig.Value.ExchangeSetFileFolder);
            fileSystemHelper.CheckAndCreateFolder(exchangeSetRootPath);

            fileSystemHelper.CreateFileContentWithBytes(outputFileName, cat031Bytes);

            await Task.CompletedTask;

            if (fileSystemHelper.CheckFileExists(outputFileName))
                return true;
            else
            {
                logger.LogError(EventIds.CatalogFileIsNotCreated.ToEventId(), "Error in creating catalog.031 file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                throw new FulfilmentException(EventIds.CatalogFileIsNotCreated.ToEventId());
            }
        }

        private void CreateCatalogEntry(FulfilmentDataResponse listItem, List<Tuple<string, string>> orderPreference, ICatalog031Builder catBuilder, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse, string exchangeSetRootPath, string batchId, string correlationId)
        {
            int length = 2;
            listItem.Files = listItem.Files.OrderByDescending(
                                item => Enumerable.Reverse(orderPreference).ToList().IndexOf(new Tuple<string, string>(item.MimeType.ToLower(), GetMimeType(item.Filename.ToLower(), item.MimeType.ToLower(), batchId, correlationId))));

            foreach (var item in listItem.Files)
            {
                string fileLocation = Path.Combine(listItem.ProductName.Substring(0, length), listItem.ProductName, listItem.EditionNumber.ToString(), listItem.UpdateNumber.ToString(), item.Filename);
                string mimeType = GetMimeType(item.Filename.ToLower(), item.MimeType.ToLower(), batchId, correlationId);
                string comment = string.Empty;               
                BoundingRectangle boundingRectangle = new BoundingRectangle();

                if (mimeType == "BIN")
                {
                    var salescatalogProduct = salesCatalogueDataResponse.ResponseBody.Where(s => s.ProductName == listItem.ProductName).Select(s => s).FirstOrDefault();
                    if (salescatalogProduct == null)
                    {
                        logger.LogError(EventIds.SalesCatalogueServiceCatalogueDataNotFoundForProduct.ToEventId(), "Error in sales catalogue service catalogue end point when product details not found for Product:{ProductName} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", listItem.ProductName, batchId, correlationId);
                        throw new FulfilmentException(EventIds.SalesCatalogueServiceCatalogueDataNotFoundForProduct.ToEventId());
                    }
                    
                    comment = SetCatalogFileComment(listItem, salescatalogProduct, salesCatalogueProductResponse);

                    boundingRectangle.LatitudeNorth = salescatalogProduct.CellLimitNorthernmostLatitude;
                    boundingRectangle.LatitudeSouth = salescatalogProduct.CellLimitSouthernmostLatitude;
                    boundingRectangle.LongitudeEast = salescatalogProduct.CellLimitEasternmostLatitude;
                    boundingRectangle.LongitudeWest = salescatalogProduct.CellLimitWesternmostLatitude;
                }

                catBuilder.Add(new CatalogEntry()
                {
                    FileLocation = fileLocation,
                    FileLongName = "",
                    Implementation = mimeType,
                    Crc = (mimeType == "BIN") ? item.Attributes.Where(a => a.Key == "s57-CRC").Select(a => a.Value).FirstOrDefault() : GetCrcString(Path.Combine(exchangeSetRootPath, fileLocation)),
                    Comment = comment,
                    BoundingRectangle = boundingRectangle
                });
            }
        }

        private string SetCatalogFileComment(FulfilmentDataResponse listItem, SalesCatalogueDataProductResponse salescatalogProduct, SalesCatalogueProductResponse salesCatalogueProductResponse)
        {
            string getIssueAndUpdateDate = null;
            int? cancelledUpdateNumber = null;
            var salescatalogProductResponse = salesCatalogueProductResponse.Products.Where(s => s.ProductName == listItem.ProductName).Where(s => s.EditionNumber == listItem.EditionNumber).Select(s => s).FirstOrDefault();

            if (salescatalogProductResponse.Dates != null)
            {
                var dates = salescatalogProductResponse.Dates.Where(s => s.UpdateNumber == listItem.UpdateNumber).Select(s => s).FirstOrDefault();
                getIssueAndUpdateDate = GetIssueAndUpdateDate(dates);
            }
            if (salescatalogProductResponse.Cancellation != null)
            {
                cancelledUpdateNumber = salescatalogProductResponse.Cancellation.UpdateNumber;
            }

            //BoundingRectangle and Comment only required for BIN
            return salescatalogProduct.BaseCellEditionNumber == 0 && cancelledUpdateNumber == listItem.UpdateNumber
                ? $"{fileShareServiceConfig.Value.CommentVersion},EDTN={salescatalogProduct.BaseCellEditionNumber},UPDN={listItem.UpdateNumber},{getIssueAndUpdateDate}"
                : $"{fileShareServiceConfig.Value.CommentVersion},EDTN={listItem.EditionNumber},UPDN={listItem.UpdateNumber},{getIssueAndUpdateDate}";             
        }

        private string GetMimeType(string fileName, string mimeType, string batchId, string correlationId)
        {
            string fileExtension = Path.GetExtension(fileName);
            switch (mimeType)
            {
                case "application/s63":
                    return "BIN";

                case "text/plain":
                    if (fileExtension == ".txt")
                        return "TXT";
                    else
                        return "ASC";

                case "image/tiff":
                    return "TIF";

                default:
                    logger.LogInformation(EventIds.UnexpectedDefaultFileExtension.ToEventId(), "Default - Unexpected file extension for File:{filename} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fileName, batchId, correlationId);
                    return fileExtension?.TrimStart('.').ToUpper();
            }
        }

        public async Task<bool> CreateProductFile(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse)
        {
            if (salesCatalogueDataResponse.ResponseCode == HttpStatusCode.OK)
            {
                string fileName = fileShareServiceConfig.Value.ProductFileName;
                string filePath = Path.Combine(exchangeSetInfoPath, fileName);

                var productsBuilder = new ProductListBuilder();
                productsBuilder.UseDefaultOutputTime = false;

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
                        CancelledCellReplacements = String.Join(";", product.CancelledCellReplacements)
                    });

                var content = productsBuilder.WriteProductsList(DateTime.UtcNow);
                fileSystemHelper.CheckAndCreateFolder(exchangeSetInfoPath);

                var response = fileSystemHelper.CreateFileContent(filePath, content);
                await Task.CompletedTask;

                if (!response)
                {
                    logger.LogError(EventIds.ProductFileIsNotCreated.ToEventId(), "Error in creating sales catalogue data product.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", batchId, correlationId);
                    throw new FulfilmentException(EventIds.ProductFileIsNotCreated.ToEventId());
                }
                return true;
            }
            else
            {
                logger.LogError(EventIds.SalesCatalogueServiceCatalogueDataNonOkResponse.ToEventId(), "Error in sales catalogue service catalogue end point for product.txt responded with {ResponseCode} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", salesCatalogueDataResponse.ResponseCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.SalesCatalogueServiceCatalogueDataNonOkResponse.ToEventId());
            }
        }

        private string GetCrcString(string fullFilePath)
        {
            var crcHash = Crc32CheckSumProvider.Instance.Compute(fileSystemHelper.ReadAllBytes(fullFilePath));
            return crcHash.ToString("X").PadLeft(crcLength, '0');
        }

        private string GetIssueAndUpdateDate(Dates dateResponse)
        {
            string UADT = dateResponse.UpdateApplicationDate == null ? null : dateResponse.UpdateApplicationDate.Value.ToString("yyyyMMdd");
            string ISDT = dateResponse.IssueDate.ToString("yyyyMMdd");
            return (UADT != null) ? $"UADT={UADT},ISDT={ISDT};" : $"ISDT={ISDT};";
        }
    }
}