using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Options;
using UKHO.SalesCatalogueFileShareServicesMock.API.Common;
using UKHO.SalesCatalogueFileShareServicesMock.API.Helpers;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.V2.Enums;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.V2.Response;

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

        public SalesCatalogueDataResponse GetEssData()
        {
            var responseData = FileHelper.ReadJsonFile<SalesCatalogueDataResponse>(salesCatalogueConfiguration.Value.FileDirectoryPath + salesCatalogueConfiguration.Value.ScsCatalogueResponseFile);
            return responseData;
        }

        public V2SalesCatalogueResponse GetProductNames(string productNames)
        {
            var responseData = FileHelper.ReadJsonFile<List<V2SalesCatalogueResponse>>(salesCatalogueConfiguration.Value.V2FileDirectoryPath + salesCatalogueConfiguration.Value.ScsResponseFile);
            var selectedProductNames = responseData?.FirstOrDefault(a => a.Id.ToLowerInvariant() == productNames.ToLowerInvariant());
            return selectedProductNames;
        }

        public V2SalesCatalogueResponse GetV2ProductVersion(string productVersion)
        {
            var responseData = FileHelper.ReadJsonFile<List<V2SalesCatalogueResponse>>(salesCatalogueConfiguration.Value.V2FileDirectoryPath + salesCatalogueConfiguration.Value.ScsResponseFile);
            var selectedProductVersion = responseData?.FirstOrDefault(a => a.Id.ToLowerInvariant() == productVersion.ToLowerInvariant());
            return selectedProductVersion;
        }

        public V2SalesCatalogueResponse GetUpdatesSinceDateTime(string sinceDateTime, string productIdentifier)
        {
            string searchId = string.IsNullOrEmpty(productIdentifier) ? "updatesSince" : $"updatesSince-{productIdentifier}";
            var responseData = FileHelper.ReadJsonFile<List<V2SalesCatalogueResponse>>(salesCatalogueConfiguration.Value.V2FileDirectoryPath + salesCatalogueConfiguration.Value.ScsResponseFile);
            var selectedProductSinceDateTime = responseData?.FirstOrDefault(a => a.Id.ToLowerInvariant() == searchId.ToLowerInvariant());
            return selectedProductSinceDateTime;
        }

        public bool ValidateSinceDateTime(string sinceDateTime)
        {
            DateTime currentDateTime = DateTime.UtcNow;
            if (string.IsNullOrEmpty(sinceDateTime))
            {
                return false;
            }

            if (!DateTime.TryParse(sinceDateTime, out DateTime parsedDateTime))
            {
                return false;
            }

            if (parsedDateTime.Date >= currentDateTime.Date)
            {
                return false;
            }

            if ((currentDateTime - parsedDateTime).TotalDays > 28)
            {
                return false;
            }
            return true;
        }

        public bool ValidateProductIdentifier(string productIdentifier)
        {
            if (string.IsNullOrEmpty(productIdentifier) || Enum.TryParse<S100ProductType>(productIdentifier, out _))
            {
                return true;
            }
            return false;
        }
    }
}
