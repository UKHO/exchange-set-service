using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Enums;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Request;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class FileShareService : IFileShareService
    {
        private readonly IFileShareServiceClient fileShareServiceClient;
        private readonly IAuthTokenProvider authTokenProvider;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;
        private readonly ILogger<FileShareService> logger;
        private readonly IFileSystemHelper fileSystemHelper;
        public FileShareService(IFileShareServiceClient fileShareServiceClient,
                                IAuthTokenProvider authTokenProvider,
                                IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                ILogger<FileShareService> logger,
                                IFileSystemHelper fileSystemHelper)
        {
            this.fileShareServiceClient = fileShareServiceClient;
            this.authTokenProvider = authTokenProvider;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.logger = logger;
            this.fileSystemHelper = fileSystemHelper;
        }
        public async Task<CreateBatchResponse> CreateBatch()
        {
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            var jwtSecurityToken = new JwtSecurityToken(accessToken);
            var oid = jwtSecurityToken.Claims.FirstOrDefault(m => m.Type == "oid").Value;
            var uri = $"/batch";

            CreateBatchRequest createBatchRequest = CreateBatchRequest(oid);

            string payloadJson = JsonConvert.SerializeObject(createBatchRequest);

            var httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Post, payloadJson, accessToken, uri);

            CreateBatchResponse createBatchResponse = await CreateBatchResponse(httpResponse, createBatchRequest.ExpiryDate);
            return createBatchResponse;
        }

        private CreateBatchRequest CreateBatchRequest(string oid)
        {
            CreateBatchRequest createBatchRequest = new CreateBatchRequest
            {
                BusinessUnit = fileShareServiceConfig.Value.BusinessUnit,
                Attributes = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("Exchange Set Type", "Update"),
                    new KeyValuePair<string, string>("Media Type", "Zip"),
                    new KeyValuePair<string, string>("Product Type", "AVCS")
                },
                ExpiryDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                Acl = new Acl()
                {
                    ReadUsers = new List<string>() { oid }
                }
            };

            return createBatchRequest;
        }

        private async Task<CreateBatchResponse> CreateBatchResponse(HttpResponseMessage httpResponse, string batchExpiryDateTime)
        {
            var createBatchResponse = new CreateBatchResponse();
            var body = await httpResponse.Content.ReadAsStringAsync();

            if (httpResponse.StatusCode != HttpStatusCode.Created)
            {
                logger.LogError(EventIds.FSSCreateBatchNonOkResponse.ToEventId(), $"File share service create batch endpoint responded with {httpResponse.StatusCode} and message {body}");
                createBatchResponse.ResponseCode = httpResponse.StatusCode;
                createBatchResponse.ResponseBody = null;
            }
            else
            {
                createBatchResponse.ResponseCode = httpResponse.StatusCode;
                createBatchResponse.ResponseBody = JsonConvert.DeserializeObject<CreateBatchResponseModel>(body);
                createBatchResponse.ResponseBody.BatchStatusUri = $"{fileShareServiceConfig.Value.BaseUrl}/batch/{createBatchResponse.ResponseBody.BatchId}";
                createBatchResponse.ResponseBody.BatchExpiryDateTime = batchExpiryDateTime;
                createBatchResponse.ResponseBody.ExchangeSetFileUri = $"{createBatchResponse.ResponseBody.BatchStatusUri}/files/{fileShareServiceConfig.Value.ExchangeSetFileName}";
            }

            return createBatchResponse;
        }

        public async Task<SearchBatchResponse> GetBatchInfoBasedOnProducts(List<Products> products, string correlationId)
        {
            SearchBatchResponse internalSearchBatchResponse = new SearchBatchResponse();
            internalSearchBatchResponse.Entries = new List<BatchDetail>();
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            var productWithAttributes = GenerateQueryForFss(products);
            var uri = $"/batch?limit={fileShareServiceConfig.Value.Limit}&start={fileShareServiceConfig.Value.Start}&$filter={fileShareServiceConfig.Value.ProductCode} {productWithAttributes}";

            HttpResponseMessage httpResponse;

            string payloadJson = string.Empty;
            var productList = new List<string>();
            var prodCount = products.Select(a => a.UpdateNumbers).Sum(a => a.Count);
            do
            {
                httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, uri, correlationId);

                if (httpResponse.IsSuccessStatusCode)
                {
                    uri = await SelectLatestPublishedDateBatch(products, internalSearchBatchResponse, uri, httpResponse, productList);
                }
                else
                {
                    logger.LogError(EventIds.QueryFileShareServiceNonOkResponse.ToEventId(), "File share service with uri {RequestUri}, responded with {StatusCode} and _X-Correlation-ID:{correlationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, correlationId);
                }
            } while (httpResponse.IsSuccessStatusCode && internalSearchBatchResponse.Entries.Count != 0 && internalSearchBatchResponse.Entries.Count < prodCount);

            return internalSearchBatchResponse;
        }

        private async Task<string> SelectLatestPublishedDateBatch(List<Products> products, SearchBatchResponse internalSearchBatchResponse, string uri, HttpResponseMessage httpResponse, List<string> productList)
        {
            SearchBatchResponse searchBatchResponse = await SearchBatchResponse(httpResponse);
            foreach (var item in searchBatchResponse.Entries)
            {
                foreach (var productItem in products)
                {
                    if (CheckProductDoesExistInResponseItem(item, productItem) && CheckEditionNumberDoesExistInResponseItem(item, productItem)
                        && CheckUpdateNumberDoesExistInResponseItem(item, productItem))
                    {
                        var matchProduct = item.Attributes.Where(a => a.Key == "UpdateNumber");
                        var updateNumber = matchProduct.Select(a => a.Value).FirstOrDefault();
                        var compareProducts = $"{productItem.ProductName}|{productItem.EditionNumber}|{updateNumber}";
                        if (!productList.Contains(compareProducts))
                        {
                            internalSearchBatchResponse.Entries.Add(item);
                            productList.Add(compareProducts);
                        }
                    }
                }
                uri = searchBatchResponse.Links.Next?.Href;
            }

            return uri;
        }

        public bool CheckProductDoesExistInResponseItem(BatchDetail batchDetail, Products product)
        {
            return batchDetail.Attributes.Any(a => a.Key == "CellName" && a.Value == product.ProductName);
        }

        public bool CheckEditionNumberDoesExistInResponseItem(BatchDetail batchDetail, Products product)
        {
            return batchDetail.Attributes.Any(a => a.Key == "EditionNumber" && product.EditionNumber.Value.ToString() == a.Value);
        }

        public bool CheckUpdateNumberDoesExistInResponseItem(BatchDetail batchDetail, Products product)
        {
            var matchProduct = batchDetail.Attributes.Where(a => a.Key == "UpdateNumber");
            var updateNumber = matchProduct.Select(a => a.Value).FirstOrDefault();
            return product.UpdateNumbers.Where(x => x.Value.ToString() == updateNumber).ToList().Count > 0;
        }

        private async Task<SearchBatchResponse> SearchBatchResponse(HttpResponseMessage httpResponse)
        {
            var body = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SearchBatchResponse>(body);
        }

        public string GenerateQueryForFss(List<Products> products)
        {
            var productIndex = 1;
            var productCount = products.Count;
            StringBuilder sb = new StringBuilder();
            sb.Append("(");////1st main (
            foreach (var item in products)
            {
                sb.Append("(");////1st product
                sb.AppendFormat(fileShareServiceConfig.Value.CellName, item.ProductName);
                sb.AppendFormat(fileShareServiceConfig.Value.EditionNumber, item.EditionNumber);
                var lstCount = item.UpdateNumbers.Count;
                var index = 1;
                if (item.UpdateNumbers != null && item.UpdateNumbers.Any())
                {
                    foreach (var updateNumberItem in item.UpdateNumbers)
                    {
                        if (index == 1)
                        {
                            sb.Append("((");
                        }
                        sb.AppendFormat(fileShareServiceConfig.Value.UpdateNumber, updateNumberItem.Value);
                        sb.Append(lstCount != index ? "or " : "))");
                        index += 1;
                    }
                }
                sb.Append(productCount == productIndex ? ")" : ") or ");/////last product or with multiple
                productIndex += 1;
            }
            sb.Append(")");//// last main )
            return sb.ToString();
        }

        public async Task<bool> DownloadBatchFiles(IEnumerable<string> uri, string downloadPath, string correlationId)
        {
            string payloadJson = string.Empty;
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            return await ProcessBatchFile(uri, downloadPath, payloadJson, accessToken, correlationId);
        }

        private async Task<bool> ProcessBatchFile(IEnumerable<string> uri, string downloadPath, string payloadJson, string accessToken, string correlationId)
        {
            bool result = false;
            foreach (var item in uri)
            {
                HttpResponseMessage httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, item, correlationId);
                var fileName = item.Split("/").Last();
                if (httpResponse.IsSuccessStatusCode)
                {
                    fileSystemHelper.CheckAndCreateFolder(downloadPath);
                    string path = Path.Combine(downloadPath, fileName);
                    if (!File.Exists(path))
                    {
                        await CopyFileToFolder(httpResponse, path);
                        result = true;
                    }
                }
                else
                {
                    logger.LogInformation(EventIds.DownloadFileShareServiceNonOkResponse.ToEventId(), "File share service download end point with uri {RequestUri} responded with {StatusCode} and _X-Correlation-ID:{correlationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, correlationId);
                }
            }
            return result;
        }

        private async Task CopyFileToFolder(HttpResponseMessage httpResponse, string path)
        {
            using (Stream stream = await httpResponse.Content.ReadAsStreamAsync())
            {
                fileSystemHelper.CreateFileCopy(path, stream);
            }
        }

        public async Task<bool> DownloadReadMeFile(string readMeFilePath, string batchId, string exchangeSetRootPath, string correlationId)
        {
            string payloadJson = string.Empty;
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            string fileName = fileShareServiceConfig.Value.ReadMeFileName;
            string filePath = Path.Combine(exchangeSetRootPath, fileName);
            fileSystemHelper.CheckAndCreateFolder(exchangeSetRootPath);
            string lineToWrite = string.Concat("File date: ", DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ssZ"));
            HttpResponseMessage httpReadMeFileResponse;
            httpReadMeFileResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, readMeFilePath, correlationId);
            if (httpReadMeFileResponse.IsSuccessStatusCode)
            {
                using (Stream stream = await httpReadMeFileResponse.Content.ReadAsStreamAsync())
                {
                    return fileSystemHelper.DownloadReadmeFile(filePath, stream, lineToWrite);
                }
            }
            else
            {
                logger.LogError(EventIds.ReadMeTextFileIsNotDownloaded.ToEventId(), "Error in downloading readme.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", batchId, correlationId);
                throw new CustomException();
            }
        }

        public async Task<string> SearchReadMeFilePath(string batchId, string correlationId)
        {
            string payloadJson = string.Empty;
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            string filePath = string.Empty;
            var uri = $"{fileShareServiceConfig.Value.BaseUrl}/batch?$filter={fileShareServiceConfig.Value.ProductType} fileName eq '{fileShareServiceConfig.Value.ReadMeFileName}' and BusinessUnit eq '{fileShareServiceConfig.Value.BusinessUnit}'";
            HttpResponseMessage httpResponse;
            httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, uri, correlationId);
            if (httpResponse.IsSuccessStatusCode)
            {
                SearchBatchResponse searchBatchResponse = await SearchBatchResponse(httpResponse);
                if (searchBatchResponse.Entries.Count > 0)
                {
                    var batchResult = searchBatchResponse.Entries.FirstOrDefault();
                    filePath = batchResult.Files.FirstOrDefault().Links.Get.Href;
                }
                else
                {
                    logger.LogError(EventIds.ReadMeTextFileNotFound.ToEventId(), "Readme.txt file not found for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                    throw new CustomException();
                }
            }
            else
            {
                logger.LogError(EventIds.QueryFileShareServiceNonOkResponse.ToEventId(), "Query File share service for readme file with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchId, correlationId);
                throw new CustomException();
            }

            return filePath;
        }

        public async Task <bool> CreateZipFileForExchangeSet(string batchId, string exchangeSetZipRootPath, string correlationId)
        {
            bool isCreateZipFileExchangeSetcreated = false;
            var zipName = $"{exchangeSetZipRootPath}.zip";
            string filePath = Path.Combine(exchangeSetZipRootPath, zipName);
            if (fileSystemHelper.CheckDirectoryAndFileExists(exchangeSetZipRootPath, filePath))
            {
                fileSystemHelper.CreateZipFile(exchangeSetZipRootPath, zipName);
                await Task.CompletedTask;
                
                if (fileSystemHelper.CheckFileExists(zipName))
                {
                    logger.LogInformation(EventIds.CreateZipFileRequestCompleted.ToEventId(), "Exchange set {ExchangeSetFileName} created for BatchId:{BatchId} and  _X-Correlation-ID:{correlationId}", fileShareServiceConfig.Value.ExchangeSetFileName, batchId, correlationId);
                    isCreateZipFileExchangeSetcreated = true;
                }
                else
                {
                    logger.LogError(EventIds.ErrorInCreatingZipFile.ToEventId(), "Error in creating {ExchangeSetFileName} zip for BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", fileShareServiceConfig.Value.ExchangeSetFileName, batchId, correlationId);
                    throw new CustomException();
                }
            }
            else
            {
                logger.LogError(EventIds.ErrorInCreatingZipFile.ToEventId(), "Error in creating {ExchangeSetFileName} for BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", fileShareServiceConfig.Value.ExchangeSetFileName, batchId, correlationId);
                throw new CustomException();              
            }
            return isCreateZipFileExchangeSetcreated;
        }

        //Upload either Exchange Set or Error File
        public async Task<bool> UploadFileToFileShareService(string batchId, string exchangeSetZipRootPath, string correlationId, string fileName)
        {
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            bool isUploadZipFile = false;
            CustomFileInfo customFileInfo = fileSystemHelper.GetFileInfo(Path.Combine(exchangeSetZipRootPath, fileName));

            bool isZipFileCreated = await CreateExchangeSetFile(batchId, correlationId, accessToken, customFileInfo);
            if (isZipFileCreated)
            {
                bool isWriteBlock = await UploadAndWriteBlock(batchId, correlationId, accessToken, customFileInfo);
                if (isWriteBlock)
                {
                    string batchStatus = await CommitAndGetBatchStatus(batchId, correlationId, accessToken, customFileInfo);
                    if (batchStatus == BatchStatus.Committed.ToString())
                    {
                        isUploadZipFile = true;
                    }
                }
            }
            return isUploadZipFile;
        }

        public async Task<bool> CreateExchangeSetFile(string batchId, string correlationId, string accessToken, CustomFileInfo customFileInfo)
        {
            FileCreateMetaData fileCreateMetaData = new FileCreateMetaData()
            {
                AccessToken = accessToken,
                BatchId = batchId,
                FileName = customFileInfo.Name,
                Length = customFileInfo.Length
            };
            return await CreateFile(fileCreateMetaData, accessToken, correlationId);
        }

        public async Task<string> CommitAndGetBatchStatus(string batchId, string correlationId, string accessToken, CustomFileInfo customFileInfo)
        {
            BatchCommitMetaData batchCommitMetaData = new BatchCommitMetaData()
            {
                BatchId = batchId,
                AccessToken = accessToken,
                FullFileName = customFileInfo.FullName
            };
            bool isBatchCommitted = await UploadCommitBatch(batchCommitMetaData, correlationId);
            string batchStatus = BatchStatus.CommitInProgress.ToString();
            if (isBatchCommitted)
            {
                var batchStatusMetaData = new BatchStatusMetaData()
                {
                    AccessToken = accessToken,
                    BatchId = batchId
                };
                batchStatus = await GetBatchStatus(batchStatusMetaData, correlationId);
            }
            return batchStatus;
        }

        public async Task<bool> UploadAndWriteBlock(string batchId, string correlationId, string accessToken, CustomFileInfo customFileInfo)
        {
            var blockIdList = await UploadBlockFile(batchId, customFileInfo, accessToken, correlationId);
            WriteBlocksToFileMetaData writeBlocksToFileMetaData = new WriteBlocksToFileMetaData()
            {
                BatchId = batchId,
                FileName = customFileInfo.Name,
                AccessToken = accessToken,
                BlockIds = blockIdList
            };
            return await WriteBlockFile(writeBlocksToFileMetaData, correlationId);
        }

        public async Task<bool> CreateFile(FileCreateMetaData fileCreateMetaData, string accessToken, string correlationId)
        {
            logger.LogInformation(EventIds.ExchangeSetFileCreateStart.ToEventId(), "File creation process for BatchId {batchId} and _X-Correlation-ID:{CorrelationId}", fileCreateMetaData.BatchId, correlationId);
            HttpResponseMessage httpResponse;
            httpResponse = await fileShareServiceClient.AddFileInBatchAsync(HttpMethod.Post, new FileCreateModel(), accessToken, fileShareServiceConfig.Value.BaseUrl, fileCreateMetaData.BatchId, fileCreateMetaData.FileName, fileCreateMetaData.Length, "application/zip", correlationId);
            if (httpResponse.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                logger.LogError(EventIds.CreateExchangeSetFileNonOkResponse.ToEventId(), "Error in creating exchange set file with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, fileCreateMetaData.BatchId, correlationId);
                return false;
            }
        }

        public async Task<List<string>> UploadBlockFile(string batchId, CustomFileInfo customFileInfo, string accessToken, string correlationId)
        {
            UploadMessage uploadMessage = new UploadMessage()
            {
                UploadSize = customFileInfo.Length,
                BlockSizeInMultipleOfKBs = fileShareServiceConfig.Value.BlockSizeInMultipleOfKBs
            };
            var blockSizeInMultipleOfKBs = uploadMessage.BlockSizeInMultipleOfKBs <= 0
                                            || uploadMessage.BlockSizeInMultipleOfKBs > 4096 ? 1024 : uploadMessage.BlockSizeInMultipleOfKBs;

            long blockSize = blockSizeInMultipleOfKBs * 1024;
            List<string> blockIdList = new List<string>();
            List<Task> ParallelBlockUploadTasks = new List<Task>();
            long uploadedBytes = 0;
            int blockNum = 0;

            while (uploadedBytes < customFileInfo.Length)
            {
                blockNum++;
                int readBlockSize = (int)(customFileInfo.Length - uploadedBytes <= blockSize ? customFileInfo.Length - uploadedBytes : blockSize);
                string blockId = CommonHelper.GetBlockIds(blockNum);

                var blockUploadMetaData = new UploadBlockMetaData()
                {
                    BatchId = batchId,
                    BlockId = blockId,
                    FullFileName = customFileInfo.FullName,
                    JwtToken = accessToken,
                    Offset = uploadedBytes,
                    Length = readBlockSize,
                    FileName = customFileInfo.Name

                };
                ParallelBlockUploadTasks.Add(UploadFileBlockMetaData(blockUploadMetaData, correlationId));

                blockIdList.Add(blockId);
                uploadedBytes += readBlockSize;
                //run uploads in parallel
                if (ParallelBlockUploadTasks.Count >= fileShareServiceConfig.Value.ParallelUploadThreadCount)
                {
                    Task.WaitAll(ParallelBlockUploadTasks.ToArray());
                    ParallelBlockUploadTasks.Clear();
                }
            }

            if (ParallelBlockUploadTasks.Count > 0)
            {
                await Task.WhenAll(ParallelBlockUploadTasks);
                ParallelBlockUploadTasks.Clear();
            }
            return blockIdList;
        }

        public async Task UploadFileBlockMetaData(UploadBlockMetaData UploadBlockMetaData, string correlationId)
        {
            logger.LogInformation($"Uploaded block id {UploadBlockMetaData.BlockId} for file {UploadBlockMetaData.FileName} and Batch {UploadBlockMetaData.BatchId}");
            byte[] byteData = fileSystemHelper.UploadFileBlockMetaData(UploadBlockMetaData);
            var blockMd5Hash = CommonHelper.CalculateMD5(byteData);
            HttpResponseMessage httpResponse;
            httpResponse = await fileShareServiceClient.UploadFileBlockAsync(HttpMethod.Put, fileShareServiceConfig.Value.BaseUrl, UploadBlockMetaData.BatchId, UploadBlockMetaData.FileName, UploadBlockMetaData.BlockId, byteData, blockMd5Hash, UploadBlockMetaData.JwtToken, correlationId);
            if (httpResponse.IsSuccessStatusCode)
            {
                logger.LogInformation(EventIds.UploadFileBlockMetaDataCompleted.ToEventId(), "File blocks are uploaded for BatchId {batchId} and _X-Correlation-ID:{CorrelationId}", UploadBlockMetaData.BatchId, correlationId);
            }
            else
            {
                logger.LogError(EventIds.UploadFileBlockMetaDataNonOkResponse.ToEventId(), "Error in uploading file blocks with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, UploadBlockMetaData.BatchId, correlationId);
            }
        }

        public async Task<bool> WriteBlockFile(WriteBlocksToFileMetaData writeBlocksToFileMetaData, string correlationId)
        {
            logger.LogInformation(EventIds.WriteBlocksToFileStart.ToEventId(), "Write Block to file process started for BatchId  {batchId} and _X-Correlation-ID:{CorrelationId}", writeBlocksToFileMetaData.BatchId, correlationId);
            WriteBlockFileModel writeBlockfileModel = new WriteBlockFileModel()
            {
                BlockIds = writeBlocksToFileMetaData.BlockIds
            };
            HttpResponseMessage httpResponse;
            httpResponse = await fileShareServiceClient.WriteBlockInFileAsync(HttpMethod.Put, fileShareServiceConfig.Value.BaseUrl, writeBlocksToFileMetaData.BatchId, writeBlocksToFileMetaData.FileName, writeBlockfileModel, writeBlocksToFileMetaData.AccessToken, correlationId: correlationId);
            if (httpResponse.IsSuccessStatusCode)
            {
                logger.LogInformation(EventIds.WriteBlocksToFileCompleted.ToEventId(), "Added blocks to file process started for BatchId {batchId} and _X-Correlation-ID:{CorrelationId}", writeBlocksToFileMetaData.BatchId, correlationId);
                return true;
            }
            else
            {
                logger.LogError(EventIds.WriteBlockToFileNonOkResponse.ToEventId(), "Error in adding blocks to file process for BatchId {batchId} and _X-Correlation-ID:{CorrelationId}", writeBlocksToFileMetaData.BatchId, correlationId);
                return false;
            }
        }

        public async Task<bool> UploadCommitBatch(BatchCommitMetaData batchCommitMetaData, string correlationId)
        {
            logger.LogInformation(EventIds.UploadCommitBatchStart.ToEventId(), "Batch commit for BatchId {batchId} and _X-Correlation-ID:{CorrelationId}", batchCommitMetaData.BatchId, correlationId);
            List<FileDetail> fileDetails = fileSystemHelper.UploadCommitBatch(batchCommitMetaData);
            var batchCommitModel = new BatchCommitModel()
            {
                FileDetails = fileDetails
            };
            HttpResponseMessage httpResponse;
            httpResponse = await fileShareServiceClient.CommitBatchAsync(HttpMethod.Put, fileShareServiceConfig.Value.BaseUrl, batchCommitMetaData.BatchId, batchCommitModel, batchCommitMetaData.AccessToken, correlationId);
            if (httpResponse.IsSuccessStatusCode)
            {
                logger.LogInformation(EventIds.UploadCommitBatchCompleted.ToEventId(), "Batch commit for BatchId {batchId} and _X-Correlation-ID:{CorrelationId} completed", batchCommitMetaData.BatchId, correlationId);
                return true;
            }
            else
            {
                logger.LogError(EventIds.UploadCommitBatchNonOkResponse.ToEventId(), "Error while commiting batch for BatchId {batchId} and _X-Correlation-ID:{CorrelationId} completed", batchCommitMetaData.BatchId, correlationId);
                return false;
            }
        }

        public async Task<string> GetBatchStatus(BatchStatusMetaData batchStatusMetaData, string correlationId)
        {
            logger.LogInformation(EventIds.GetBatchStatusStart.ToEventId(), "Getting batch status for BatchId {batchId} and _X-Correlation-ID:{CorrelationId}", batchStatusMetaData.BatchId, correlationId);
            HttpResponseMessage httpResponse;
            httpResponse = await fileShareServiceClient.GetBatchStatusAsync(HttpMethod.Get, fileShareServiceConfig.Value.BaseUrl, batchStatusMetaData.BatchId, batchStatusMetaData.AccessToken);
            if (httpResponse.IsSuccessStatusCode)
            {
                string bodyJson = await httpResponse.Content.ReadAsStringAsync();
                ResponseBatchStatusModel responseBatchStatusModel = JsonConvert.DeserializeObject<ResponseBatchStatusModel>(bodyJson);
                logger.LogInformation(EventIds.GetBatchStatusCompleted.ToEventId(), "Getting batch status for BatchId {batchId} and _X-Correlation-ID:{CorrelationId} completed", batchStatusMetaData.BatchId, correlationId);
                return responseBatchStatusModel.Status;
            }
            else
            {
                logger.LogError(EventIds.GetBatchStatusNonOkResponse.ToEventId(), "Error while getting batch status for BatchId {batchId} and _X-Correlation-ID:{CorrelationId} completed", batchStatusMetaData.BatchId, correlationId);
                return BatchStatus.Failed.ToString();
            }
        }
    }
}
