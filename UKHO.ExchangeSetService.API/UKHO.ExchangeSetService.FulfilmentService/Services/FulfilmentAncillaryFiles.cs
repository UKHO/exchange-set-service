using CsvHelper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.Torus.Core;
using UKHO.Torus.Enc.Core;
using UKHO.Torus.Enc.Core.EncCatalogue;

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

                var orderPreference = new List<Tuple<string, string>> {
                    new Tuple<string, string>("application/s63", "BIN"),
                    new Tuple<string, string>("text/plain", "ASC"),
                    new Tuple<string, string>("text/plain", "TXT"),
                    new Tuple<string, string>("image/tiff", "TIF") };

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
                var boundingRectangle = new BoundingRectangle();

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

            if (salescatalogProductResponse?.Dates != null)
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

        public async Task<bool> CreateProductFile(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime)
        {
            if (salesCatalogueDataResponse.ResponseCode == HttpStatusCode.OK)
            {
                string fileName = fileShareServiceConfig.Value.ProductFileName;
                string filePath = Path.Combine(exchangeSetInfoPath, fileName);

                var productsBuilder = new ProductListBuilder
                {
                    UseDefaultOutputTime = false
                };

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
                        LastUpdateNumberForPreviousEdition = product.LastUpdateNumberPreviousEdition,
                        BaseCellLocation = product.BaseCellLocation,
                        CancelledCellReplacements = String.Join(";", product.CancelledCellReplacements)
                    });

                var content = productsBuilder.WriteProductsList(scsRequestDateTime);
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

        public async Task<bool> CreateEncUpdateCsv(SalesCatalogueDataResponse salesCatalogueDataResponse, string filePath, string batchId, string correlationId)
        {
            string file = Path.Combine(filePath, "ENC Update List.csv");
            IEnumerable<ProductsCsvDetails> productsCsvDetails = salesCatalogueDataResponse.ResponseBody.OrderBy(p => p.ProductName).Select(x => new ProductsCsvDetails
            {
                ProductName = x.ProductName,
                EditionNumber = x.BaseCellEditionNumber,
                UpdateNumber = x.LatestUpdateNumber,
                EditionIssueDate = x.BaseCellIssueDate.ToString("dd/MM/yyyy"),
                UpdateIssueDate = x.IssueDateLatestUpdate?.ToString("dd/MM/yyyy")
            });

            WriteCsvFile(file, productsCsvDetails);

            var response = fileSystemHelper.CheckFileExists(file);
            await Task.CompletedTask;

            if (!response)
            {
                logger.LogError(EventIds.ENCupdateCSVFileIsNotCreated.ToEventId(), "Error in creating enc update list csv file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", batchId, correlationId);
                throw new FulfilmentException(EventIds.ENCupdateCSVFileIsNotCreated.ToEventId());
            }
            return true;
        }

        private void WriteCsvFile(string file, IEnumerable<ProductsCsvDetails> productsCsvDetails)
        {
            using TextWriter writer = fileSystemHelper.WriteStream(file);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(productsCsvDetails);
            csv.WriteField(":ECS");
            csv.Flush();
        }

        private string GetCrcString(string fullFilePath)
        {
            var crcHash = Crc32CheckSumProvider.Instance.Compute(fileSystemHelper.ReadAllBytes(fullFilePath));
            return crcHash.ToString("X").PadLeft(crcLength, '0');
        }

        private string GetIssueAndUpdateDate(Dates dateResponse)
        {
            string UADT = dateResponse.UpdateApplicationDate?.ToString("yyyyMMdd");
            string ISDT = dateResponse.IssueDate.ToString("yyyyMMdd");
            return (UADT != null) ? $"UADT={UADT},ISDT={ISDT};" : $"ISDT={ISDT};";
        }

        public async Task<bool> CreateMediaFile(string batchId, string folderpath, string correlationId, string baseNumber)
        {
            bool isMediaFileCreated = false;
            if (!string.IsNullOrWhiteSpace(folderpath))
            {
                string mediaFilePath = Path.Combine(folderpath, "MEDIA.TXT");
                fileSystemHelper.CheckAndCreateFolder(folderpath);
                int weekNumber = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow);
                var basefolders = fileSystemHelper.GetDirectoryInfo(folderpath)
                       .Where(di => di.Name.StartsWith("B") && di.Name.Length <= 3 && CommonHelper.IsNumeric(di.Name[^(di.Name.Length - 1)..]));

                string mediaFileContent = $"GBWK{weekNumber:D2}_{DateTime.UtcNow:yy}   {DateTime.UtcNow.Year:D4}{DateTime.UtcNow.Month:D2}{DateTime.UtcNow.Day:D2}BASE      M0{baseNumber}X02";
                mediaFileContent += Environment.NewLine;
                mediaFileContent += $"M{baseNumber},'UKHO AVCS Week{weekNumber:D2}_{DateTime.UtcNow:yy} Base Media','DVD_SERVICE'";
                mediaFileContent += Environment.NewLine;
                var sb = new StringBuilder();
                foreach (var directory in basefolders)
                {
                    var baseFolderName = directory.Name;
                    var baseDigit = baseFolderName.Remove(0, 1);
                    string path = Path.Combine(directory.ToString(), fileShareServiceConfig.Value.EncRoot);
                    string[] subdirectoryEntries = fileSystemHelper.GetDirectories(path);

                    var countryCodes = new List<string>();
                    foreach (string codes in subdirectoryEntries)
                    {
                        var dirName = new DirectoryInfo(codes).Name;
                        countryCodes.Add(dirName);
                    }
                    string content = $"M{baseNumber};{baseFolderName},{DateTime.UtcNow.Year:D4}{DateTime.UtcNow.Month:D2}{DateTime.UtcNow.Day:D2},'AVCS Volume{baseDigit}','ENC data for producers {string.Join(", ", countryCodes)}',,";
                    sb.AppendLine(content);

                }
                mediaFileContent += sb.ToString();
                fileSystemHelper.CreateFileContent(mediaFilePath, mediaFileContent);
                await Task.CompletedTask;

                if (fileSystemHelper.CheckFileExists(mediaFilePath))
                    isMediaFileCreated = true;
                else
                {
                    logger.LogError(EventIds.MediaFileIsNotCreated.ToEventId(), "Error in creating media.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} - Invalid Exchange Set Path", batchId, correlationId);
                    throw new FulfilmentException(EventIds.MediaFileIsNotCreated.ToEventId());
                }
            }
            return isMediaFileCreated;
        }

        public async Task<bool> CreateLargeMediaSerialEncFile(string batchId, string exchangeSetPath, string correlationId, string baseNumber, string lastBaseDirectoryNumber)
        {
            bool isSerialEncFileCreated = false;
            if (!string.IsNullOrWhiteSpace(exchangeSetPath))
            {
                string serialFilePath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.SerialFileName);
                fileSystemHelper.CheckAndCreateFolder(exchangeSetPath);
                int weekNumber = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow);

                var serialFileContent = $"GBWK{weekNumber:D2}-{DateTime.UtcNow:yy}   {DateTime.UtcNow.Year:D4}{DateTime.UtcNow.Month:D2}{DateTime.UtcNow.Day:D2}BASE      {2:D2}.00B0{baseNumber}X0{lastBaseDirectoryNumber}\x0b\x0d\x0a";

                fileSystemHelper.CreateFileContent(serialFilePath, serialFileContent);
                await Task.CompletedTask;

                if (fileSystemHelper.CheckFileExists(serialFilePath))
                    isSerialEncFileCreated = true;
                else
                {
                    logger.LogError(EventIds.SerialFileIsNotCreated.ToEventId(), "Error in creating large media exchange set serial.enc file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} - Invalid Exchange Set Path", batchId, correlationId);
                    throw new FulfilmentException(EventIds.SerialFileIsNotCreated.ToEventId());
                }
            }
            return isSerialEncFileCreated;
        }

        public async Task<bool> CreateLargeExchangeSetCatalogFile(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse)
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

                var orderPreference = new List<Tuple<string, string>> {
                    new Tuple<string, string>("application/s63", "BIN"),
                    new Tuple<string, string>("text/plain", "ASC"),
                    new Tuple<string, string>("text/plain", "TXT"),
                    new Tuple<string, string>("image/tiff", "TIF") };

                foreach (var listItem in listFulfilmentData)
                {
                    CreateLargeExchangeSetCatalogEntry(listItem, orderPreference, catBuilder, salesCatalogueDataResponse, salesCatalogueProductResponse, exchangeSetRootPath, batchId, correlationId);
                }
            }
            else
            {
                logger.LogError(EventIds.CatalogFileIsNotCreated.ToEventId(), "Error in creating catalog.031 file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                throw new FulfilmentException(EventIds.CatalogFileIsNotCreated.ToEventId());
            }

            IDirectoryInfo directoryInfo = fileSystemHelper.GetParent(Path.GetDirectoryName(exchangeSetRootPath));
            var path = directoryInfo.Name;
            var cat031Bytes = catBuilder.WriteCatalog(path);
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

        private void CreateLargeExchangeSetCatalogEntry(FulfilmentDataResponse listItem, List<Tuple<string, string>> orderPreference, ICatalog031Builder catBuilder, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse, string exchangeSetRootPath, string batchId, string correlationId)
        {
            int length = 2;
            listItem.Files = listItem.Files.OrderByDescending(
                                item => Enumerable.Reverse(orderPreference).ToList().IndexOf(new Tuple<string, string>(item.MimeType.ToLower(), GetMimeType(item.Filename.ToLower(), item.MimeType.ToLower(), batchId, correlationId))));

            foreach (var item in listItem.Files)
            {
                string fileLocation = Path.Combine(listItem.ProductName.Substring(0, length), listItem.ProductName, listItem.EditionNumber.ToString(), item.Filename);
                string mimeType = GetMimeType(item.Filename.ToLower(), item.MimeType.ToLower(), batchId, correlationId);
                string comment = string.Empty;
                var boundingRectangle = new BoundingRectangle();

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

        public async Task<bool> CreateSerialAioFile(string batchId, string aioExchangeSetPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse)
        {
            bool checkSerialAioFileCreated = false;
            if (!string.IsNullOrWhiteSpace(aioExchangeSetPath))
            {
                string serialFilePath = Path.Combine(aioExchangeSetPath, fileShareServiceConfig.Value.SerialAioFileName);

                fileSystemHelper.CheckAndCreateFolder(aioExchangeSetPath);

                string cdType = GetCdType(salesCatalogueDataResponse.ResponseBody, aioExchangeSetPath); //Get cdType BASE/UPDATE
                int weekNumber = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow);

                var serialFileContent = $"GBWK{weekNumber:D2}-{DateTime.UtcNow:yy}   {DateTime.UtcNow.Year:D4}{DateTime.UtcNow.Month:D2}{DateTime.UtcNow.Day:D2}{cdType}      {2:D2}.00\x0b\x0d\x0a";

                fileSystemHelper.CreateFileContent(serialFilePath, serialFileContent);
                await Task.CompletedTask;

                if (fileSystemHelper.CheckFileExists(serialFilePath))
                    checkSerialAioFileCreated = true;
                else
                {
                    logger.LogError(EventIds.SerialAioFileIsNotCreated.ToEventId(), "Error in creating serial.aio file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} - Invalid Exchange Set Path", batchId, correlationId);
                    throw new FulfilmentException(EventIds.SerialAioFileIsNotCreated.ToEventId());
                }
            }
            return checkSerialAioFileCreated;
        }

        //Added this method temporarily. We will use existing method implementation Ref. CreateCatalogFile() while adding contents to this file.
        public async Task<bool> CreateCatalogFileForAio(string batchId, string exchangeSetRootPath, string correlationId)
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

            var cat031Bytes = catBuilder.WriteCatalog(fileShareServiceConfig.Value.AioExchangeSetFileFolder);

            fileSystemHelper.CheckAndCreateFolder(exchangeSetRootPath);
            fileSystemHelper.CreateFile(outputFileName);

            fileSystemHelper.CreateFileContentWithBytes(outputFileName, cat031Bytes);

            await Task.CompletedTask;

            if (fileSystemHelper.CheckFileExists(outputFileName))
                return true;
            else
            {
                logger.LogError(EventIds.CatalogFileIsNotCreated.ToEventId(), "Error in creating catalog.031 file for aio exchange set for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                throw new FulfilmentException(EventIds.CatalogFileIsNotCreated.ToEventId());
            }
        }

        /// <summary>
        /// return cdType, BASE when base '0' folder and product with '.000' extension found in Aio Exchangeset
        /// </summary>
        /// <param name="salesCatalogueDataProductResponses"></param>
        /// <param name="aioExchangeSetPath"></param>
        /// <returns></returns>
        private string GetCdType(List<SalesCatalogueDataProductResponse> salesCatalogueDataProductResponses, string aioExchangeSetPath)
        {
            string cdType = "UPDATE";
            foreach (var response in salesCatalogueDataProductResponses)
            {
                string path = Path.Combine(aioExchangeSetPath, fileShareServiceConfig.Value.EncRoot, response.ProductName[..2], 
                                           response.ProductName, Convert.ToString(response.BaseCellEditionNumber), "0",
                                           response.ProductName + ".000");
                if (fileSystemHelper.CheckFileExists(path))
                {
                    cdType = "BASE";
                    break;
                }
            }
            return cdType;
        }
    }
}