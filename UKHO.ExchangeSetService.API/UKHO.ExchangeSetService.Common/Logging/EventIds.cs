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
        SCSResponseStoredAndSentMessageInQueue = 805023
    }

    public static class EventIdExtensions
    {
        public static EventId ToEventId(this EventIds eventId)
        {
            return new EventId((int)eventId, eventId.ToString());
        }
    }
}