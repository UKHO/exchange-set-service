using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.IO.Compression;
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
       
        public FileShareService(IFileShareServiceClient fileShareServiceClient,
                                IAuthTokenProvider authTokenProvider,
                                IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                ILogger<FileShareService> logger)
        {
            this.fileShareServiceClient = fileShareServiceClient;
            this.authTokenProvider = authTokenProvider;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.logger = logger;
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
                    logger.LogInformation(EventIds.QueryFileShareServiceNonOkResponse.ToEventId(), "File share service with uri {RequestUri}, responded with {StatusCode} and _X-Correlation-ID:{correlationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, correlationId);
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

        public async Task<bool> DownloadBatchFiles(IEnumerable<string> uri, string downloadPath,string correlationId)
        {
            string payloadJson = string.Empty;
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            return await ProcessBatchFile(uri, downloadPath, payloadJson, accessToken, correlationId);
        }

        private async Task<bool> ProcessBatchFile(IEnumerable<string> uri, string downloadPath, string payloadJson, string accessToken,string correlationId)
        {
            bool result = false;
            foreach (var item in uri)
            {
                HttpResponseMessage httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, item, correlationId);
                var fileName = item.Split("/").Last();
                if (httpResponse.IsSuccessStatusCode)
                {
                    CheckCreateFolderPath(downloadPath);
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

        private static async Task CopyFileToFolder(HttpResponseMessage httpResponse, string path)
        {
            using (Stream stream = await httpResponse.Content.ReadAsStreamAsync())
            {
                using (FileStream outputFileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                {
                    stream.CopyTo(outputFileStream);
                }
            }
        }

        public async Task<bool> DownloadReadMeFile(string readMeFilePath, string batchId, string exchangeSetRootPath, string correlationId)
        {
            string payloadJson = string.Empty;
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            string fileName = fileShareServiceConfig.Value.ReadMeFileName;            
            string file = Path.Combine(exchangeSetRootPath, fileName);           
            CheckCreateFolderPath(exchangeSetRootPath);              
            string lineToWrite = string.Concat("File date: ", DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ssZ"));
            string secondLineText = string.Empty;
            HttpResponseMessage httpReadMeFileResponse;
            httpReadMeFileResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, readMeFilePath, correlationId);
            if (httpReadMeFileResponse.IsSuccessStatusCode)
            {
                using (Stream stream = await httpReadMeFileResponse.Content.ReadAsStreamAsync())
                {
                    using (var outputFileStream = new FileStream(file, FileMode.Create, FileAccess.ReadWrite))
                    {
                        stream.CopyTo(outputFileStream);
                    }
                    using StreamReader reader = new StreamReader(stream);
                    secondLineText = GetLine(file);
                }
                string text = File.ReadAllText(file);
                text = secondLineText.Length == 0 ? lineToWrite: text.Replace(secondLineText, lineToWrite);
                File.WriteAllText(file, text);
                return true;
            }
            else
            {
                logger.LogInformation(EventIds.ReadMeTextFileIsNotDownloaded.ToEventId(), "Error in downloading readme.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", batchId, correlationId);
                return false;
            }
        }

        public async Task<string> SearchReadMeFilePath(string batchId,string correlationId)
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
                    filePath =  batchResult.Files.FirstOrDefault().Links.Get.Href;                  
                }
                else
                    logger.LogInformation(EventIds.ReadMeTextFileNotFound.ToEventId(), "Readme.txt file not found for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
            }
            else
                logger.LogInformation(EventIds.QueryFileShareServiceNonOkResponse.ToEventId(), "Query File share service for readme file with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchId, correlationId);

            return filePath;
        }

        public bool CreateZipFileForExchangeSet(string exchangeSetZipRootPath, string correlationId)
        {
            var zipName = $"{exchangeSetZipRootPath}.zip";
            string path = Path.Combine(exchangeSetZipRootPath, zipName);
            if (Directory.Exists(exchangeSetZipRootPath) && !File.Exists(path))
            {
                ZipFile.CreateFromDirectory(exchangeSetZipRootPath, zipName);
                logger.LogInformation(EventIds.CreateZipFileRequestCompleted.ToEventId(), "Exchange set V01X01.zip created with _X-Correlation-ID:{correlationId}", correlationId);
                return true;
            }
            else
            {
                logger.LogInformation(EventIds.ErrorInCreatingZipFile.ToEventId(), "Error in creating V01X01.zip with _X-Correlation-ID:{correlationId}", correlationId);
                return false;
            }                
        }

        public async Task<bool> UploadZipFileForExchangeSetToFileShareService(string batchId , string exchangeSetZipRootPath, string correlationId)
        {
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            bool isUploadZipFile = false;
            FileInfo fileInfo = new FileInfo(fileShareServiceConfig.Value.ExchangeSetFileName);
            await CreateExchangeSetFile(batchId, correlationId, accessToken, fileInfo);
            await UploadAndWriteBlock(batchId, correlationId, accessToken, fileInfo);
            string batchStatus = await CommitAndGetBatchStatus(batchId, correlationId, accessToken, fileInfo);
            if (batchStatus == BatchStatus.Committed.ToString())
            {
                isUploadZipFile = true;
            }
            return isUploadZipFile;
        }

        private async Task CreateExchangeSetFile(string batchId, string correlationId, string accessToken, FileInfo fileInfo)
        {
            var fileCreateMetaData = new FileCreateMetaData()
            {
                AccessToken = accessToken,
                BatchId = batchId,
                FileName = fileInfo.Name,
                Length = fileInfo.Length
            };
            await CreateFile(fileCreateMetaData, accessToken, correlationId);
        }

        private async Task<string> CommitAndGetBatchStatus(string batchId, string correlationId, string accessToken, FileInfo fileInfo)
        {
            BatchCommitMetaData batchCommitMetaData = new BatchCommitMetaData()
            {
                BatchId = batchId,
                AccessToken = accessToken,
                FullFileName = fileInfo.FullName
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
                if (batchStatus != BatchStatus.Committed.ToString())
                {
                    batchStatus = await GetBatchStatus(batchStatusMetaData, correlationId);
                }
            }
            return batchStatus;
        }

        private async Task UploadAndWriteBlock(string batchId, string correlationId, string accessToken, FileInfo fileInfo)
        {
            var blockIdList = await UploadBlockFile(batchId, fileInfo.Name, fileInfo.Length, accessToken, correlationId);
            WriteBlocksToFileMetaData writeBlocksToFileMetaData = new WriteBlocksToFileMetaData()
            {
                BatchId = batchId,
                FileName = fileInfo.Name,
                AccessToken = accessToken,
                BlockIds = blockIdList
            };
            await WriteBlockFile(writeBlocksToFileMetaData, correlationId);
        }

        private static string GetLine(string filePath)
        {
            int lineFound = 2;
            string secondLine = string.Empty;
            using (var sr = new StreamReader(filePath))
            {
                for (int i = 1; i < lineFound; i++)
                    sr.ReadLine();
               secondLine = sr.ReadLine();
            }            
            return secondLine ?? string.Empty;
        }

        public async Task<bool> CreateFile(FileCreateMetaData fileCreateMetaData, string accessToken, string correlationId)
        {
            logger.LogInformation(EventIds.ExchangeSetFileCreateStart.ToEventId(), "File creation process for BatchId {batchId} and _X-Correlation-ID:{CorrelationId}", fileCreateMetaData.BatchId, correlationId);
            HttpResponseMessage httpResponse;           
            httpResponse = await fileShareServiceClient.AddFileInBatchAsync(HttpMethod.Post, new FileCreateModel(), accessToken, fileShareServiceConfig.Value.BaseUrl, fileCreateMetaData.BatchId, fileCreateMetaData.FileName, fileCreateMetaData.Length, correlationId);
            if (httpResponse.IsSuccessStatusCode)
            {
                logger.LogInformation(EventIds.ExchangeSetFileCreateCompleted.ToEventId(), "File creation process for BatchId {batchId} and _X-Correlation-ID:{CorrelationId}", fileCreateMetaData.BatchId, correlationId);
                return true;
            }   
            else
            {
                logger.LogInformation(EventIds.CreateExchangeSetFileNonOkResponse.ToEventId(), "Error in creating exchange set file with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, fileCreateMetaData.BatchId, correlationId);
                return false;
            }              
        }

        public async Task<List<string>> UploadBlockFile(string batchId, string fileName, long length, string accessToken, string correlationId)
        {                 
            UploadMessage uploadMessage = new UploadMessage()
            {
                UploadSize = length,
                BlockSizeInMultipleOfKBs = fileShareServiceConfig.Value.BlockSizeInMultipleOfKBs
            };
            var blockSizeInMultipleOfKBs = uploadMessage.BlockSizeInMultipleOfKBs <= 0
                                            || uploadMessage.BlockSizeInMultipleOfKBs > 4096 ? 1024 : uploadMessage.BlockSizeInMultipleOfKBs;

            long blockSize = blockSizeInMultipleOfKBs * 1024;                   
            List<string> blockIdList = new List<string>();
            List<Task> ParallelBlockUploadTasks = new List<Task>();
            long uploadedBytes = 0;
            int blockNum = 0;
            BlocksHelper blocksHelper = new BlocksHelper();

            while (uploadedBytes < length)
            {
                blockNum++;               
                int readBlockSize = (int)(length - uploadedBytes <= blockSize ? length - uploadedBytes : blockSize);
                string blockId = blocksHelper.GetBlockIds(blockNum);

                var blockUploadMetaData = new UploadBlockMetaData()
                {
                    BatchId = batchId,
                    BlockId = blockId,
                    FullFileName = fileName,
                    JwtToken = accessToken,
                    Offset = uploadedBytes,
                    Length = readBlockSize
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
            var fileInfo = new FileInfo(UploadBlockMetaData.FullFileName);
            logger.LogInformation($"Uploaded block id {UploadBlockMetaData.BlockId} for file {fileInfo.Name} and Batch {UploadBlockMetaData.BatchId}");

            Byte[] byteData = new Byte[UploadBlockMetaData.Length];
            using (var fs = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Seek(UploadBlockMetaData.Offset, SeekOrigin.Begin);
                fs.Read(byteData);
            }
            var blockMd5Hash = HashHelper.CalculateMD5(byteData);
            HttpResponseMessage httpResponse;
            httpResponse = await fileShareServiceClient.UploadFileBlockAsync(HttpMethod.Put, fileShareServiceConfig.Value.BaseUrl,UploadBlockMetaData.BatchId, fileInfo.Name, UploadBlockMetaData.BlockId, byteData, blockMd5Hash, UploadBlockMetaData.JwtToken);
            if (httpResponse.IsSuccessStatusCode)
            {
                logger.LogInformation(EventIds.UploadFileBlockMetaDataCompleted.ToEventId(), "File blocks are uploaded for BatchId {batchId} and _X-Correlation-ID:{CorrelationId}", UploadBlockMetaData.BatchId, correlationId);
            }
            else
            {
                logger.LogInformation(EventIds.UploadFileBlockMetaDataNonOkResponse.ToEventId(), "Error in uploading file blocks with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, UploadBlockMetaData.BatchId, correlationId);
            }            
        }

        public async Task WriteBlockFile(WriteBlocksToFileMetaData writeBlocksToFileMetaData, string correlationId)
        {           
            logger.LogInformation(EventIds.WriteBlocksToFileStart.ToEventId(), "Write Block to file process started for BatchId  {batchId} and _X-Correlation-ID:{CorrelationId}", writeBlocksToFileMetaData.BatchId, correlationId);

            WriteBlockFileModel writeBlockfileModel = new WriteBlockFileModel()
            {
                BlockIds = writeBlocksToFileMetaData.BlockIds
            };
            HttpResponseMessage httpResponse;
            
            httpResponse = await fileShareServiceClient.WriteBlockInFileAsync(HttpMethod.Put, fileShareServiceConfig.Value.BaseUrl, writeBlocksToFileMetaData.BatchId, writeBlocksToFileMetaData.FileName, writeBlockfileModel, writeBlocksToFileMetaData.AccessToken, correlationId);

            if (httpResponse.IsSuccessStatusCode)
                logger.LogInformation(EventIds.WriteBlocksToFileCompleted.ToEventId(), "Added blocks to file process started for BatchId {batchId} and _X-Correlation-ID:{CorrelationId}", writeBlocksToFileMetaData.BatchId, correlationId);
            else
                logger.LogInformation(EventIds.WriteBlockToFileNonOkResponse.ToEventId(), "Added blocks to file process started for BatchId {batchId} and _X-Correlation-ID:{CorrelationId}", writeBlocksToFileMetaData.BatchId, correlationId);
        }

        public async Task<bool> UploadCommitBatch(BatchCommitMetaData batchCommitMetaData, string correlationId)
        {
            logger.LogInformation(EventIds.UploadCommitBatchStart.ToEventId(), "Batch commit for BatchId {batchId} and _X-Correlation-ID:{CorrelationId}", batchCommitMetaData.BatchId, correlationId);
            FileInfo fileInfo = new FileInfo(batchCommitMetaData.FullFileName);
            using var fs = fileInfo.OpenRead();
            var fileMd5Hash = HashHelper.CalculateMD5(fs);
            List<FileDetail> fileDetails = new List<FileDetail>();
            FileDetail fileDetail = new FileDetail()
            {
                FileName = fileInfo.Name,
                Hash = Convert.ToBase64String(fileMd5Hash)
            };
            fileDetails.Add(fileDetail);

            var batchCommitModel = new BatchCommitModel()
            {
                FileDetails = fileDetails
            };
            HttpResponseMessage httpResponse;
            httpResponse = await fileShareServiceClient.CommitBatchAsync(HttpMethod.Put, fileShareServiceConfig.Value.BaseUrl, batchCommitMetaData.BatchId, batchCommitModel, batchCommitMetaData.AccessToken, correlationId);
            if (httpResponse.IsSuccessStatusCode)
            {
                logger.LogInformation(EventIds.UploadCommitBatchStart.ToEventId(), "Batch commit for BatchId {batchId} and _X-Correlation-ID:{CorrelationId} completed", batchCommitMetaData.BatchId, correlationId);
                return true;
            }
            else
            {
                logger.LogInformation(EventIds.UploadCommitBatchStart.ToEventId(), "Error while commiting batch for BatchId {batchId} and _X-Correlation-ID:{CorrelationId} completed", batchCommitMetaData.BatchId, correlationId);
                return false;
            }
        }

        public async Task<string> GetBatchStatus(BatchStatusMetaData batchStatusMetaData, string correlationId)
        {
            logger.LogInformation(EventIds.GetBatchStatusStart.ToEventId(), "Getting batch status for BatchId {batchId} and _X-Correlation-ID:{CorrelationId}", batchStatusMetaData.BatchId, correlationId);
            ResponseBatchStatusModel responseBatchStatusModel = new ResponseBatchStatusModel();
            HttpResponseMessage httpResponse;
            httpResponse = await fileShareServiceClient.GetBatchStatusAsync(HttpMethod.Get, fileShareServiceConfig.Value.BaseUrl,batchStatusMetaData.BatchId, batchStatusMetaData.AccessToken);
            if (httpResponse.IsSuccessStatusCode)
            {
                responseBatchStatusModel = await httpResponse.ReadAsTypeAsync<ResponseBatchStatusModel>();
                logger.LogInformation(EventIds.GetBatchStatusCompleted.ToEventId(), "Getting batch status for BatchId {batchId} and _X-Correlation-ID:{CorrelationId} completed", batchStatusMetaData.BatchId, correlationId);
                return responseBatchStatusModel.Status;
            }
            else
            {
                logger.LogInformation(EventIds.GetBatchStatusNonOkResponse.ToEventId(), "Error while getting batch status for BatchId {batchId} and _X-Correlation-ID:{CorrelationId} completed", batchStatusMetaData.BatchId, correlationId);
                return responseBatchStatusModel.Status;
            }
        }

        private static void CheckCreateFolderPath(string downloadPath)
        {
            if (!Directory.Exists(downloadPath))
            {
                Directory.CreateDirectory(downloadPath);
            }
        }
    }
}
