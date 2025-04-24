using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.FileBuilders
{
    public interface IFileBuilder
    {
        Task CreateAncillaryFiles(BatchInfo batchInfo, IEnumerable<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueProductResponse salecatalogueProductResponse, DateTime scsRequestDateTime, SalesCatalogueDataResponse salesCatalogueEssDataResponse, bool encryption);
        Task<bool> CreateAncillaryFilesForAio(BatchInfo batchInfo, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime, SalesCatalogueProductResponse salesCatalogueProductResponse, IEnumerable<FulfilmentDataResponse> listFulfilmentAioData);
        Task<bool> CreateSerialAioFile(BatchInfo batchInfo, SalesCatalogueDataResponse salesCatalogueDataResponse);
        Task<bool> CreateProductFileForAio(BatchInfo batchInfo, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime);
        Task<bool> CreateCatalogFileForAio(BatchInfo batchInfo, IEnumerable<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse);
        Task<bool> CreateCatalogFile(BatchInfo batchInfo, IEnumerable<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse);
        Task<bool> CreateProductFile(BatchInfo batchInfo, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime, bool encryption);
        Task CreateSerialEncFile(BatchInfo batchInfo);
        Task<bool> CreateLargeMediaSerialEncFile(BatchInfo batchInfo, string rootfolder);
        Task<bool> CreateLargeMediaExchangesetCatalogFile(BatchInfo batchInfo, IEnumerable<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse);
    }
}
