using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Services
{
    public class SalesCatalogueService
    {
        private JsonSerializerOptions Options { get; set; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public SalesCatalogueService()
        {
        }

        public SalesCatalogueResponse GetProductIdentifier(string productIdentifiers)
        {
            var folderDetails = Path.Combine(Directory.GetCurrentDirectory(), $"Data\\SalesCatalogueService\\ProductIdentifierResponse.json");
            var myJsonString = File.ReadAllText(folderDetails);
            var jsonObj = JsonSerializer.Deserialize<List<SalesCatalogueResponse>>(myJsonString, Options);
            var selectedProductIdentifier = jsonObj.FirstOrDefault(a => a.Id.ToLowerInvariant() == productIdentifiers.ToLowerInvariant());
            return selectedProductIdentifier;
        }

        public SalesCatalogueResponse GetProductVersion(string productIdentifiers)
        {
            var folderDetails = Path.Combine(Directory.GetCurrentDirectory(), $"Data\\SalesCatalogueService\\ProductVersionResponse.json");
            var myJsonString = File.ReadAllText(folderDetails);
            var jsonObj = JsonSerializer.Deserialize<List<SalesCatalogueResponse>>(myJsonString, Options);
            var selectedProductIdentifier = jsonObj.FirstOrDefault(a => a.Id.ToLowerInvariant() == productIdentifiers.ToLowerInvariant());
            return selectedProductIdentifier;
        }

        public SalesCatalogueResponse GetProductSinceDateTime(string sinceDateTime)
        {
            var folderDetails = Path.Combine(Directory.GetCurrentDirectory(), $"Data\\SalesCatalogueService\\SinceDateTimeResponse.json");
            var myJsonString = File.ReadAllText(folderDetails);
            var jsonObj = JsonSerializer.Deserialize<List<SalesCatalogueResponse>>(myJsonString, Options);
            var selectedProductIdentifier = jsonObj.FirstOrDefault();
            return selectedProductIdentifier;
        }
    }
}
