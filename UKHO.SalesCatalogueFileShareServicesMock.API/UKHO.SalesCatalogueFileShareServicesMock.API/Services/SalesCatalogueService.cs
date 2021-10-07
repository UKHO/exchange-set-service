using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using UKHO.SalesCatalogueFileShareServicesMock.API.Common;
using UKHO.SalesCatalogueFileShareServicesMock.API.Helpers;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Services
{
    public class SalesCatalogueService
    {
        private readonly IOptions<SalesCatalogueConfiguration> salesCatalogueConfiguration;

        public SalesCatalogueService(IOptions<SalesCatalogueConfiguration> salesCatalogueConfiguration)
        {
            this.salesCatalogueConfiguration = salesCatalogueConfiguration;
        }

        public SalesCatalogueResponse GetProductIdentifier(string productIdentifiers)
        {
            var responseData = FileHelper.ReadJsonFile<List<SalesCatalogueResponse>>(salesCatalogueConfiguration.Value.FileDirectoryPath + salesCatalogueConfiguration.Value.ScsResponseFile);
            var selectedProductIdentifier = responseData?.FirstOrDefault(a => a.Id.ToLowerInvariant() == productIdentifiers.ToLowerInvariant());
            return selectedProductIdentifier;
        }

        public SalesCatalogueResponse GetProductVersion(string productVersions)
        {
            var responseData = FileHelper.ReadJsonFile<List<SalesCatalogueResponse>>(salesCatalogueConfiguration.Value.FileDirectoryPath + salesCatalogueConfiguration.Value.ScsResponseFile);
            var selectedProductVersion = responseData?.FirstOrDefault(a => a.Id.ToLowerInvariant() == productVersions.ToLowerInvariant());
            return selectedProductVersion;
        }

        public SalesCatalogueResponse GetProductSinceDateTime(string sinceDateTime)
        {
            if (!string.IsNullOrWhiteSpace(sinceDateTime))
            {
                var responseData = FileHelper.ReadJsonFile<List<SalesCatalogueResponse>>(salesCatalogueConfiguration.Value.FileDirectoryPath + salesCatalogueConfiguration.Value.ScsResponseFile);
                var selectedProductSinceDateTime = responseData?.FirstOrDefault(a => a.Id.ToLowerInvariant() == "sinceDateTime".ToLowerInvariant());
                return selectedProductSinceDateTime; 
            }
            return null;
        }
    }
}
