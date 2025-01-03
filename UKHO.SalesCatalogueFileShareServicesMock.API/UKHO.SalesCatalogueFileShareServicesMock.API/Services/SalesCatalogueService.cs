using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Options;
using UKHO.SalesCatalogueFileShareServicesMock.API.Common;
using UKHO.SalesCatalogueFileShareServicesMock.API.Helpers;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.V2.Enums;

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

        public Models.V2.Response.SalesCatalogueResponse GetUpdatesSinceDateTime(string sinceDateTime, string productIdentifier)
        {
            string searchId= $"updatesSince-{productIdentifier}";
            var responseData = FileHelper.ReadJsonFile<List<Models.V2.Response.SalesCatalogueResponse>>(salesCatalogueConfiguration.Value.V2FileDirectoryPath + salesCatalogueConfiguration.Value.ScsResponseFile);
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

            if (!DateTime.TryParseExact(sinceDateTime, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDateTime))
            {
                return false;
            }

            if (parsedDateTime >= currentDateTime)
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
            if (string.IsNullOrEmpty(productIdentifier) || !Enum.TryParse<S100ProductType>(productIdentifier, out _))
            {
                return false;
            }
            return true;
        }
    }
}
