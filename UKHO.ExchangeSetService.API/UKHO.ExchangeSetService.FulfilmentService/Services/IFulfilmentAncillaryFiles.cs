using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentAncillaryFiles
    {
        Task<bool> CreateSerialEncFile(string batchId, string exchangeSetPath, string correlationId);
        Task<bool> CreateCatalogFile(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse);
        Task<bool> CreateProductFile(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime);
        Task<bool> CreateMediaFile(string batchId, string folderpath, string correlationId, string baseNumber);
        Task<bool> CreateLargeMediaSerialEncFile(string batchId, string exchangeSetPath, string correlationId, string baseNumber, string lastBaseDirectoryNumber);
        Task<bool> CreateLargeExchangeSetCatalogFile(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse);
        Task<bool> CreateEncUpdateCsv(SalesCatalogueDataResponse salesCatalogueDataResponse, string filePath, string batchId, string correlationId);
    }
}