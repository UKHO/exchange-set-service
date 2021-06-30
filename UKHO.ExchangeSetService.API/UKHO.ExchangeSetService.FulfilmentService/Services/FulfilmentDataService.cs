using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly IAzureBlobStorageService azureBlobStorageService;
        private readonly IFulfilmentFileShareService fulfilmentFileShareService;
        private readonly ILogger<FulfilmentDataService> logger;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;
        private readonly IConfiguration configuration;
        private readonly IFulfilmentAncillaryFiles fulfilmentAncillaryFiles;
        private readonly IFulfilmentSalesCatalogueService fulfilmentSalesCatalogueService;


        public FulfilmentDataService(IAzureBlobStorageService azureBlobStorageService,
                                    IFulfilmentFileShareService fulfilmentFileShareService,
                                    ILogger<FulfilmentDataService> logger,
                                    IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                    IConfiguration configuration,
                                    IFulfilmentAncillaryFiles fulfilmentAncillaryFiles,
                                    IFulfilmentSalesCatalogueService fulfilmentSalesCatalogueService)
        {
            this.azureBlobStorageService = azureBlobStorageService;
            this.fulfilmentFileShareService = fulfilmentFileShareService;
            this.logger = logger;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.configuration = configuration;
            this.fulfilmentAncillaryFiles = fulfilmentAncillaryFiles;
            this.fulfilmentSalesCatalogueService = fulfilmentSalesCatalogueService;
        }

        public async Task<string> CreateExchangeSet(SalesCatalogueServiceResponseQueueMessage message)
        {
            string homeDirectoryPath = configuration["HOME"];
            var exchangeSetPath = Path.Combine(homeDirectoryPath, DateTime.UtcNow.ToString("ddMMMyyyy"), message.BatchId, fileShareServiceConfig.Value.ExchangeSetFileFolder);
            var exchangeSetRootPath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.EncRoot);

            var response = await DownloadSalesCatalogueResponse(message);
            if (response.Products != null && response.Products.Any())
            {
                int parallelSearchTaskCount = fileShareServiceConfig.Value.ParallelSearchTaskCount;
                int productGroupCount = response.Products.Count % parallelSearchTaskCount == 0 ? response.Products.Count / parallelSearchTaskCount : (response.Products.Count / parallelSearchTaskCount) + 1;
                var productsList = ConfigHelper.SplitList((response.Products), productGroupCount);

                var tasks = productsList.Select(async item =>
                {
                    await QueryAndDownloadFileShareServiceFiles(message, item, exchangeSetRootPath);
                });
                await Task.WhenAll(tasks);
            }

            await CreateAncillaryFiles(message.BatchId, exchangeSetPath, message.CorrelationId);
            return "Received Fulfilment Data Successfully!!!!";
        }

        public async Task<SalesCatalogueProductResponse> DownloadSalesCatalogueResponse(SalesCatalogueServiceResponseQueueMessage message)
        {
            return await azureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, message.CorrelationId);
        }

        public async Task QueryAndDownloadFileShareServiceFiles(SalesCatalogueServiceResponseQueueMessage message, List<Products> products, string exchangeSetRootPath)
        {
            logger.LogInformation(EventIds.QueryFileShareServiceRequestStart.ToEventId(), "Query File share service request started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId,message.CorrelationId);
            var searchBatchResponse = await fulfilmentFileShareService.QueryFileShareServiceData(products, message.CorrelationId);
            logger.LogInformation(EventIds.QueryFileShareServiceRequestCompleted.ToEventId(), "Query File share service request completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId,message.CorrelationId);

            if (searchBatchResponse != null && searchBatchResponse.Any())
            {
                logger.LogInformation(EventIds.DownloadFileShareServiceFilesStart.ToEventId(), "Download File share service request started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId,message.CorrelationId);
                await fulfilmentFileShareService.DownloadFileShareServiceFiles(message, searchBatchResponse, exchangeSetRootPath);
                logger.LogInformation(EventIds.DownloadFileShareServiceFilesCompleted.ToEventId(), "Download File share service request completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId,message.CorrelationId);
            }
        }
        private async Task CreateAncillaryFiles(string batchId, string exchangeSetPath, string correlationId)
        {
            var exchangeSetRootPath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.EncRoot);
            var exchangeSetInfoPath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.Info);
            SalesCatalogueDataResponse salesCatalogueDataResponse = await GetSalesCatalogueDataResponse(batchId, correlationId);

            CreateProductFile(batchId, exchangeSetInfoPath,correlationId, salesCatalogueDataResponse);
            await DownloadReadMeFile(batchId, exchangeSetRootPath, correlationId);
        }

        public async Task DownloadReadMeFile(string batchId, string exchangeSetRootPath, string correlationId)
        {
            logger.LogInformation(EventIds.QueryFileShareServiceRequestStart.ToEventId(), "Query File share service request started for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);           
            string readMeFilePath = await fulfilmentFileShareService.SearchReadMeFilePath(batchId, correlationId);
            logger.LogInformation(EventIds.QueryFileShareServiceRequestCompleted.ToEventId(), "Query File share service request completed for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);

            if (!string.IsNullOrWhiteSpace(readMeFilePath))
            {
                logger.LogInformation(EventIds.DownloadReadMeFileRequestStart.ToEventId(), "Search and download ReadMe Text File start for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                await fulfilmentFileShareService.DownloadReadMeFile(readMeFilePath, batchId, exchangeSetRootPath, correlationId);
                logger.LogInformation(EventIds.DownloadReadMeFileRequestCompleted.ToEventId(), "Search and download ReadMe Text File completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
            }               
        }

        public void CreateProductFile(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse)
        {
            logger.LogInformation(EventIds.CreateProductFileRequestStart.ToEventId(), "Create product file request started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
            fulfilmentAncillaryFiles.CreateProductFile(batchId, exchangeSetInfoPath, correlationId, salesCatalogueDataResponse);
            logger.LogInformation(EventIds.CreateProductFileRequestCompleted.ToEventId(), "Create product file request completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
        }

        public async Task<SalesCatalogueDataResponse> GetSalesCatalogueDataResponse(string batchId, string correlationId)
        {
            SalesCatalogueDataResponse salesCatalogueTypeResponse = await fulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(batchId, correlationId);
            return salesCatalogueTypeResponse;
        }
    }
}
