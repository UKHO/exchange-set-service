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
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
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

        public FulfilmentDataService(IAzureBlobStorageService azureBlobStorageService,
                                    IFulfilmentFileShareService fulfilmentFileShareService,
                                    ILogger<FulfilmentDataService> logger,
                                    IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                    IConfiguration configuration,
                                    IFulfilmentAncillaryFiles fulfilmentAncillaryFiles)
        {
            this.azureBlobStorageService = azureBlobStorageService;
            this.fulfilmentFileShareService = fulfilmentFileShareService;
            this.logger = logger;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.configuration = configuration;
            this.fulfilmentAncillaryFiles = fulfilmentAncillaryFiles;
        }

        public async Task<string> CreateExchangeSet(SalesCatalogueServiceResponseQueueMessage message)
        {
            string homeDirectoryPath = configuration["HOME"];
            var exchangeSetPath = Path.Combine(homeDirectoryPath, DateTime.UtcNow.ToString("ddMMMyyyy"), message.BatchId, fileShareServiceConfig.Value.ExchangeSetFileFolder);
            var exchangeSetRootPath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.EncRoot);
            var listFulfilmentData = new List<FulfilmentDataResponse>();

            var response = await DownloadSalesCatalogueResponse(message);
            if (response.Products != null && response.Products.Any())
            {
                int parallelSearchTaskCount = fileShareServiceConfig.Value.ParallelSearchTaskCount;
                int productGroupCount = response.Products.Count % parallelSearchTaskCount == 0 ? response.Products.Count / parallelSearchTaskCount : (response.Products.Count / parallelSearchTaskCount) + 1;
                var productsList = CommonHelper.SplitList((response.Products), productGroupCount);

                var tasks = productsList.Select(async item =>
                {
                    listFulfilmentData.AddRange(await QueryAndDownloadFileShareServiceFiles(message, item, exchangeSetRootPath));
                });
                await Task.WhenAll(tasks);
            }
            await CreateAncillaryFiles(message.BatchId, exchangeSetPath, message.CorrelationId, listFulfilmentData);
            return "Received Fulfilment Data Successfully!!!!";
        }

        public async Task<SalesCatalogueProductResponse> DownloadSalesCatalogueResponse(SalesCatalogueServiceResponseQueueMessage message)
        {
            return await azureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, message.CorrelationId);
        }

        public async Task<List<FulfilmentDataResponse>> QueryAndDownloadFileShareServiceFiles(SalesCatalogueServiceResponseQueueMessage message, List<Products> products, string exchangeSetRootPath)
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
            return searchBatchResponse;
        }
        private async Task CreateAncillaryFiles(string batchId, string exchangeSetPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData)
        {
            var exchangeSetRootPath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.EncRoot);
            await CreateSerialEncFile(batchId, exchangeSetPath, correlationId);
            await DownloadReadMeFile(batchId, exchangeSetRootPath, correlationId);
            await CreateCatalogFile(batchId, exchangeSetRootPath, correlationId, listFulfilmentData);
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

        public async Task CreateSerialEncFile(string batchId, string exchangeSetPath, string correlationId)
        {
            logger.LogInformation(EventIds.CreateSerialFileRequestStart.ToEventId(), "Serial Enc File creation started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
            await fulfilmentAncillaryFiles.CreateSerialEncFile(batchId, exchangeSetPath, correlationId);
            logger.LogInformation(EventIds.CreateSerialFileRequestCompleted.ToEventId(), "Serial Enc File creation completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
        }

        public async Task<bool> CreateCatalogFile(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData)
        {
            bool isFileCreated = false;

            if (!string.IsNullOrWhiteSpace(exchangeSetRootPath))
            {
                logger.LogInformation(EventIds.CreateCatalogFileRequestStart.ToEventId(), "Create catalog file request started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                isFileCreated = await fulfilmentAncillaryFiles.CreateCatalogFile(batchId, exchangeSetRootPath, correlationId, listFulfilmentData);
                logger.LogInformation(EventIds.CreateCatalogFileRequestCompleted.ToEventId(), "Create catalog file request completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
            }

            return isFileCreated;
        }
    }
}
