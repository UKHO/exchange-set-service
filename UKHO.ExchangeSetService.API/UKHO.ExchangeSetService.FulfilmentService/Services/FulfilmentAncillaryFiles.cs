using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.Torus.Enc.Core.EncCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentAncillaryFiles : IFulfilmentAncillaryFiles
    {
        private readonly ILogger<FulfilmentAncillaryFiles> logger;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;        
        private readonly IFileSystemHelper fileSystemHelper;

        public FulfilmentAncillaryFiles(ILogger<FulfilmentAncillaryFiles> logger, IOptions<FileShareServiceConfiguration> fileShareServiceConfig, IFileSystemHelper fileSystemHelper)
        {
            this.logger = logger;
            this.fileShareServiceConfig = fileShareServiceConfig;            
            this.fileSystemHelper = fileSystemHelper;
        }

        public async Task<bool> CreateCatalogFile(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData)
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
                int length = 2;

                foreach (var listItem in listFulfilmentData)
                {
                    foreach (var item in listItem.Files)
                    {
                        catBuilder.Add(new CatalogEntry()
                        {
                            FileLocation = $"{listItem.ProductName.Substring(0, length)}/{listItem.ProductName}/{listItem.EditionNumber}/{listItem.UpdateNumber}/{item.Filename}",
                            FileLongName = "",
                            Implementation = GetMimeType(Path.GetExtension(item.Filename.ToLower()), item.MimeType.ToLower())
                        });
                    }
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
                logger.LogError(EventIds.CatalogueFileIsNotCreated.ToEventId(), "Error in creating catalogue.031 file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                return false;
            }
        }

        private string GetMimeType(string fileExtension, string mimeType)
        {
            switch (mimeType)
            {
                case "application/s63":
                    return "BIN";

                case "text/plain":
                    if (fileExtension != ".txt")
                        return "ASC";
                    else
                        return "TXT";

                case "image/tiff":
                    return "TIF";

                default:
                    return "TXT";
            }
        }
    }
}
