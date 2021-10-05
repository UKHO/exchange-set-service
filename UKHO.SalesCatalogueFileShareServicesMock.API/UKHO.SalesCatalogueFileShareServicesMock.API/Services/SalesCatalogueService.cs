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
            var folderDetails = Path.Combine(Directory.GetCurrentDirectory(), $"Data\\SalesCatalogueService\\SCSResponse.json");
            var myJsonString = File.ReadAllText(folderDetails);
            var jsonObj = JsonSerializer.Deserialize<List<SalesCatalogueResponse>>(myJsonString, Options);
            var selectedProductIdentifier = jsonObj.FirstOrDefault(a => a.Id.ToLowerInvariant() == productIdentifiers.ToLowerInvariant());
            return selectedProductIdentifier;
        }

        public SalesCatalogueResponse GetProductVersion(string productVersions)
        {
            var folderDetails = Path.Combine(Directory.GetCurrentDirectory(), $"Data\\SalesCatalogueService\\SCSResponse.json");
            var myJsonString = File.ReadAllText(folderDetails);
            var jsonObj = JsonSerializer.Deserialize<List<SalesCatalogueResponse>>(myJsonString, Options);
            var selectedProductVersion = jsonObj.FirstOrDefault(a => a.Id.ToLowerInvariant() == productVersions.ToLowerInvariant());
            return selectedProductVersion;
        }

        public SalesCatalogueResponse GetProductSinceDateTime(string sinceDateTime)
        {
            if (!string.IsNullOrWhiteSpace(sinceDateTime))
            {
                var folderDetails = Path.Combine(Directory.GetCurrentDirectory(), $"Data\\SalesCatalogueService\\SCSResponse.json");
                var myJsonString = File.ReadAllText(folderDetails);
                var jsonObj = JsonSerializer.Deserialize<List<SalesCatalogueResponse>>(myJsonString, Options);
                var selectedProductSinceDateTime = jsonObj.FirstOrDefault(a => a.Id.ToLowerInvariant() == "sinceDateTime".ToLowerInvariant());
                return selectedProductSinceDateTime; 
            }
            return null;
        }
    }
}
