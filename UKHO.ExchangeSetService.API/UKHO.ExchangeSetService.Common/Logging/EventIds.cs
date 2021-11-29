using Microsoft.Extensions.Logging;

namespace UKHO.ExchangeSetService.Common.Logging
{
    public enum EventIds
    {
        /*Event id range for ESS - 805000 to 805122 */
        /// <summary>
        /// 805000 - Request for sales catalogue service products sincedatetime endpoint is started.
        /// </summary>
        SCSGetProductsFromSpecificDateRequestStart = 805000,
        /// <summary>
        /// 805001 - Request for sales catalogue service products sincedatetime endpoint is completed.
        /// </summary>
        SCSGetProductsFromSpecificDateRequestCompleted = 805001,
        /// <summary>
        /// 805002 - Request for sales catalogue service endpoint is failed due to non ok response.
        /// </summary>
        SalesCatalogueServiceNonOkResponse = 805002,
        /// <summary>
        /// 805003 - Request for sales catalogue service product identifiers endpoint is completed.
        /// </summary>
        SCSPostProductIdentifiersRequestCompleted = 805003,
        /// <summary>
        /// 805004 - Request for sales catalogue service product identifiers endpoint is started.
        /// </summary>
        SCSPostProductIdentifiersRequestStart = 805004,
        /// <summary>
        /// 805005 - Request for sales catalogue service product versions endpoint is started.
        /// </summary>
        SCSPostProductVersionsRequestStart = 805005,
        /// <summary>
        /// 805006 - Request for sales catalogue service product versions endpoint is completed.
        /// </summary>
        SCSPostProductVersionsRequestCompleted = 805006,
        /// <summary>
        /// 805007 - Request/response information is logged successfully.
        /// </summary>
        LogRequest = 805007,
        /// <summary>
        /// 805008 - Error while redacting for response body for specific property.
        /// </summary>
        ErrorRedactingResponseBody = 805008,
        /// <summary>
        /// 805009 - Request for creating batch in file share service is started.
        /// </summary>
        FSSCreateBatchRequestStart = 805009,
        /// <summary>
        /// 805010 - Request for creating batch in file share service is completed.
        /// </summary>
        FSSCreateBatchRequestCompleted = 805010,
        /// <summary>
        /// 805011 - Request for creating batch in file share service is failed due to non ok response.
        /// </summary>
        FSSCreateBatchNonOkResponse = 805011,
        /// <summary>
        /// 805012 - Request sent to the server is incorrect or corrupt.
        /// </summary>
        BadRequest = 805012,
        /// <summary>
        /// 805013 - Server encountered an unexpected condition that prevented it from fulfilling the request.
        /// </summary>
        InternalServerError = 805013,
        /// <summary>
        /// 805014 - The requested resource has not been modified since the last time you accessed it.
        /// </summary>
        NotModified = 805014,
        /// <summary>
        /// 805015 - Request for exchange set service product data sincedatetime endpoint is started.
        /// </summary>
        ESSGetProductsFromSpecificDateRequestStart = 805015,
        /// <summary>
        /// 805016 - Request for exchange set service product data sincedatetime endpoint is completed.
        /// </summary>
        ESSGetProductsFromSpecificDateRequestCompleted = 805016,
        /// <summary>
        /// 805017 - Request for exchange set service product identifiers endpoint is started.
        /// </summary>
        ESSPostProductIdentifiersRequestStart = 805017,
        /// <summary>
        /// 805018 - Request for exchange set service product identifiers endpoint is completed.
        /// </summary>
        ESSPostProductIdentifiersRequestCompleted = 805018,
        /// <summary>
        /// 805019 - Request for exchange set service product versions endpoint is started.
        /// </summary>
        ESSPostProductVersionsRequestStart = 805019,
        /// <summary>
        /// 805020 - Request for exchange set service product versions endpoint is completed.
        /// </summary>
        ESSPostProductVersionsRequestCompleted = 805020,
        /// <summary>
        /// 805021 - Request for storing sales catalogue service response in blob storage is started.
        /// </summary>
        SCSResponseStoreRequestStart = 805021,
        /// <summary>
        /// 805022 - Request for storing sales catalogue service response in blob storage is completed.
        /// </summary>
        SCSResponseStoreRequestCompleted = 805022,
        /// <summary>
        /// 805023 - Sales catalogue service response is stored successfully in blob storage.
        /// </summary>
        SCSResponseStoredToBlobStorage = 805023,
        /// <summary>
        /// 805024 - Create exchange set web job is started.
        /// </summary>
        CreateExchangeSetRequestStart = 805024,
        /// <summary>
        /// 805025 - Create exchange set web job is completed.
        /// </summary>
        CreateExchangeSetRequestCompleted = 805025,
        /// <summary>
        /// 805026 - Download of sales catalogue service response from blob storage is started.
        /// </summary>
        DownloadSalesCatalogueResponseDataStart = 805026,
        /// <summary>
        /// 805027 - Download of sales catalogue service response from blob storage is completed.
        /// </summary>
        DownloadSalesCatalogueResponseDataCompleted = 805027,
        /// <summary>
        /// 805028 - Request for searching ENC files from file share service is started.
        /// </summary>
        QueryFileShareServiceENCFilesRequestStart = 805028,
        /// <summary>
        /// 805029 - Request for searching ENC files from file share service is completed.
        /// </summary>
        QueryFileShareServiceENCFilesRequestCompleted = 805029,
        /// <summary>
        /// 805030 - Request for searching ENC files from file share service is failed due to non ok response.
        /// </summary>
        QueryFileShareServiceENCFilesNonOkResponse = 805030,
        /// <summary>
        /// 805031 - Request for downloading ENC files from file share service is started.
        /// </summary>
        DownloadENCFilesRequestStart = 805031,
        /// <summary>
        /// 805032 - Request for downloading ENC files from file share service is completed.
        /// </summary>
        DownloadENCFilesRequestCompleted = 805032,
        /// <summary>
        /// 805033 - Request for downloading ENC files from file share service is failed due to non ok response.
        /// </summary>
        DownloadENCFilesNonOkResponse = 805033,
        /// <summary>
        /// 805034 - Readme.txt file is not found while searching in file share service.
        /// </summary>
        ReadMeTextFileNotFound = 805034,
        /// <summary>
        /// 805035 - Request for downloading readme.txt from file share service is started.
        /// </summary>
        DownloadReadMeFileRequestStart = 805035,
        /// <summary>
        /// 805036 - Request for downloading readme.txt from file share service is completed.
        /// </summary>
        DownloadReadMeFileRequestCompleted = 805036,
        /// <summary>
        /// 805037 - Request for downloading readme.txt from file share service is failed due to non ok response.
        /// </summary>
        DownloadReadMeFileNonOkResponse = 805037,
        /// <summary>
        /// 805038 - Request for creating serial.enc file in exchange set is started.
        /// </summary>
        CreateSerialFileRequestStart = 805038,
        /// <summary>
        /// 805039 - Request for creating serial.enc file in exchange set is completed.
        /// </summary>
        CreateSerialFileRequestCompleted = 805039,
        /// <summary>
        /// 805040 - Request for creating serial.enc file in exchange set is failed.
        /// </summary>
        SerialFileIsNotCreated = 805040,
        /// <summary>
        /// 805041 - Request for creating exchange set zip file is started.
        /// </summary>
        CreateZipFileRequestStart = 805041,
        /// <summary>
        /// 805042 - Request for creating exchange set zip file is completed.
        /// </summary>
        CreateZipFileRequestCompleted = 805042,
        /// <summary>
        /// 805043 - Request for uploading exchange set zip file to file share service is started.
        /// </summary>
        UploadExchangeSetToFssStart = 805043,
        /// <summary>
        /// 805044 - Request for uploading exchange set zip file to file share service is completed.
        /// </summary>
        UploadExchangeSetToFssCompleted = 805044,
        /// <summary>
        /// 805045 - File upload process in file share service is started.
        /// </summary>
        UploadFileCreationProcessStarted = 805045,
        /// <summary>
        /// 805046 - File upload process in file share service is completed.
        /// </summary>
        UploadFileCreationProcessCompleted = 805046,
        /// <summary>
        /// 805047 - Request for creating file in batch in file share service is started.
        /// </summary>
        CreateFileInBatchStart = 805047,
        /// <summary>
        /// 805048 - Request for creating file in batch in file share service is failed due to non ok response.
        /// </summary>
        CreateFileInBatchNonOkResponse = 805048,
        /// <summary>
        /// 805049 - Request for uploading block into the file for a given batch in file share service is completed.
        /// </summary>
        UploadFileBlockCompleted = 805049,
        /// <summary>
        /// 805050 - Request for uploading block into the file for a given batch in file share service is failed due to non ok response.
        /// </summary>
        UploadFileBlockNonOkResponse = 805050,
        /// <summary>
        /// 805051 - Request for writing block into the file for a given batch in file share service is started.
        /// </summary>
        WriteBlocksToFileStart = 805051,
        /// <summary>
        /// 805052 - Request for writing block into the file for a given batch in file share service is completed.
        /// </summary>
        WriteBlocksToFileCompleted = 805052,
        /// <summary>
        /// 805053 - Request to commit the batch in file share service is started.
        /// </summary>
        UploadCommitBatchStart = 805053,
        /// <summary>
        /// 805054 - Request to commit the batch in file share service is completed.
        /// </summary>
        UploadCommitBatchCompleted = 805054,
        /// <summary>
        /// 805055 - Request for writing block into the file for a given batch in file share service is failed due to non ok response.
        /// </summary>
        WriteBlockToFileNonOkResponse = 805055,
        /// <summary>
        /// 805056 - Request to commit the batch in file share service is failed due to non ok response.
        /// </summary>
        UploadCommitBatchNonOkResponse = 805056,
        /// <summary>
        /// 805057 - Request to get status of the batch from file share service is started.
        /// </summary>
        GetBatchStatusStart = 805057,
        /// <summary>
        /// 805058 - Request to get status of the batch from file share service is completed.
        /// </summary>
        GetBatchStatusCompleted = 805058,
        /// <summary>
        /// 805059 - Request to get status of the batch from file share service is failed due to non ok response.
        /// </summary>
        GetBatchStatusNonOkResponse = 805059,
        /// <summary>
        /// 805060 - Request for creating exchange set zip file is failed.
        /// </summary>
        ErrorInCreatingZipFile = 805060,
        /// <summary>
        /// 805061 - Request for creating catalog.031 file in exchange set is started.
        /// </summary>
        CreateCatalogFileRequestStart = 805061,
        /// <summary>
        /// 805062 - Request for creating catalog.031 file in exchange set is completed.
        /// </summary>
        CreateCatalogFileRequestCompleted = 805062,
        /// <summary>
        /// 805063 - An unhandled exception occurred while processing the request.
        /// </summary>
        UnhandledControllerException = 805063,
        /// <summary>
        /// 805064 - Request for creating catalog.031 file in exchange set is failed.
        /// </summary>
        CatalogFileIsNotCreated = 805064,
        /// <summary>
        /// 805065 - An unexpected file extension is found for file in file share service hence actual file extension is passed.
        /// </summary>
        UnexpectedDefaultFileExtension = 805065,
        /// <summary>
        /// 805066 - Exchange set is created with error.
        /// </summary>
        ExchangeSetCreatedWithError = 805066,
        /// <summary>
        /// 805067 - Exchange set is created successfully.
        /// </summary>
        ExchangeSetCreated = 805067,
        /// <summary>
        /// 805068 - Request for creating products.txt file in exchange set is started.
        /// </summary>
        CreateProductFileRequestStart = 805068,
        /// <summary>
        /// 805069 - Request for creating products.txt file in exchange set is completed.
        /// </summary>
        CreateProductFileRequestCompleted = 805069,
        /// <summary>
        /// 805070 - Request for sales catalogue service catalogue endpoint is started.
        /// </summary>
        SCSGetSalesCatalogueDataRequestStart = 805070,
        /// <summary>
        /// 805071 - Request for sales catalogue service catalogue endpoint is completed.
        /// </summary>
        SCSGetSalesCatalogueDataRequestCompleted = 805071,
        /// <summary>
        /// 805072 - Request for creating products.txt file in exchange set is failed.
        /// </summary>
        ProductFileIsNotCreated = 805072,
        /// <summary>
        /// 805073 - Sales catalogue service is healthy.
        /// </summary>
        SalesCatalogueServiceIsHealthy = 805073,
        /// <summary>
        /// 805074 - Sales catalogue service is unhealthy.
        /// </summary>
        SalesCatalogueServiceIsUnhealthy = 805074,
        /// <summary>
        /// 805075 - File share service is healthy.
        /// </summary>
        FileShareServiceIsHealthy = 805075,
        /// <summary>
        /// 805076 - File share service is unhealthy.
        /// </summary>
        FileShareServiceIsUnhealthy = 805076,
        /// <summary>
        /// 805077 - Event data for exchange set service event hub health check.
        /// </summary>
        EventHubLoggingEventDataForHealthCheck = 805077,
        /// <summary>
        /// 805078 - Event hub for exchange set service is healthy.
        /// </summary>
        EventHubLoggingIsHealthy = 805078,
        /// <summary>
        /// 805079 - Event hub for exchange set service is unhealthy.
        /// </summary>
        EventHubLoggingIsUnhealthy = 805079,
        /// <summary>
        /// 805080 - Requested exchange set is too large.
        /// </summary>
        ExchangeSetTooLarge = 805080,
        /// <summary>
        /// 805081 - Status of batch is failed in file share service.
        /// </summary>
        BatchFailedStatus = 805081,
        /// <summary>
        /// 805082 - Batch is not committed in file share service within defined cutoff time.
        /// </summary>
        BatchCommitTimeout = 805082,
        /// <summary>
        /// 805083 - Batch status of a file in file share service.
        /// </summary>
        BatchStatus = 805083,
        /// <summary>
        /// 805084 - Post callback uri is called after exchange set is created successfully.
        /// </summary>
        ExchangeSetCreatedPostCallbackUriCalled = 805084,
        /// <summary>
        /// 805085 - Post callback uri is not called after exchange set is created successfully.
        /// </summary>
        ExchangeSetCreatedPostCallbackUriNotCalled = 805085,
        /// <summary>
        /// 805086 - Post callback uri is not provided by requestor for successful exchange set creation.
        /// </summary>
        ExchangeSetCreatedPostCallbackUriNotProvided = 805086,
        /// <summary>
        /// 805087 - A message is added to exchange set fulfilment queue.
        /// </summary>
        AddedMessageInQueue = 805087,
        /// <summary>
        /// 805088 - A system exception occurred while processing the exchange set fulfilment request.
        /// </summary>
        SystemException = 805088,
        /// <summary>
        /// 805089 - Download of sales catalogue service response from blob storage is failed.
        /// </summary>
        DownloadSalesCatalogueResponseNonOkResponse = 805089,
        /// <summary>
        /// 805090 - Error while uploading error.txt file to file share service.
        /// </summary>
        ErrorTxtNotUploaded = 805090,
        /// <summary>
        /// 805091 - Error while creating error.txt file.
        /// </summary>
        ErrorTxtNotCreated = 805091,
        /// <summary>
        /// 805092 - Request for uploading block into the file for a given batch in file share service is started.
        /// </summary>
        UploadFileBlockStarted = 805092,
        /// <summary>
        /// 805093 - Request for creating file in batch in file share service is completed.
        /// </summary>
        CreateFileInBatchCompleted = 805093,
        /// <summary>
        /// 805094 - No data found for ENC files while searching particular products in file share service.
        /// </summary>
        FSSResponseNotFoundForRespectiveProductWhileQuerying = 805094,
        /// <summary>
        /// 805095 - Request for sales catalogue service catalogue endpoint is failed due to non ok response.
        /// </summary>
        SalesCatalogueServiceCatalogueDataNonOkResponse = 805095,
        /// <summary>
        /// 805096 - Request for searching readme.txt from file share service is started.
        /// </summary>
        QueryFileShareServiceReadMeFileRequestStart = 805096,
        /// <summary>
        /// 805097 - Request for searching readme.txt from file share service is completed.
        /// </summary>
        QueryFileShareServiceReadMeFileRequestCompleted = 805097,
        /// <summary>
        /// 805098 - Request for searching readme.txt from file share service is failed due to non ok response.
        /// </summary>
        QueryFileShareServiceReadMeFileNonOkResponse = 805098,
        /// <summary>
        /// 805099 - Error while processing exchange set creation and error.txt file is created and uploaded in file share service.
        /// </summary>
        ErrorTxtIsUploaded = 805099,
        /// <summary>
        /// 805100 - Exchange set cleanup web job is started.
        /// </summary>
        ESSCleanUpJobRequestStart = 805100,
        /// <summary>
        /// 805101 - Exchange set cleanup web job is completed.
        /// </summary>
        ESSCleanUpJobRequestCompleted = 805101,
        /// <summary>
        /// 805102 - Request for deleting historic folders and files of exchange set is started.
        /// </summary>
        DeleteHistoricFoldersAndFilesStarted = 805102,
        /// <summary>
        /// 805103 - Request for deleting historic folders and files of exchange set is completed.
        /// </summary>
        DeleteHistoricFoldersAndFilesCompleted = 805103,
        /// <summary>
        /// 805104 - Request for deleting historic folders and files of exchange set is failed.
        /// </summary>
        DeleteHistoricFoldersAndFilesFailed = 805104,
        /// <summary>
        /// 805105 - Historic sales catalogue service response json file is deleted successfully from the container.
        /// </summary>
        HistoricSCSResponseFileDeleted = 805105,
        /// <summary>
        /// 805106 - Historic sales catalogue service response json file is not found in the container for specific batch.
        /// </summary>
        HistoricSCSResponseFileNotFound = 805106,
        /// <summary>
        /// 805107 - Historic folder is deleted successfully for exchange set for specific date.
        /// </summary>
        HistoricDateFolderDeleted = 805107,
        /// <summary>
        /// 805108 - Historic folder is not found for exchange set for past date.
        /// </summary>
        HistoricDateFolderNotFound = 805108,
        /// <summary>
        /// 805109 - An exception occurred while deleting historic folders and files of exchange set.
        /// </summary>
        DeleteHistoricFoldersAndFilesException = 805109,
        /// <summary>
        /// 805110 - Request for retrying sales catalogue service endpoint.
        /// </summary>
        RetryHttpClientSCSRequest = 805110,
        /// <summary>
        /// 805111 - Request for retrying file share service endpoint.
        /// </summary>
        RetryHttpClientFSSRequest = 805111,
        /// <summary>
        /// 805112 - Post callback uri is called after exchange set is created with error.
        /// </summary>
        ExchangeSetCreatedWithErrorPostCallbackUriCalled = 805112,
        /// <summary>
        /// 805113 - Post callback uri is not called after exchange set is created with error.
        /// </summary>
        ExchangeSetCreatedWithErrorPostCallbackUriNotCalled = 805113,
        /// <summary>
        /// 805114 - Post callback uri is not provided by requestor for exchange set creation with error.
        /// </summary>
        ExchangeSetCreatedWithErrorPostCallbackUriNotProvided = 805114,
        /// <summary>
        /// 805115 - Azure blob storage for exchange set service is healthy.
        /// </summary>
        AzureBlobStorageIsHealthy = 805115,
        /// <summary>
        /// 805116 - Azure blob storage for exchange set service is unhealthy.
        /// </summary>
        AzureBlobStorageIsUnhealthy = 805116,
        /// <summary>
        /// 805117 - Azure message queue for exchange set service is healthy.
        /// </summary>
        AzureMessageQueueIsHealthy = 805117,
        /// <summary>
        /// 805118 - Azure message queue for exchange set service is unhealthy.
        /// </summary>
        AzureMessageQueueIsUnhealthy = 805118,
        /// <summary>
        /// 805119 - Azure web job for exchange set service is healthy.
        /// </summary>
        AzureWebJobIsHealthy = 805119,
        /// <summary>
        /// 805120 - Azure web job for exchange set service is unhealthy.
        /// </summary>
        AzureWebJobIsUnhealthy = 805120,
        /// <summary>
        /// 805121 - Product details not found for particular product in sales catalogue service catalogue endpoint.
        /// </summary>
        SalesCatalogueServiceCatalogueDataNotFoundForProduct = 805121,
        /// <summary>
        /// 805122 - New access token is added to cache for external end point resource.
        /// </summary>
        CachingExternalEndPointToken = 805122,
        /// <summary>
        ///  805123 -Preparing to download ENC files based on Product/CellName EditionNumber and UpdateNumber from file share service.
        /// </summary>
        FileShareServicePreparingToDownloadENCFilesStart = 805123,
        /// <summary>
        ///  805124 -Completed download of ENC files based on Product/CellName EditionNumber and UpdateNumber from file share service.
        /// </summary>
        FileShareServiceDownloadENCFilesCompleted = 805124,
        /// <summary>
        /// 805125 - Preparing to query ENC files based on Product/CellName EditionNumber and UpdateNumber from file share service.
        /// </summary>
        FileShareServicePreparingToSearchSetOfENCsStarted = 805125,
        /// <summary>
        /// 805126 - Completed query for ENC files based on Product/CellName EditionNumber and UpdateNumber from file share service.
        /// </summary>
        FileShareServiceSearchQueryForSetOfENCsCompleted = 805126,
        /// <summary>
        /// 805127 - Cancellation of task/token is called/requested when any of the async task in parallel thread is failed.
        /// </summary>
        CancellationTokenEvent = 805127,
        DownloadENCFiles307RedirectResponse = 805128,
        DownloadReadmeFile307RedirectResponse = 805129
    }

    public static class EventIdExtensions
    {
        public static EventId ToEventId(this EventIds eventId)
        {
            return new EventId((int)eventId, eventId.ToString());
        }
    }
}