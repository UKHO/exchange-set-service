using Microsoft.Extensions.Logging;

namespace UKHO.ExchangeSetService.Common.Logging
{
    public enum EventIds
    {
        SCSGetProductsFromSpecificDateRequestStart = 805000,
        SCSGetProductsFromSpecificDateRequestCompleted = 805001,
        SalesCatalogueNonOkResponse = 805002,
        SCSPostProductIdentifiersRequestCompleted = 805003,
        SCSPostProductIdentifiersRequestStart = 805004,
        SCSPostProductVersionsRequestStart = 805005,
        SCSPostProductVersionsRequestCompleted = 805006,
        LogRequest = 805007,
        ErrorRedactingResponseBody = 805008,
        FSSCreateBatchRequestStart = 805009,
        FSSCreateBatchRequestCompleted = 805010,
        FSSCreateBatchNonOkResponse = 805011,
        BadRequest = 805012,
        InternalServerError = 805013,
        NotModified = 805014,
        ESSGetProductsFromSpecificDateRequestStart = 805015,
        ESSGetProductsFromSpecificDateRequestCompleted = 805016,
        ESSPostProductIdentifiersRequestStart = 805017,
        ESSPostProductIdentifiersRequestCompleted = 805018,        
        ESSPostProductVersionsRequestStart = 805019,
        ESSPostProductVersionsRequestCompleted = 805020,
        SCSResponseStoreRequestStart = 805021,
        SCSResponseStoreRequestCompleted = 805022,
        SCSResponseStoredAndSentMessageInQueue = 805023,
        CreateExchangeSetRequestStart = 805024,
        CreateExchangeSetRequestCompleted = 805025,
        DownloadSalesCatalogueResponsDataStart = 805026,
        DownloadSalesCatalogueResponsDataCompleted = 805027,
        QueryFileShareServiceRequestStart = 805028,
        QueryFileShareServiceRequestCompleted = 805029,
        QueryFileShareServiceNonOkResponse = 805030,
        DownloadFileShareServiceFilesStart = 805031,
        DownloadFileShareServiceFilesCompleted = 805032,
        DownloadFileShareServiceNonOkResponse = 805033,        
        ReadMeTextFileNotFound = 805034,
        DownloadReadMeFileRequestStart = 805035,
        DownloadReadMeFileRequestCompleted = 805036,
        ReadMeTextFileIsNotDownloaded = 805037,
        CreateCatalogFileRequestStart = 805038,
        CreateCatalogFileRequestCompleted = 805039,
        UnhandledControllerException = 805040,
        CatalogueFileIsNotCreated = 805041,
        UnexpectedDefaultFileExtension = 805042
    }

    public static class EventIdExtensions
    {
        public static EventId ToEventId(this EventIds eventId)
        {
            return new EventId((int)eventId, eventId.ToString());
        }
    }
}