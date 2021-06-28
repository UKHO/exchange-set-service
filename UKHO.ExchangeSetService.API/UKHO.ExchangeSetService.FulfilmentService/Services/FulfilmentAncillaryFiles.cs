using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.Torus.Enc.Core.EncCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentAncillaryFiles : IFulfilmentAncillaryFiles
    {
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;

        public FulfilmentAncillaryFiles(IOptions<FileShareServiceConfiguration> fileShareServiceConfig)
        {
            this.fileShareServiceConfig = fileShareServiceConfig;
        }

        public async Task CreateCatalogFile(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData)
        {
            var catBuilder = new Catalog031BuilderFactory().Create();
            var readMeFileName = Path.Combine(exchangeSetRootPath, fileShareServiceConfig.Value.ReadMeFileName);

            catBuilder.Add(new CatalogEntry()
            {
                FileLocation = fileShareServiceConfig.Value.CatalogFileName,
                Implementation = "ASC"
            });
            
            if (File.Exists(readMeFileName))
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

                var cat031Bytes = catBuilder.WriteCatalog(fileShareServiceConfig.Value.ExchangeSetFileFolder);
                CheckCreateFolderPath(exchangeSetRootPath);

                var outputFileName = Path.Combine(exchangeSetRootPath, fileShareServiceConfig.Value.CatalogFileName);
                if (File.Exists(outputFileName))
                    File.Delete(outputFileName);

                using (var output = File.OpenWrite(outputFileName))
                {
                    output.Write(cat031Bytes, 0, cat031Bytes.Length);
                }
            }
            await Task.CompletedTask;
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

        private static void CheckCreateFolderPath(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }
    }
}
