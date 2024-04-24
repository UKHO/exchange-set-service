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
        Task<(bool, List<(string fileName, string filePath, byte[] fileContent)>)> CreateSerialEncFile1(string batchId, string correlationId);
        Task<bool> CreateCatalogFile(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse);
        Task<List<(string fileName, string filePath, byte[] fileContent)>> CreateCatalogFile1(string batchId, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse, List<(string fileName, string filePath, byte[] fileContent)> lstFiles);
        Task<bool> CreateProductFile(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime);
        Task<(bool, List<(string fileName, string filePath, byte[] fileContent)>)> CreateProductFile1(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime);
        Task<bool> CreateMediaFile(string batchId, string folderpath, string correlationId, string baseNumber);
        Task<bool> CreateLargeMediaSerialEncFile(string batchId, string exchangeSetPath, string correlationId, string baseNumber, string lastBaseDirectoryNumber);
        Task<bool> CreateLargeExchangeSetCatalogFile(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse);
        Task<bool> CreateEncUpdateCsv(SalesCatalogueDataResponse salesCatalogueDataResponse, string filePath, string batchId, string correlationId);
        Task<bool> CreateSerialAioFile(string batchId, string aioExchangeSetPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse);
    }
}