using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentAncillaryFiles
    {
        Task<bool> CreateSerialEncFile(BatchInfo batchInfo);
        Task<bool> CreateCatalogFile(BatchInfo batchInfo, IEnumerable<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse);
        Task<bool> CreateProductFile(BatchInfo batchInfo, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime, bool encryption = true);
        Task<bool> CreateMediaFile(BatchInfo batchInfo, string baseNumber);
        Task<bool> CreateLargeMediaSerialEncFile(BatchInfo batchInfo, string baseNumber, string lastBaseDirectoryNumber);
        Task<bool> CreateLargeExchangeSetCatalogFile(BatchInfo batchInfo, IEnumerable<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse);
        Task<bool> CreateEncUpdateCsv(BatchInfo batchInfo, SalesCatalogueDataResponse salesCatalogueDataResponse);
        Task<bool> CreateSerialAioFile(BatchInfo batchInfo, SalesCatalogueDataResponse salesCatalogueDataResponse);
    }
}