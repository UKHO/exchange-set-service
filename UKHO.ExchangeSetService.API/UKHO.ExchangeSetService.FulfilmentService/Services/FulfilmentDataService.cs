using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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

        public FulfilmentDataService(IAzureBlobStorageService azureBlobStorageService,
                                    IFulfilmentFileShareService fulfilmentFileShareService,
                                    ILogger<FulfilmentDataService> logger,
                                    IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                    IConfiguration configuration,
                                    IFulfilmentAncillaryFiles fulfilmentAncillaryFiles,
                                    IFulfilmentSalesCatalogueService fulfilmentSalesCatalogueService,
                                    IFulfilmentCallBackService fulfilmentCallBackService,
                                    IMonitorHelper monitorHelper,
                                    IFileSystemHelper fileSystemHelper)
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
                var cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                var tasks = productsList.Select(async item =>
                {
                    fulfilmentDataResponse = await QueryFileShareServiceFiles(message, item, exchangeSetRootPath, cancellationTokenSource, cancellationToken);
                    int queryCount = fulfilmentDataResponse.Count > 0 ? fulfilmentDataResponse.FirstOrDefault().FileShareServiceSearchQueryCount : 0;
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
            await CreateAncillaryFiles(message.BatchId, exchangeSetPath, message.CorrelationId, listFulfilmentData, response);
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

        public async Task<string> CreateLargeExchangeSet(SalesCatalogueServiceResponseQueueMessage message, string currentUtcDate, string largeExchangeSetFolderName)
        {
            string homeDirectoryPath = configuration["HOME"];
            LargeExchangeSetDataResponse response = await SearchAndDownloadEncFilesFromFss(message, homeDirectoryPath, currentUtcDate, largeExchangeSetFolderName);

            var rootDirectorys = fileSystemHelper.GetDirectoryInfo(Path.Combine(homeDirectoryPath, currentUtcDate, message.BatchId))
                                                  .Where(di => di.Name.StartsWith("M0"));

            List<Task> ParallelCreateFolderTasks = new List<Task> { };
            Parallel.ForEach(rootDirectorys, rootDirectoryFolder =>
            {
                string dvdNumber = rootDirectoryFolder.ToString().Substring(rootDirectoryFolder.ToString().Length - 4).Remove(1, 3);

                ParallelCreateFolderTasks.Add(CreatePosFolderStructure(rootDirectoryFolder.ToString()));
                ParallelCreateFolderTasks.Add(fulfilmentAncillaryFiles.CreateMediaFile(message.BatchId, rootDirectoryFolder.ToString(), message.CorrelationId, dvdNumber.ToString()));
                ParallelCreateFolderTasks.Add(DownloadLargeMediaReadMeFile(message.BatchId, rootDirectoryFolder.ToString(), message.CorrelationId));
                ParallelCreateFolderTasks.Add(CreateLargeMediaSerialEncFile(message.BatchId, rootDirectoryFolder.ToString(), message.CorrelationId));
                ParallelCreateFolderTasks.Add(CreateProductFile(message.BatchId, Path.Combine(rootDirectoryFolder.ToString(), fileShareServiceConfig.Value.Info), message.CorrelationId, response.SalesCatalogueDataResponse));
            });

            await Task.WhenAll(ParallelCreateFolderTasks);
            ParallelCreateFolderTasks.Clear();

            List<Task> ParallelCreateFolderTaskForCatlogFile = new List<Task> { };
            Parallel.ForEach(rootDirectorys, rootDirectoryFolder =>
            {
                ParallelCreateFolderTaskForCatlogFile.Add(CreateLargeMediaExchangesetCatalogFile(message.BatchId, rootDirectoryFolder.ToString(), message.CorrelationId));
            });

            await Task.WhenAll(ParallelCreateFolderTaskForCatlogFile);
            ParallelCreateFolderTaskForCatlogFile.Clear();

            return "Large Exchange Set Created Successfully";
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

        private async Task CreateAncillaryFiles(string batchId, string exchangeSetPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueProductResponse salecatalogueProductResponse)
        {
            var exchangeSetRootPath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.EncRoot);
            var exchangeSetInfoPath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.Info);
            SalesCatalogueDataResponse salesCatalogueDataResponse = await GetSalesCatalogueDataResponse(batchId, correlationId);

            await CreateProductFile(batchId, exchangeSetInfoPath, correlationId, salesCatalogueDataResponse);
            await CreateSerialEncFile(batchId, exchangeSetPath, correlationId);
            await DownloadReadMeFile(batchId, exchangeSetRootPath, correlationId);
            await CreateCatalogFile(batchId, exchangeSetRootPath, correlationId, listFulfilmentData, salesCatalogueDataResponse, salecatalogueProductResponse);
        }

        public async Task DownloadReadMeFile(string batchId, string exchangeSetRootPath, string correlationId)
        {
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
                await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadReadMeFileRequestStart,
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

        public async Task<bool> PackageAndUploadExchangeSetZipFileToFileShareService(string batchId, string exchangeSetPath, string exchangeSetZipFilePath, string correlationId)
        {
            bool isZipFileUploaded = false;
            bool isZipFileCreated = false;
            DateTime createZipFileTaskStartedAt = DateTime.UtcNow;
            isZipFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateZipFileRequestStart,
                       EventIds.CreateZipFileRequestCompleted,
                       "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                       async () =>
                       {
                           return await fulfilmentFileShareService.CreateZipFileForExchangeSet(batchId, exchangeSetPath, correlationId);
                       },
                       batchId, correlationId);

            DateTime createZipFileTaskCompletedAt = DateTime.UtcNow;
            monitorHelper.MonitorRequest("Create Zip File Task", createZipFileTaskStartedAt, createZipFileTaskCompletedAt, correlationId, null, null, null, batchId);
            if (isZipFileCreated)
            {
                isZipFileUploaded = await logger.LogStartEndAndElapsedTimeAsync(EventIds.UploadExchangeSetToFssStart,
                      EventIds.UploadExchangeSetToFssCompleted,
                      "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                      async () =>
                      {
                          return await fulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(batchId, exchangeSetZipFilePath, correlationId);
                      },
                  batchId, correlationId);
            }
            return isZipFileUploaded;
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

        public async Task<bool> CreateProductFile(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse)
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
                            return await fulfilmentAncillaryFiles.CreateProductFile(batchId, exchangeSetInfoPath, correlationId, salesCatalogueDataResponse);
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
        private async Task<LargeExchangeSetDataResponse> SearchAndDownloadEncFilesFromFss(SalesCatalogueServiceResponseQueueMessage message, string homeDirectoryPath, string currentUtcDate, string largeExchangeSetFolderName)
        {
            var batchPath = Path.Combine(homeDirectoryPath, currentUtcDate, message.BatchId, largeExchangeSetFolderName);
            var exchangeSetRootPath = Path.Combine(batchPath, "{1}", fileShareServiceConfig.Value.EncRoot);
            var listFulfilmentData = new List<FulfilmentDataResponse>();
            LargeExchangeSetDataResponse largeExchangeSetDataResponse = new LargeExchangeSetDataResponse();
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
                var cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                var tasks = productsList.Select(async item =>
                {
                    fulfilmentDataResponse = await QueryFileShareServiceFiles(message, item, exchangeSetRootPath, cancellationTokenSource, cancellationToken);
                    int queryCount = fulfilmentDataResponse.Count > 0 ? fulfilmentDataResponse.FirstOrDefault().FileShareServiceSearchQueryCount : 0;
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

                largeExchangeSetDataResponse.SalesCatalogueProductResponse = response;
                largeExchangeSetDataResponse.FulfilmentDataResponses = listFulfilmentData;
            }

            largeExchangeSetDataResponse.SalesCatalogueDataResponse = await GetSalesCatalogueDataResponse(message.BatchId, message.CorrelationId);
            return largeExchangeSetDataResponse;
        }

        private async Task DownloadLargeMediaReadMeFile(string batchId, string exchangeSetPath, string correlationId)
        {
            var baseDirectory = fileSystemHelper.GetDirectoryInfo(exchangeSetPath)
                       .Where(di => di.Name.StartsWith("B") && di.Name.Count() == 2 && char.IsDigit(Convert.ToChar(di.Name.ToString()[^1..])));

            List<string> encFolderList = new List<string>();
            foreach (var directory in baseDirectory)
            {
                var encFolder = Path.Combine(directory.ToString(), fileShareServiceConfig.Value.EncRoot);
                encFolderList.Add(encFolder);
            }
            List<Task> ParallelCreateFolderTasks = new List<Task> { };

            Parallel.ForEach(encFolderList, encFolder =>
            {
                ParallelCreateFolderTasks.Add(DownloadReadMeFile(batchId, encFolder, correlationId));
            });
            await Task.WhenAll(ParallelCreateFolderTasks);
            ParallelCreateFolderTasks.Clear();
        }

        private async Task<bool> CreateLargeMediaSerialEncFile(string batchId, string exchangeSetPath, string correlationId)
        {
            DateTime createLargeMediaSerialEncFileTaskStartedAt = DateTime.UtcNow;

            return await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateSerialFileRequestStart,
                      EventIds.CreateSerialFileRequestCompleted,
                      "Create large media serial enc file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                      async () =>
                      {
                          var baseDirectorys = fileSystemHelper.GetDirectoryInfo(exchangeSetPath)
                                                  .Where(di => di.Name.StartsWith("B") && di.Name.Count() == 2 && char.IsDigit(Convert.ToChar(di.Name.ToString().Substring(di.Name.ToString().Length - 1))));

                          string baseLastDirectoryPath = exchangeSetPath.Replace("M01", "M02");
                          if(!Directory.Exists(baseLastDirectoryPath))
                          {
                              baseLastDirectoryPath = string.Empty;
                          }
                          var baseLastDirectory = fileSystemHelper.GetDirectoryInfo(!string.IsNullOrWhiteSpace(baseLastDirectoryPath) ? baseLastDirectoryPath : exchangeSetPath)
                                                  .LastOrDefault(di => di.Name.StartsWith("B") && di.Name.Count() == 2 && char.IsDigit(Convert.ToChar(di.Name.ToString().Substring(di.Name.ToString().Length - 1))));

                          string lastBaseDirectoryNumber = baseLastDirectory != null && !string.IsNullOrWhiteSpace(baseLastDirectory.ToString()) ? baseLastDirectory.ToString().Substring(baseLastDirectory.ToString().Length - 1) : "9";

                          List<Task<bool>> ParallelBaseFolderTasks = new List<Task<bool>> { };
                          Parallel.ForEach(baseDirectorys, baseDirectoryFolder =>
                          {
                              string baseDirectoryNumber = baseDirectoryFolder.ToString().Substring(baseDirectoryFolder.ToString().Length - 1);
                              ParallelBaseFolderTasks.Add(fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(batchId, baseDirectoryFolder.ToString(), correlationId, baseDirectoryNumber.ToString(), lastBaseDirectoryNumber));
                          });
                          await Task.WhenAll(ParallelBaseFolderTasks);

                          DateTime createLargeMediaSerialEncFileTaskCompletedAt = DateTime.UtcNow;
                          monitorHelper.MonitorRequest("Create Large Media Serial Enc File Task", createLargeMediaSerialEncFileTaskStartedAt, createLargeMediaSerialEncFileTaskCompletedAt, correlationId, null, null, null, batchId);

                          return await Task.FromResult(ParallelBaseFolderTasks.All(x => x.Result.Equals(true)));
                      },
                  batchId, correlationId);
        }

        private async Task CreateLargeMediaExchangesetCatalogFile(string batchId, string exchangeSetPath, string correlationId)
        {
            var baseDirectory = fileSystemHelper.GetDirectoryInfo(exchangeSetPath)
                       .Where(di => di.Name.StartsWith("B") && di.Name.Count() == 2 && char.IsDigit(Convert.ToChar(di.Name.ToString()[^1..])));

            List<string> encFolderList = new List<string>();
            foreach (var directory in baseDirectory)
            {
                var encFolder = Path.Combine(directory.ToString(), fileShareServiceConfig.Value.EncRoot);
                encFolderList.Add(encFolder);
            }
            List<Task> ParallelCreateFolderTasks = new List<Task> { };

            Parallel.ForEach(encFolderList, encFolder =>
            {
                ParallelCreateFolderTasks.Add(fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(batchId, encFolder, correlationId));
            });
            await Task.WhenAll(ParallelCreateFolderTasks);
            ParallelCreateFolderTasks.Clear();
        }
    }
}