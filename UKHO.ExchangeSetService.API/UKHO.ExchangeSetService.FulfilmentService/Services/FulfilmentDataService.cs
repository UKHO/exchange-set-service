using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Validation;

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
        private readonly IFileSystemHelper fileSystemHelper;
        private readonly IProductDataValidator productDataValidator;
        private readonly AioConfiguration aioConfiguration;

        public FulfilmentDataService(IAzureBlobStorageService azureBlobStorageService,
                                    IFulfilmentFileShareService fulfilmentFileShareService,
                                    ILogger<FulfilmentDataService> logger,
                                    IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                    IConfiguration configuration,
                                    IFulfilmentAncillaryFiles fulfilmentAncillaryFiles,
                                    IFulfilmentSalesCatalogueService fulfilmentSalesCatalogueService,
                                    IFulfilmentCallBackService fulfilmentCallBackService,
                                    IMonitorHelper monitorHelper,
                                    IFileSystemHelper fileSystemHelper,
                                    IProductDataValidator productDataValidator,
                                    IOptions<AioConfiguration> aioConfiguration)
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
            this.fileSystemHelper = fileSystemHelper;
            this.productDataValidator = productDataValidator;
            this.aioConfiguration = aioConfiguration.Value;
        }

        public async Task<string> CreateExchangeSet(SalesCatalogueServiceResponseQueueMessage message, string currentUtcDate)
        {
            DateTime createExchangeSetTaskStartedAt = DateTime.UtcNow;
            string homeDirectoryPath = configuration["HOME"];
            var exchangeSetPath = Path.Combine(homeDirectoryPath, currentUtcDate, message.BatchId, fileShareServiceConfig.Value.ExchangeSetFileFolder);
            var exchangeSetZipFilePath = Path.Combine(homeDirectoryPath, currentUtcDate, message.BatchId);

            //Get SCS catalogue essData response
            SalesCatalogueDataResponse salesCatalogueEssDataResponse = await GetSalesCatalogueDataResponse(message.BatchId, message.CorrelationId);

            var response = await DownloadSalesCatalogueResponse(message);

            List<string> aioCells = !string.IsNullOrEmpty(aioConfiguration.AioCells) ? new(aioConfiguration.AioCells.Split(',')) : new List<string>();
            var essItems = response.Products
                .Where(product => aioCells.All(aioCell => product.ProductName != aioCell))
                .ToList();

            var aioItems = response.Products
                    .Where(product => aioCells.Any(aioCell => product.ProductName == aioCell))
                    .ToList();

            if (aioConfiguration.IsAioEnabled)
            {
                SalesCatalogueDataResponse salesCatalogueEssDataResponseForAio = (SalesCatalogueDataResponse)salesCatalogueEssDataResponse.Clone();

                if (essItems != null && essItems.Any() || response.Products.Count == 0)
                {
                    salesCatalogueEssDataResponse.ResponseBody = salesCatalogueEssDataResponse.ResponseBody
                                                                 .Where(x => !aioItems.Any(y => y.ProductName.Equals(x.ProductName))).ToList();
                    await CreateStandardExchangeSet(message, response, essItems, exchangeSetPath, salesCatalogueEssDataResponse);
                }
                if ((aioItems != null && aioItems.Count > 0) || (response.Products.Count == aioItems.Count && aioItems.Count > 0))
                {
                    salesCatalogueEssDataResponseForAio.ResponseBody = salesCatalogueEssDataResponseForAio.ResponseBody
                                                         .Where(x => aioItems.Any(y => y.ProductName.Equals(x.ProductName))).ToList();
                    await CreateAioExchangeSet(message, currentUtcDate, homeDirectoryPath, aioItems, salesCatalogueEssDataResponseForAio, response);
                }
            }
            else
            {
                await CreateStandardExchangeSet(message, response, essItems, exchangeSetPath, salesCatalogueEssDataResponse);
            }

            bool isZipFileUploaded = await PackageAndUploadExchangeSetZipFileToFileShareService(message.BatchId, exchangeSetZipFilePath, message.CorrelationId);

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

        public async Task<string> CreateLargeExchangeSet(SalesCatalogueServiceResponseQueueMessage message, string currentUtcDate, string largeExchangeSetFolderName)
        {
            DateTime createExchangeSetTaskStartedAt = DateTime.UtcNow;
            string homeDirectoryPath = configuration["HOME"];
            var exchangeSetFilePath = Path.Combine(homeDirectoryPath, currentUtcDate, message.BatchId);
            bool isExchangeSetFolderCreated = false;
            bool isZipAndUploadSuccessful = false;

            var response = new LargeExchangeSetDataResponse
            {
                //Get SCS catalogue essData response
                SalesCatalogueDataResponse = await GetSalesCatalogueDataResponse(message.BatchId, message.CorrelationId),
                SalesCatalogueProductResponse = await DownloadSalesCatalogueResponse(message)
            };

            List<string> aioCells = !string.IsNullOrEmpty(aioConfiguration.AioCells) ? new(aioConfiguration.AioCells.Split(',')) : new List<string>();
            var essItems = response.SalesCatalogueProductResponse.Products
                .Where(product => aioCells.All(aioCell => product.ProductName != aioCell))
                .ToList();

            var aioItems = response.SalesCatalogueProductResponse.Products
                    .Where(product => aioCells.Any(aioCell => product.ProductName == aioCell))
                    .ToList();

            if (aioConfiguration.IsAioEnabled)
            {
                var largeExchangeSetDataResponseForAio = new LargeExchangeSetDataResponse()
                {
                    SalesCatalogueDataResponse = (SalesCatalogueDataResponse)response.SalesCatalogueDataResponse.Clone(),
                    SalesCatalogueProductResponse = response.SalesCatalogueProductResponse
                };

                if (essItems.Count > 0)
                {
                    response.SalesCatalogueDataResponse.ResponseBody = response.SalesCatalogueDataResponse.ResponseBody
                                                                       .Where(x => !aioItems.Any(y => y.ProductName == x.ProductName)).ToList();
                    isExchangeSetFolderCreated = await CreateStandardLargeMediaExchangeSet(message, homeDirectoryPath, currentUtcDate, response, largeExchangeSetFolderName, exchangeSetFilePath);

                    if (!isExchangeSetFolderCreated)
                    {
                        logger.LogError(EventIds.LargeExchangeSetCreatedWithError.ToEventId(), "Large media exchange creation failed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId, message.CorrelationId);
                        throw new FulfilmentException(EventIds.LargeExchangeSetCreatedWithError.ToEventId());
                    }
                }

                if (aioItems.Count > 0)
                {
                    largeExchangeSetDataResponseForAio.SalesCatalogueDataResponse.ResponseBody = largeExchangeSetDataResponseForAio.SalesCatalogueDataResponse.ResponseBody
                                                                                        .Where(x => aioItems.Any(y => y.ProductName == x.ProductName)).ToList();
                    isExchangeSetFolderCreated = await CreateAioExchangeSet(message, currentUtcDate, homeDirectoryPath, aioItems, largeExchangeSetDataResponseForAio.SalesCatalogueDataResponse, largeExchangeSetDataResponseForAio.SalesCatalogueProductResponse);

                    if (!isExchangeSetFolderCreated)
                    {
                        logger.LogError(EventIds.AIOExchangeSetCreatedWithError.ToEventId(), "AIO exchange creation failed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId, message.CorrelationId);
                        throw new FulfilmentException(EventIds.AIOExchangeSetCreatedWithError.ToEventId());
                    }
                }
            }
            else
            {
                isExchangeSetFolderCreated = await CreateStandardLargeMediaExchangeSet(message, homeDirectoryPath, currentUtcDate, response, largeExchangeSetFolderName, exchangeSetFilePath);

                if (!isExchangeSetFolderCreated)
                {
                    logger.LogError(EventIds.LargeExchangeSetCreatedWithError.ToEventId(), "Large media exchange creation failed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId, message.CorrelationId);
                    throw new FulfilmentException(EventIds.LargeExchangeSetCreatedWithError.ToEventId());
                }
            }

            if (isExchangeSetFolderCreated)
            {
                var rootDirectories = fileSystemHelper.GetDirectoryInfo(Path.Combine(homeDirectoryPath, currentUtcDate, message.BatchId));

                var parallelZipUploadTasks = new List<Task<bool>> { };
                Parallel.ForEach(rootDirectories, rootDirectoryFolder =>
                {
                    if (rootDirectoryFolder.Name.StartsWith("M0"))  //Large Media Exchange Set
                    {
                        string dvdNumber = rootDirectoryFolder.ToString()[^4..].Remove(1, 3);
                        parallelZipUploadTasks.Add(PackageAndUploadLargeMediaExchangeSetZipFileToFileShareService(message.BatchId, rootDirectoryFolder.ToString(), exchangeSetFilePath, message.CorrelationId, string.Format(largeExchangeSetFolderName, dvdNumber.ToString())));
                    }
                    else // AIO
                    {
                        parallelZipUploadTasks.Add(PackageAndUploadLargeMediaExchangeSetZipFileToFileShareService(message.BatchId, rootDirectoryFolder.ToString(), exchangeSetFilePath, message.CorrelationId, fileShareServiceConfig.Value.AioExchangeSetFileFolder));
                    }
                });

                await Task.WhenAll(parallelZipUploadTasks);
                isZipAndUploadSuccessful = await Task.FromResult(parallelZipUploadTasks.All(x => x.Result.Equals(true)));
                parallelZipUploadTasks.Clear();
            }

            if (isZipAndUploadSuccessful)
            {
                var isBatchCommitted = await fulfilmentFileShareService.CommitLargeMediaExchangeSet(message.BatchId, exchangeSetFilePath, message.CorrelationId);
                if (isBatchCommitted)
                {
                    logger.LogInformation(EventIds.ExchangeSetCreated.ToEventId(), "Large media exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId, message.CorrelationId);
                    monitorHelper.MonitorRequest("Create Exchange Set Task", createExchangeSetTaskStartedAt, DateTime.UtcNow, message.CorrelationId, null, null, null, message.BatchId);
                    return "Large Media Exchange Set Created Successfully";
                }
            }

            monitorHelper.MonitorRequest("Create Exchange Set Task", createExchangeSetTaskStartedAt, DateTime.UtcNow, message.CorrelationId, null, null, null, message.BatchId);
            return "Large Media Exchange Set Is Not Created";
        }


        public async Task<SalesCatalogueProductResponse> DownloadSalesCatalogueResponse(SalesCatalogueServiceResponseQueueMessage message)
        {
            return await azureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, message.BatchId, message.CorrelationId);
        }

        public async Task<List<FulfilmentDataResponse>> QueryFileShareServiceFiles(SalesCatalogueServiceResponseQueueMessage message, List<Products> products, string exchangeSetRootPath, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            return await logger.LogStartEndAndElapsedTimeAsync(EventIds.QueryFileShareServiceENCFilesRequestStart,
                   EventIds.QueryFileShareServiceENCFilesRequestCompleted,
                   "File share service search query and download request for ENC files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                   async () =>
                   {
                       return await fulfilmentFileShareService.QueryFileShareServiceData(products, message, cancellationTokenSource, cancellationToken, exchangeSetRootPath);
                   },
               message.BatchId, message.CorrelationId);
        }

        private async Task CreateAncillaryFiles(string batchId, string exchangeSetPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueProductResponse salecatalogueProductResponse, DateTime scsRequestDateTime, SalesCatalogueDataResponse salesCatalogueEssDataResponse)
        {
            var exchangeSetRootPath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.EncRoot);
            var exchangeSetInfoPath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.Info);

            await CreateProductFile(batchId, exchangeSetInfoPath, correlationId, salesCatalogueEssDataResponse, scsRequestDateTime);
            await CreateSerialEncFile(batchId, exchangeSetPath, correlationId);
            await DownloadReadMeFile(batchId, exchangeSetRootPath, correlationId);
            await CreateCatalogFile(batchId, exchangeSetRootPath, correlationId, listFulfilmentData, salesCatalogueEssDataResponse, salecatalogueProductResponse);
        }

        public async Task<bool> DownloadReadMeFile(string batchId, string exchangeSetRootPath, string correlationId)
        {
            bool isDownloadReadMeFileSuccess = false;
            string readMeFilePath = await logger.LogStartEndAndElapsedTimeAsync(EventIds.QueryFileShareServiceReadMeFileRequestStart,
                  EventIds.QueryFileShareServiceReadMeFileRequestCompleted,
                  "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                  async () =>
                  {
                      return await fulfilmentFileShareService.SearchReadMeFilePath(batchId, correlationId);
                  },
               batchId, correlationId);

            if (!string.IsNullOrWhiteSpace(readMeFilePath))
            {
                DateTime createReadMeFileTaskStartedAt = DateTime.UtcNow;
                isDownloadReadMeFileSuccess = await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadReadMeFileRequestStart,
                   EventIds.DownloadReadMeFileRequestCompleted,
                   "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                   async () =>
                   {
                       return await fulfilmentFileShareService.DownloadReadMeFile(readMeFilePath, batchId, exchangeSetRootPath, correlationId);
                   },
                batchId, correlationId);

                DateTime createReadMeFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Download ReadMe File Task", createReadMeFileTaskStartedAt, createReadMeFileTaskCompletedAt, correlationId, null, null, null, batchId);
            }

            return isDownloadReadMeFileSuccess;
        }

        public async Task CreateSerialEncFile(string batchId, string exchangeSetPath, string correlationId)
        {
            await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateSerialFileRequestStart,
                      EventIds.CreateSerialFileRequestCompleted,
                      "Create serial enc file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                      async () =>
                      {
                          return await fulfilmentAncillaryFiles.CreateSerialEncFile(batchId, exchangeSetPath, correlationId);
                      },
                  batchId, correlationId);
        }

        private async Task<bool> PackageAndUploadExchangeSetZipFileToFileShareService(string batchId, string exchangeSetZipFilePath, string correlationId)
        {
            bool isZipFileCreated = false;
            bool isZipFileUploaded = false;
            bool isBatchCommitted = false;

            IDirectoryInfo[] dir = fileSystemHelper.GetSubDirectories(exchangeSetZipFilePath);
            DateTime createZipFileTaskStartedAt = DateTime.UtcNow;

            foreach (var dirPath in dir)
            {
                isZipFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateZipFileRequestStart,
                       EventIds.CreateZipFileRequestCompleted,
                       "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                       async () =>
                       {
                           return await fulfilmentFileShareService.CreateZipFileForExchangeSet(batchId, dirPath.FullName, correlationId);
                       },
                       batchId, correlationId);

                if (!isZipFileCreated)
                {
                    logger.LogError(EventIds.ErrorInCreatingZipFile.ToEventId(), "Error in creating exchange set zip:{ExchangeSetFileName} for BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", dirPath.Name + ".zip", batchId, correlationId);
                    throw new FulfilmentException(EventIds.ErrorInCreatingZipFile.ToEventId());
                }
            }
            DateTime createZipFileTaskCompletedAt = DateTime.UtcNow;
            monitorHelper.MonitorRequest("Create Zip File Task", createZipFileTaskStartedAt, createZipFileTaskCompletedAt, correlationId, null, null, null, batchId);

            if (isZipFileCreated)
            {
                IFileInfo[] fileInfos = fileSystemHelper.GetZipFiles(exchangeSetZipFilePath);

                foreach (var file in fileInfos)
                {
                    isZipFileUploaded = await logger.LogStartEndAndElapsedTimeAsync(EventIds.UploadExchangeSetToFssStart,
                      EventIds.UploadExchangeSetToFssCompleted,
                      "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                      async () =>
                      {
                          return await fulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(batchId, exchangeSetZipFilePath, correlationId, file.Name);
                      },
                    batchId, correlationId);
                }
            }

            if (isZipFileUploaded)
            {
                isBatchCommitted = await fulfilmentFileShareService.CommitExchangeSet(batchId, correlationId, exchangeSetZipFilePath);
            }

            return isBatchCommitted;
        }

        public async Task<bool> CreateCatalogFile(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse)
        {
            bool isFileCreated = false;

            if (!string.IsNullOrWhiteSpace(exchangeSetRootPath))
            {
                DateTime createCatalogFileTaskStartedAt = DateTime.UtcNow;
                isFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateCatalogFileRequestStart,
                        EventIds.CreateCatalogFileRequestCompleted,
                        "Create catalog file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                        async () =>
                        {
                            return await fulfilmentAncillaryFiles.CreateCatalogFile(batchId, exchangeSetRootPath, correlationId, listFulfilmentData, salesCatalogueDataResponse, salesCatalogueProductResponse);
                        },
                        batchId, correlationId);

                DateTime createCatalogFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Create Catalog File Task", createCatalogFileTaskStartedAt, createCatalogFileTaskCompletedAt, correlationId, null, null, null, batchId);
            }

            return isFileCreated;
        }

        public async Task<bool> CreateProductFile(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime)
        {
            bool isProductFileCreated = false;

            if (!string.IsNullOrWhiteSpace(exchangeSetInfoPath))
            {
                DateTime createProductFileTaskStartedAt = DateTime.UtcNow;
                isProductFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateProductFileRequestStart,
                        EventIds.CreateProductFileRequestCompleted,
                        "Create product file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                        async () =>
                        {
                            return await fulfilmentAncillaryFiles.CreateProductFile(batchId, exchangeSetInfoPath, correlationId, salesCatalogueDataResponse, scsRequestDateTime);
                        },
                        batchId, correlationId);

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

        private async Task CreatePosFolderStructure(string largeMediaExchangeSetPath)
        {
            fileSystemHelper.CheckAndCreateFolder(largeMediaExchangeSetPath);
            var largeMediaExchangeSetInfoPath = Path.Combine(largeMediaExchangeSetPath, "INFO");
            fileSystemHelper.CheckAndCreateFolder(largeMediaExchangeSetInfoPath);
            var largeMediaExchangeSetAdcPath = Path.Combine(largeMediaExchangeSetInfoPath, "ADC");
            fileSystemHelper.CheckAndCreateFolder(largeMediaExchangeSetAdcPath);
            await Task.CompletedTask;
        }

        //Search and download ENC files for large media exchange set
        private async Task<LargeExchangeSetDataResponse> SearchAndDownloadEncFilesFromFss(SalesCatalogueServiceResponseQueueMessage message, string homeDirectoryPath, string currentUtcDate, string largeExchangeSetFolderName, LargeExchangeSetDataResponse largeExchangeSetDataResponse)
        {
            var batchPath = Path.Combine(homeDirectoryPath, currentUtcDate, message.BatchId, largeExchangeSetFolderName);
            var exchangeSetRootPath = Path.Combine(batchPath, "{1}", fileShareServiceConfig.Value.EncRoot);
            var listFulfilmentData = new List<FulfilmentDataResponse>();

            List<string> aioCells = !string.IsNullOrEmpty(aioConfiguration.AioCells) ? new(aioConfiguration.AioCells.Split(',')) : new List<string>();

            var essItems = largeExchangeSetDataResponse.SalesCatalogueProductResponse.Products
                .Where(product => aioCells.All(aioCell => product.ProductName != aioCell))
                .ToList();

            Task<ValidationResult> validationResult = productDataValidator.Validate(essItems);

            if (!validationResult.Result.IsValid)
            {
                largeExchangeSetDataResponse.ValidationtFailedMessage = validationResult.Result.Errors[0].ToString();
                return largeExchangeSetDataResponse;
            }

            if (essItems != null && essItems.Any())
            {
                DateTime queryAndDownloadEncFilesFromFileShareServiceTaskStartedAt = DateTime.UtcNow;
                int parallelSearchTaskCount = fileShareServiceConfig.Value.ParallelSearchTaskCount;
                int productGroupCount = essItems.Count % parallelSearchTaskCount == 0 ? essItems.Count / parallelSearchTaskCount : (essItems.Count / parallelSearchTaskCount) + 1;
                var productsList = CommonHelper.SplitList(essItems, productGroupCount);
                var fulfilmentDataResponse = new List<FulfilmentDataResponse>();
                var sync = new object();
                int fileShareServiceSearchQueryCount = 0;
                var cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                var tasks = productsList.Select(async item =>
                {
                    fulfilmentDataResponse = await QueryFileShareServiceFiles(message, item, exchangeSetRootPath, cancellationTokenSource, cancellationToken);
                    int queryCount = fulfilmentDataResponse.Any() ? fulfilmentDataResponse.First().FileShareServiceSearchQueryCount : 0;
                    lock (sync)
                    {
                        fileShareServiceSearchQueryCount += queryCount;
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationTokenSource.Cancel();
                        logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation is cancelled as IsCancellationRequested flag is true in QueryFileShareServiceFiles with {cancellationTokenSource.Token} and batchId:{message.BatchId} and CorrelationId:{message.CorrelationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), message.BatchId, message.CorrelationId);
                        throw new OperationCanceledException();
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
                largeExchangeSetDataResponse.FulfilmentDataResponses = listFulfilmentData;
            }

            return largeExchangeSetDataResponse;
        }

        private async Task DownloadLargeMediaReadMeFile(string batchId, string exchangeSetPath, string correlationId)
        {
            var baseDirectory = fileSystemHelper.GetDirectoryInfo(exchangeSetPath)
                       .Where(di => di.Name.StartsWith("B") && di.Name.Length <= 3 && CommonHelper.IsNumeric(di.Name[^(di.Name.Length - 1)..]));

            var encFolderList = new List<string>();
            foreach (var directory in baseDirectory)
            {
                var encFolder = Path.Combine(directory.ToString(), fileShareServiceConfig.Value.EncRoot);
                encFolderList.Add(encFolder);
            }
            var ParallelCreateFolderTasks = new List<Task> { };

            Parallel.ForEach(encFolderList, encFolder =>
            {
                ParallelCreateFolderTasks.Add(DownloadReadMeFile(batchId, encFolder, correlationId));
            });
            await Task.WhenAll(ParallelCreateFolderTasks);
            ParallelCreateFolderTasks.Clear();
        }

        private async Task<bool> CreateLargeMediaSerialEncFile(string batchId, string exchangeSetPath, string rootfolder, string correlationId)
        {
            DateTime createLargeMediaSerialEncFileTaskStartedAt = DateTime.UtcNow;

            return await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateSerialFileRequestStart,
                      EventIds.CreateSerialFileRequestCompleted,
                      "Create large media serial enc file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                      async () =>
                      {
                          var rootLastDirectoryPath = fileSystemHelper.GetDirectoryInfo(exchangeSetPath)
                                                  .LastOrDefault(di => di.Name.StartsWith("M0"));

                          var baseDirectoryies = fileSystemHelper.GetDirectoryInfo(Path.Combine(exchangeSetPath, rootfolder))
                                                  .Where(di => di.Name.StartsWith("B") && di.Name.Length <= 3 && CommonHelper.IsNumeric(di.Name[^(di.Name.Length - 1)..]));

                          var baseLastDirectory = fileSystemHelper.GetDirectoryInfo(rootLastDirectoryPath?.ToString())
                                                  .LastOrDefault(di => di.Name.StartsWith("B") && di.Name.Length <= 3 && CommonHelper.IsNumeric(di.Name[^(di.Name.Length - 1)..]));

                          string lastBaseDirectoryNumber = baseLastDirectory.ToString().Replace(Path.Combine(rootLastDirectoryPath.ToString(), "B"), "");

                          var ParallelBaseFolderTasks = new List<Task<bool>> { };
                          Parallel.ForEach(baseDirectoryies, baseDirectoryFolder =>
                          {
                              string baseDirectoryNumber = baseDirectoryFolder.ToString().Replace(Path.Combine(exchangeSetPath, rootfolder, "B"), "");
                              ParallelBaseFolderTasks.Add(fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(batchId, baseDirectoryFolder.ToString(), correlationId, baseDirectoryNumber.ToString(), lastBaseDirectoryNumber));
                          });
                          await Task.WhenAll(ParallelBaseFolderTasks);

                          DateTime createLargeMediaSerialEncFileTaskCompletedAt = DateTime.UtcNow;
                          monitorHelper.MonitorRequest("Create Large Media Serial Enc File Task", createLargeMediaSerialEncFileTaskStartedAt, createLargeMediaSerialEncFileTaskCompletedAt, correlationId, null, null, null, batchId);

                          return await Task.FromResult(ParallelBaseFolderTasks.All(x => x.Result.Equals(true)));
                      },
                  batchId, correlationId);
        }

        private async Task<bool> CreateLargeMediaExchangesetCatalogFile(string batchId, string exchangeSetPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse)
        {
            var baseDirectory = fileSystemHelper.GetDirectoryInfo(exchangeSetPath)
                       .Where(di => di.Name.StartsWith("B") && di.Name.Length <= 3 && CommonHelper.IsNumeric(di.Name[^(di.Name.Length - 1)..]));

            var encFolderList = new List<string>();
            foreach (var directory in baseDirectory)
            {
                var encFolder = Path.Combine(directory.ToString(), fileShareServiceConfig.Value.EncRoot);
                encFolderList.Add(encFolder);
            }
            var ParallelCreateFolderTasks = new List<Task<bool>> { };

            Parallel.ForEach(encFolderList, encFolder =>
            {
                var countryCodes = fileSystemHelper.GetDirectoryInfo(encFolder)
                                   .Select(di => di.Name[^2..]).ToList();

                var fulfilmentDataBasedonCountryCode = listFulfilmentData.Where(x => countryCodes.Any(z => x.ProductName.StartsWith(z))).ToList();
                ParallelCreateFolderTasks.Add(fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(batchId, encFolder, correlationId, fulfilmentDataBasedonCountryCode, salesCatalogueDataResponse, salesCatalogueProductResponse));
            });
            await Task.WhenAll(ParallelCreateFolderTasks);
            var isCreateFolderTasksSuccessful = await Task.FromResult(ParallelCreateFolderTasks.All(x => x.Result.Equals(true)));
            ParallelCreateFolderTasks.Clear();

            return isCreateFolderTasksSuccessful;
        }

        private async Task<bool> PackageAndUploadLargeMediaExchangeSetZipFileToFileShareService(string batchId, string exchangeSetPath, string exchangeSetZipFilePath, string correlationId, string mediaZipFileName)
        {
            bool isZipFileUploaded = false;
            bool isZipFileCreated = false;
            DateTime createZipFileTaskStartedAt = DateTime.UtcNow;

            isZipFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateZipFileRequestStart,
                       EventIds.CreateZipFileRequestCompleted,
                       "Create large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                       async () => await fulfilmentFileShareService.CreateZipFileForExchangeSet(batchId, exchangeSetPath, correlationId),
                       batchId, correlationId);

            monitorHelper.MonitorRequest("Create Zip File Task", createZipFileTaskStartedAt, DateTime.UtcNow, correlationId, null, null, null, batchId);

            if (isZipFileCreated)
            {
                isZipFileUploaded = await logger.LogStartEndAndElapsedTimeAsync(EventIds.UploadExchangeSetToFssStart,
                      EventIds.UploadExchangeSetToFssCompleted,
                      "Upload large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                      async () => await fulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(batchId, exchangeSetZipFilePath, correlationId, $"{mediaZipFileName}.zip"),
                      batchId, correlationId);
            }
            return isZipFileUploaded;
        }

        public async Task DownloadInfoFolderFiles(string batchId, string exchangeSetInfoPath, string correlationId)
        {
            IEnumerable<BatchFile> fileDetails = await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadInfoFolderRequestStart,
                  EventIds.DownloadInfoFolderRequestCompleted,
                  "File share service search query request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                  async () => await fulfilmentFileShareService.SearchFolderDetails(batchId, correlationId, fileShareServiceConfig.Value.Info),
                  batchId, correlationId);

            if (fileDetails != null && fileDetails.Any())
            {
                DateTime createInfoFolderFileTaskStartedAt = DateTime.UtcNow;
                await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadInfoFolderRequestStart,
                   EventIds.DownloadInfoFolderRequestCompleted,
                   "File share service download request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                   async () => await fulfilmentFileShareService.DownloadFolderDetails(batchId, correlationId, fileDetails, exchangeSetInfoPath),
                   batchId, correlationId);

                DateTime createInfoFolderFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Download Info Folder File Task", createInfoFolderFileTaskStartedAt, createInfoFolderFileTaskCompletedAt, correlationId, null, null, null, batchId);
            }
        }

        public async Task DownloadAdcFolderFiles(string batchId, string exchangeSetAdcPath, string correlationId)
        {
            IEnumerable<BatchFile> fileDetails = await logger.LogStartEndAndElapsedTimeAsync(EventIds.QueryFileShareServiceAdcFolderFilesRequestStart,
                  EventIds.QueryFileShareServiceAdcFolderFilesRequestCompleted,
                  "File share service search query request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                  async () => await fulfilmentFileShareService.SearchFolderDetails(batchId, correlationId, fileShareServiceConfig.Value.Adc),
                  batchId, correlationId);

            if (fileDetails != null && fileDetails.Any())
            {
                DateTime createAdcFolderFilesTaskStartedAt = DateTime.UtcNow;
                await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadAdcFolderFilesStart,
                   EventIds.DownloadAdcFolderFilesCompleted,
                   "File share service download request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                   async () => await fulfilmentFileShareService.DownloadFolderDetails(batchId, correlationId, fileDetails, exchangeSetAdcPath),
                   batchId, correlationId);

                DateTime createAdcFolderFilesTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Download Adc Folder File Task", createAdcFolderFilesTaskStartedAt, createAdcFolderFilesTaskCompletedAt, correlationId, null, null, null, batchId);
            }
        }

        #region AIO Exchanges Set

        private async Task<bool> CreateAioExchangeSet(SalesCatalogueServiceResponseQueueMessage message, string currentUtcDate, string homeDirectoryPath, List<Products> aioItems, SalesCatalogueDataResponse salesCatalogueEssDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse)
        {
            var aioExchangeSetPath = Path.Combine(homeDirectoryPath, currentUtcDate, message.BatchId, fileShareServiceConfig.Value.AioExchangeSetFileFolder);
            var aioExchangeSetRootPath = Path.Combine(aioExchangeSetPath, fileShareServiceConfig.Value.EncRoot);
            var listFulfilmentAioData = new List<FulfilmentDataResponse>();

            if (aioItems != null && aioItems.Any())
            {
                DateTime queryAndDownloadEncFilesFromFileShareServiceTaskStartedAt = DateTime.UtcNow;
                int parallelSearchTaskCount = fileShareServiceConfig.Value.ParallelSearchTaskCount;
                int productGroupCount = aioItems.Count % parallelSearchTaskCount == 0 ? aioItems.Count / parallelSearchTaskCount : (aioItems.Count / parallelSearchTaskCount) + 1;
                var productsList = CommonHelper.SplitList(aioItems, productGroupCount);
                var fulfilmentDataResponse = new List<FulfilmentDataResponse>();
                var sync = new object();
                int fileShareServiceSearchQueryCount = 0;
                var cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                var tasks = productsList.Select(async item =>
                {
                    fulfilmentDataResponse = await QueryFileShareServiceFiles(message, item, aioExchangeSetRootPath, cancellationTokenSource, cancellationToken);
                    int queryCount = fulfilmentDataResponse.Any() ? fulfilmentDataResponse.First().FileShareServiceSearchQueryCount : 0;
                    lock (sync)
                    {
                        fileShareServiceSearchQueryCount += queryCount;
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationTokenSource.Cancel();
                        logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation is cancelled as IsCancellationRequested flag is true in QueryFileShareServiceFiles with {cancellationTokenSource.Token} and batchId:{message.BatchId} and CorrelationId:{message.CorrelationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), message.BatchId, message.CorrelationId);
                        throw new OperationCanceledException();
                    }
                    listFulfilmentAioData.AddRange(fulfilmentDataResponse);
                });

                await Task.WhenAll(tasks);

                DateTime queryAndDownloadEncFilesFromFileShareServiceTaskCompletedAt = DateTime.UtcNow;
                int downloadedENCFileCount = 0;
                foreach (var item in listFulfilmentAioData)
                {
                    downloadedENCFileCount += item.Files.Count();
                }
                monitorHelper.MonitorRequest("Query and Download ENC Files Task", queryAndDownloadEncFilesFromFileShareServiceTaskStartedAt, queryAndDownloadEncFilesFromFileShareServiceTaskCompletedAt, message.CorrelationId, fileShareServiceSearchQueryCount, downloadedENCFileCount, null, message.BatchId);
            }

            return await CreateAncillaryFilesForAio(message.BatchId, aioExchangeSetPath, message.CorrelationId, salesCatalogueEssDataResponse, message.ScsRequestDateTime, salesCatalogueProductResponse, listFulfilmentAioData);
        }

        #endregion

        private async Task CreateStandardExchangeSet(SalesCatalogueServiceResponseQueueMessage message, SalesCatalogueProductResponse response, List<Products> essItems, string exchangeSetPath, SalesCatalogueDataResponse salesCatalogueEssDataResponse)
        {
            var exchangeSetRootPath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.EncRoot);
            var listFulfilmentData = new List<FulfilmentDataResponse>();

            if (essItems != null && essItems.Any())
            {
                DateTime queryAndDownloadEncFilesFromFileShareServiceTaskStartedAt = DateTime.UtcNow;
                int parallelSearchTaskCount = fileShareServiceConfig.Value.ParallelSearchTaskCount;
                int productGroupCount = essItems.Count % parallelSearchTaskCount == 0 ? essItems.Count / parallelSearchTaskCount : (essItems.Count / parallelSearchTaskCount) + 1;
                var productsList = CommonHelper.SplitList(essItems, productGroupCount);
                var fulfilmentDataResponse = new List<FulfilmentDataResponse>();
                var sync = new object();
                int fileShareServiceSearchQueryCount = 0;
                var cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                var tasks = productsList.Select(async item =>
                {
                    fulfilmentDataResponse = await QueryFileShareServiceFiles(message, item, exchangeSetRootPath, cancellationTokenSource, cancellationToken);
                    int queryCount = fulfilmentDataResponse.Any() ? fulfilmentDataResponse.First().FileShareServiceSearchQueryCount : 0;
                    lock (sync)
                    {
                        fileShareServiceSearchQueryCount += queryCount;
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationTokenSource.Cancel();
                        logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation is cancelled as IsCancellationRequested flag is true in QueryFileShareServiceFiles with {cancellationTokenSource.Token} and batchId:{message.BatchId} and CorrelationId:{message.CorrelationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), message.BatchId, message.CorrelationId);
                        throw new OperationCanceledException();
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

            await CreateAncillaryFiles(message.BatchId, exchangeSetPath, message.CorrelationId, listFulfilmentData, response, message.ScsRequestDateTime, salesCatalogueEssDataResponse);
        }

        private async Task<bool> CreateStandardLargeMediaExchangeSet(SalesCatalogueServiceResponseQueueMessage message, string homeDirectoryPath, string currentUtcDate, LargeExchangeSetDataResponse largeExchangeSetDataResponse, string largeExchangeSetFolderName, string largeMediaExchangeSetFilePath)
        {
            LargeExchangeSetDataResponse response = await SearchAndDownloadEncFilesFromFss(message, homeDirectoryPath, currentUtcDate, largeExchangeSetFolderName, largeExchangeSetDataResponse);
            if (!string.IsNullOrWhiteSpace(response.ValidationtFailedMessage))
            {
                logger.LogError(EventIds.LargeExchangeSetCreatedWithError.ToEventId(), "Large media exchange set is not created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId, message.CorrelationId);
                logger.LogError(EventIds.LargeExchangeSetCreatedWithError.ToEventId(), "Operation Cancelled as product validation failed for BatchId:{BatchId}, _X-Correlation-ID:{CorrelationId} and Validation message :{Message}", message.BatchId, message.CorrelationId, response.ValidationtFailedMessage);
                throw new FulfilmentException(EventIds.BundleInfoValidationFailed.ToEventId());
            }

            var rootDirectories = fileSystemHelper.GetDirectoryInfo(Path.Combine(homeDirectoryPath, currentUtcDate, message.BatchId))
                                                  .Where(di => di.Name.StartsWith("M0"));

            var ParallelCreateFolderTasks = new List<Task> { };
            Parallel.ForEach(rootDirectories, rootDirectoryFolder =>
            {
                string dvdNumber = rootDirectoryFolder.ToString()[^4..].Remove(1, 3);

                ParallelCreateFolderTasks.Add(CreatePosFolderStructure(rootDirectoryFolder.ToString()));
                ParallelCreateFolderTasks.Add(fulfilmentAncillaryFiles.CreateMediaFile(message.BatchId, rootDirectoryFolder.ToString(), message.CorrelationId, dvdNumber.ToString()));
                ParallelCreateFolderTasks.Add(DownloadLargeMediaReadMeFile(message.BatchId, rootDirectoryFolder.ToString(), message.CorrelationId));
                ParallelCreateFolderTasks.Add(CreateLargeMediaSerialEncFile(message.BatchId, largeMediaExchangeSetFilePath, string.Format(largeExchangeSetFolderName, dvdNumber), message.CorrelationId));
                ParallelCreateFolderTasks.Add(CreateProductFile(message.BatchId, Path.Combine(rootDirectoryFolder.ToString(), fileShareServiceConfig.Value.Info), message.CorrelationId, response.SalesCatalogueDataResponse, message.ScsRequestDateTime));
                ParallelCreateFolderTasks.Add(DownloadInfoFolderFiles(message.BatchId, Path.Combine(rootDirectoryFolder.ToString(), fileShareServiceConfig.Value.Info), message.CorrelationId));
                ParallelCreateFolderTasks.Add(DownloadAdcFolderFiles(message.BatchId, Path.Combine(rootDirectoryFolder.ToString(), fileShareServiceConfig.Value.Info, fileShareServiceConfig.Value.Adc), message.CorrelationId));
                ParallelCreateFolderTasks.Add(fulfilmentAncillaryFiles.CreateEncUpdateCsv(response.SalesCatalogueDataResponse, Path.Combine(rootDirectoryFolder.ToString(), fileShareServiceConfig.Value.Info), message.BatchId, message.CorrelationId));
            });

            await Task.WhenAll(ParallelCreateFolderTasks);
            ParallelCreateFolderTasks.Clear();

            var ParallelCreateFolderTaskForCatlogFile = new List<Task<bool>> { };
            Parallel.ForEach(rootDirectories, rootDirectoryFolder =>
            {
                ParallelCreateFolderTaskForCatlogFile.Add(CreateLargeMediaExchangesetCatalogFile(message.BatchId, rootDirectoryFolder.ToString(), message.CorrelationId, response.FulfilmentDataResponses, response.SalesCatalogueDataResponse, response.SalesCatalogueProductResponse));
            });

            await Task.WhenAll(ParallelCreateFolderTaskForCatlogFile);
            bool isExchangeSetFolderCreated = await Task.FromResult(ParallelCreateFolderTaskForCatlogFile.All(x => x.Result.Equals(true)));
            ParallelCreateFolderTaskForCatlogFile.Clear();

            return isExchangeSetFolderCreated;
        }

        private async Task<bool> CreateAncillaryFilesForAio(string batchId, string aioExchangeSetPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime, SalesCatalogueProductResponse salesCatalogueProductResponse, List<FulfilmentDataResponse> listFulfilmentAioData)
        {
            var exchangeSetRootPath = Path.Combine(aioExchangeSetPath, fileShareServiceConfig.Value.EncRoot);
            var exchangeSetInfoPath = Path.Combine(aioExchangeSetPath, fileShareServiceConfig.Value.Info);

            return
            await DownloadReadMeFile(batchId, exchangeSetRootPath, correlationId) &&
            await CreateSerialAioFile(batchId, aioExchangeSetPath, correlationId) &&
            await CreateProductFileForAio(batchId, exchangeSetInfoPath, correlationId, salesCatalogueDataResponse, scsRequestDateTime) &&
            await CreateCatalogFileForAio(batchId, exchangeSetRootPath, correlationId, listFulfilmentAioData, salesCatalogueDataResponse, salesCatalogueProductResponse);
        }

        private async Task<bool> CreateSerialAioFile(string batchId, string aioExchangeSetPath, string correlationId)
        {
            bool isSerialAioCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateSerialAioFileRequestStart,
                      EventIds.CreateSerialAioFileRequestCompleted,
                      "Create serial aio file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                      async () =>
                      {
                          return await fulfilmentAncillaryFiles.CreateSerialAioFile(batchId, aioExchangeSetPath, correlationId);
                      },
                  batchId, correlationId);

            return isSerialAioCreated;
        }

        private async Task<bool> CreateProductFileForAio(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime)
        {
            bool isProductFileCreated = false;

            if (!string.IsNullOrWhiteSpace(exchangeSetInfoPath))
            {
                DateTime createProductFileTaskStartedAt = DateTime.UtcNow;
                isProductFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateProductFileRequestForAioStart,
                        EventIds.CreateProductFileRequestForAioCompleted,
                        "Create aio exchange set product file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                        async () =>
                        {
                            return await fulfilmentAncillaryFiles.CreateProductFile(batchId, exchangeSetInfoPath, correlationId, salesCatalogueDataResponse, scsRequestDateTime);
                        },
                        batchId, correlationId);

                DateTime createProductFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Create Product File Task", createProductFileTaskStartedAt, createProductFileTaskCompletedAt, correlationId, null, null, null, batchId);
            }

            return isProductFileCreated;
        }

        public async Task<bool> CreateCatalogFileForAio(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse)
        {
            bool isFileCreated = false;

            if (!string.IsNullOrWhiteSpace(exchangeSetRootPath))
            {
                DateTime createCatalogFileForAioTaskStartedAt = DateTime.UtcNow;
                isFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateCatalogFileForAioRequestStart,
                        EventIds.CreateCatalogFileForAioRequestCompleted,
                        "Create AIO exchange set catalog file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                        async () =>
                        {
                            return await fulfilmentAncillaryFiles.CreateCatalogFile(batchId, exchangeSetRootPath, correlationId, listFulfilmentData, salesCatalogueDataResponse, salesCatalogueProductResponse);
                        },
                        batchId, correlationId);

                DateTime createCatalogFileForAioTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Create Catalog File Task", createCatalogFileForAioTaskStartedAt, createCatalogFileForAioTaskCompletedAt, correlationId, null, null, null, batchId);
            }

            return isFileCreated;
        }
    }
}