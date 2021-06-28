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

        public async Task<bool> CreateCatalogFile(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData)
        {
            var catBuilder = new Catalog031BuilderFactory().Create();
            var readMeFileName = Path.Combine(exchangeSetRootPath, fileShareServiceConfig.Value.ReadMeFileName);
            var outputFileName = Path.Combine(exchangeSetRootPath, fileShareServiceConfig.Value.CatalogFileName);

            if (File.Exists(outputFileName))
                File.Delete(outputFileName);

            if (listFulfilmentData != null && listFulfilmentData.Any())
            {
                int length = 2;

                if (File.Exists(readMeFileName))
                {
                    catBuilder.Add(new CatalogEntry()
                    {
                        FileLocation = fileShareServiceConfig.Value.ReadMeFileName,
                        Implementation = "TXT"
                    });
                }

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

                using (var output = File.OpenWrite(outputFileName))
                {
                    output.Write(cat031Bytes, 0, cat031Bytes.Length);
                }
            }
            await Task.CompletedTask;
            return File.Exists(outputFileName);
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
