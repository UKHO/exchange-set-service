// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.FileCreation
{
    public interface IFileBuilder
    {
        Task CreateAncillaryFiles(string batchId, string exchangeSetPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueProductResponse salecatalogueProductResponse, DateTime scsRequestDateTime, SalesCatalogueDataResponse salesCatalogueEssDataResponse, bool encryption);
        Task<bool> CreateAncillaryFilesForAio(string batchId, string aioExchangeSetPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime, SalesCatalogueProductResponse salesCatalogueProductResponse, List<FulfilmentDataResponse> listFulfilmentAioData);
        Task<bool> CreateSerialAioFile(string batchId, string aioExchangeSetPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse);
        Task<bool> CreateProductFileForAio(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime);
        Task<bool> CreateCatalogFileForAio(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse);
        Task<bool> CreateCatalogFile(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse);
        Task<bool> CreateProductFile(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime, bool encryption);
        Task CreateSerialEncFile(string batchId, string exchangeSetPath, string correlationId);
        Task<bool> CreateLargeMediaSerialEncFile(string batchId, string exchangeSetPath, string rootfolder, string correlationId);
        Task<bool> CreateLargeMediaExchangesetCatalogFile(string batchId, string exchangeSetPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse);
    }
}
