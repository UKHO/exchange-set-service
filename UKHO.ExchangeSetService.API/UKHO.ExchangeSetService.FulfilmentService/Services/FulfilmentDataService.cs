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
        private readonly IFulfilmentSalesCatalogueService fulfilmentSalesCatalogueService;
        private readonly IFulfilmentCallBackService fulfilmentCallBackService;
        private readonly IMonitorHelper monitorHelper;
        public FulfilmentDataService(IAzureBlobStorageService azureBlobStorageService,
                                    IFulfilmentFileShareService fulfilmentFileShareService,
                                    ILogger<FulfilmentDataService> logger,
                                    IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                    IConfiguration configuration,
                                    IFulfilmentAncillaryFiles fulfilmentAncillaryFiles,
                                    IFulfilmentSalesCatalogueService fulfilmentSalesCatalogueService,
                                    IFulfilmentCallBackService fulfilmentCallBackService,
                                    IMonitorHelper monitorHelper)
        {
            this.azureBlobStorageService = azureBlobStorageService;
            this.fulfilmentFileShareService = fulfilmentFileShareService;
            this.logger = logger;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.configuration = configuration;
            this.fulfilmentAncillaryFiles = fulfilmentAncillaryFiles;
            this.fulfilmentSalesCatalogueService = fulfilmentSalesCatalogueService;
            this.fulfilmentCallBackService = fulfilmentCallBackService;
            this.monitorHelper = monitorHelper;
        }

        public async Task<string> CreateExchangeSet(SalesCatalogueServiceResponseQueueMessage message, string currentUtcDate)
        {
            DateTime createExchangeSetTaskStartedAt = DateTime.UtcNow;
            string homeDirectoryPath = configuration["HOME"];
            var exchangeSetPath = Path.Combine(homeDirectoryPath, currentUtcDate, message.BatchId, fileShareServiceConfig.Value.ExchangeSetFileFolder);
            var exchangeSetRootPath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.EncRoot);
            var exchangeSetZipFilePath = Path.Combine(homeDirectoryPath, currentUtcDate, message.BatchId);
            var listFulfilmentData = new List<FulfilmentDataResponse>();
            var response = await DownloadSalesCatalogueResponse(message);
            if (response.Products != null && response.Products.Any())
            {
                DateTime queryAndDownloadEncFilesFromFileShareServiceTaskStartedAt = DateTime.UtcNow;
                int parallelSearchTaskCount = fileShareServiceConfig.Value.ParallelSearchTaskCount;
                int productGroupCount = response.Products.Count % parallelSearchTaskCount == 0 ? response.Products.Count / parallelSearchTaskCount : (response.Products.Count / parallelSearchTaskCount) + 1;
                var productsList = CommonHelper.SplitList((response.Products), productGroupCount);
                List<FulfilmentDataResponse> fulfilmentDataResponse = new List<FulfilmentDataResponse>();
                object sync = new object();
                int fileShareServiceSearchQueryCount = 0;
                var tasks = productsList.Select(async item =>
                {
                    fulfilmentDataResponse = await QueryAndDownloadFileShareServiceFiles(message, item, exchangeSetRootPath);
                    int queryCount = fulfilmentDataResponse.Count > 0 ? fulfilmentDataResponse.FirstOrDefault().FileShareServiceSearchQueryCount : 0;
                    lock (sync)
                    {
                        fileShareServiceSearchQueryCount += queryCount;
                    }
                    listFulfilmentData.AddRange(fulfilmentDataResponse);
                    
                });
                await Task.WhenAll(tasks);
                DateTime queryAndDownloadEncFilesFromFileShareServiceTaskCompletedAt = DateTime.UtcNow;
                int downloadedENCFileCount = 0;
                foreach (var item in listFulfilmentData)
                {
                    downloadedENCFileCount += item.Files.Count();
                }
                monitorHelper.MonitorRequest("Query and Download ENC Files Task", queryAndDownloadEncFilesFromFileShareServiceTaskStartedAt, queryAndDownloadEncFilesFromFileShareServiceTaskCompletedAt, message.CorrelationId, fileShareServiceSearchQueryCount, downloadedENCFileCount, null, message.BatchId);
            }
            await CreateAncillaryFiles(message.BatchId, exchangeSetPath, message.CorrelationId, listFulfilmentData);
            bool isZipFileUploaded = await PackageAndUploadExchangeSetZipFileToFileShareService(message.BatchId, exchangeSetPath, exchangeSetZipFilePath, message.CorrelationId);
            DateTime createExchangeSetTaskCompletedAt = DateTime.UtcNow;
            if (isZipFileUploaded)
            {
                logger.LogInformation(EventIds.ExchangeSetCreated.ToEventId(), "Exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId, message.CorrelationId);
                await fulfilmentCallBackService.SendCallBackResponse(response, message);
                monitorHelper.MonitorRequest("Create Exchange Set Task", createExchangeSetTaskStartedAt, createExchangeSetTaskCompletedAt, message.CorrelationId, null, null, null, message.BatchId);
                return "Exchange Set Created Successfully";
            }            
            monitorHelper.MonitorRequest("Create Exchange Set Task", createExchangeSetTaskStartedAt, createExchangeSetTaskCompletedAt, message.CorrelationId, null, null, null, message.BatchId);
            return "Exchange Set Is Not Created";
        }

        public async Task<SalesCatalogueProductResponse> DownloadSalesCatalogueResponse(SalesCatalogueServiceResponseQueueMessage message)
        {
            return await azureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, message.BatchId, message.CorrelationId);
        }

        public async Task<List<FulfilmentDataResponse>> QueryAndDownloadFileShareServiceFiles(SalesCatalogueServiceResponseQueueMessage message, List<Products> products, string exchangeSetRootPath)
        {
            logger.LogInformation(EventIds.QueryFileShareServiceENCFilesRequestStart.ToEventId(), "File share service search query request started for ENC files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId, message.CorrelationId);
            var searchBatchResponse = await fulfilmentFileShareService.QueryFileShareServiceData(products, message.BatchId, message.CorrelationId);
            logger.LogInformation(EventIds.QueryFileShareServiceENCFilesRequestCompleted.ToEventId(), "File share service search query request completed for ENC files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId, message.CorrelationId);

            if (searchBatchResponse != null && searchBatchResponse.Any())
            {
                logger.LogInformation(EventIds.DownloadENCFilesRequestStart.ToEventId(), "File share service download request started for ENC files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId, message.CorrelationId);
                await fulfilmentFileShareService.DownloadFileShareServiceFiles(message, searchBatchResponse, exchangeSetRootPath);
                logger.LogInformation(EventIds.DownloadENCFilesRequestCompleted.ToEventId(), "File share service download request completed for ENC files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId, message.CorrelationId);
            }
            return searchBatchResponse;
        }
        private async Task CreateAncillaryFiles(string batchId, string exchangeSetPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData)
        {
            var exchangeSetRootPath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.EncRoot);
            var exchangeSetInfoPath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.Info);
            SalesCatalogueDataResponse salesCatalogueDataResponse = await GetSalesCatalogueDataResponse(batchId, correlationId);

            await CreateProductFile(batchId, exchangeSetInfoPath, correlationId, salesCatalogueDataResponse);
            await CreateSerialEncFile(batchId, exchangeSetPath, correlationId);
            await DownloadReadMeFile(batchId, exchangeSetRootPath, correlationId);
            await CreateCatalogFile(batchId, exchangeSetRootPath, correlationId, listFulfilmentData, salesCatalogueDataResponse);
        }

        public async Task DownloadReadMeFile(string batchId, string exchangeSetRootPath, string correlationId)
        {
            logger.LogInformation(EventIds.QueryFileShareServiceReadMeFileRequestStart.ToEventId(), "File share service search query request started for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
            string readMeFilePath = await fulfilmentFileShareService.SearchReadMeFilePath(batchId, correlationId);
            logger.LogInformation(EventIds.QueryFileShareServiceReadMeFileRequestCompleted.ToEventId(), "File share service search query request completed for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);

            if (!string.IsNullOrWhiteSpace(readMeFilePath))
            {
                DateTime createReadMeFileTaskStartedAt = DateTime.UtcNow;
                logger.LogInformation(EventIds.DownloadReadMeFileRequestStart.ToEventId(), "File share service download request started for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                await fulfilmentFileShareService.DownloadReadMeFile(readMeFilePath, batchId, exchangeSetRootPath, correlationId);
                logger.LogInformation(EventIds.DownloadReadMeFileRequestCompleted.ToEventId(), "File share service download request completed for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                DateTime createReadMeFileTaskCompletedAt = DateTime.UtcNow;                
                monitorHelper.MonitorRequest("Download ReadMe File Task", createReadMeFileTaskStartedAt, createReadMeFileTaskCompletedAt, correlationId, null, null, null, batchId);
            }
        }

        public async Task CreateSerialEncFile(string batchId, string exchangeSetPath, string correlationId)
        {
            logger.LogInformation(EventIds.CreateSerialFileRequestStart.ToEventId(), "Create serial enc file request started for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
            await fulfilmentAncillaryFiles.CreateSerialEncFile(batchId, exchangeSetPath, correlationId);
            logger.LogInformation(EventIds.CreateSerialFileRequestCompleted.ToEventId(), "Create serial enc file request completed for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
        }

        public async Task<bool> PackageAndUploadExchangeSetZipFileToFileShareService(string batchId, string exchangeSetPath, string exchangeSetZipFilePath, string correlationId)
        {
            bool isZipFileUploaded = false;
            DateTime createZipFileTaskStartedAt = DateTime.UtcNow;
            logger.LogInformation(EventIds.CreateZipFileRequestStart.ToEventId(), "Create exchange set zip file request started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
            bool isZipFileCreated = await fulfilmentFileShareService.CreateZipFileForExchangeSet(batchId, exchangeSetPath, correlationId);
            logger.LogInformation(EventIds.CreateZipFileRequestCompleted.ToEventId(), "Create exchange set zip file request completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
            DateTime createZipFileTaskCompletedAt = DateTime.UtcNow;
            monitorHelper.MonitorRequest("Create Zip File Task", createZipFileTaskStartedAt, createZipFileTaskCompletedAt, correlationId, null, null, null, batchId);
            if (isZipFileCreated)
            {               
                logger.LogInformation(EventIds.UploadExchangeSetToFssStart.ToEventId(), "Upload exchange set zip file request started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                isZipFileUploaded = await fulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(batchId, exchangeSetZipFilePath, correlationId);
                logger.LogInformation(EventIds.UploadExchangeSetToFssCompleted.ToEventId(), "Upload exchange set zip file request completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
            }
            return isZipFileUploaded;
        }

        public async Task<bool> CreateCatalogFile(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse)
        {
            bool isFileCreated = false;

            if (!string.IsNullOrWhiteSpace(exchangeSetRootPath))
            {
                DateTime createCatalogFileTaskStartedAt = DateTime.UtcNow;
                logger.LogInformation(EventIds.CreateCatalogFileRequestStart.ToEventId(), "Create catalog file request started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                isFileCreated = await fulfilmentAncillaryFiles.CreateCatalogFile(batchId, exchangeSetRootPath, correlationId, listFulfilmentData, salesCatalogueDataResponse);
                logger.LogInformation(EventIds.CreateCatalogFileRequestCompleted.ToEventId(), "Create catalog file request completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                DateTime createCatalogFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Create Catalog File Task", createCatalogFileTaskStartedAt, createCatalogFileTaskCompletedAt, correlationId, null, null, null, batchId);
            }

            return isFileCreated;
        }

        public async Task<bool> CreateProductFile(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse)
        {
            bool isProductFileCreated = false;

            if (!string.IsNullOrWhiteSpace(exchangeSetInfoPath))
            {
                DateTime createProductFileTaskStartedAt = DateTime.UtcNow;
                logger.LogInformation(EventIds.CreateProductFileRequestStart.ToEventId(), "Create product file request started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                isProductFileCreated = await fulfilmentAncillaryFiles.CreateProductFile(batchId, exchangeSetInfoPath, correlationId, salesCatalogueDataResponse);
                logger.LogInformation(EventIds.CreateProductFileRequestCompleted.ToEventId(), "Create product file request completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                DateTime createProductFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Create Product File Task", createProductFileTaskStartedAt, createProductFileTaskCompletedAt, correlationId, null, null, null, batchId);
            }

            return isProductFileCreated;
        }

        public async Task<SalesCatalogueDataResponse> GetSalesCatalogueDataResponse(string batchId, string correlationId)
        {
            SalesCatalogueDataResponse salesCatalogueTypeResponse = await fulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(batchId, correlationId);
            return salesCatalogueTypeResponse;
        }
    }
}