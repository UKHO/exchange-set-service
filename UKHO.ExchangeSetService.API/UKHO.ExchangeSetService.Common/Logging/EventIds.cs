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
        FSSCreateProductDataByProductIdentifiersCreateBatchRequestStart = 805007,
        FSSCreateProductDataByProductIdentifiersCreateBatchRequestCompleted = 805008,
        FSSCreateProductDataByProductVersionsCreateBatchRequestStart = 805009,
        FSSCreateProductDataByProductVersionsCreateBatchRequestCompleted = 805010,
        FSSCreateProductDataSinceDateTimeCreateBatchRequestStart = 805011,
        FSSCreateProductDataSinceDateTimeCreateBatchRequestCompleted = 805012,
        FSSCreateBatchNonOkResponse = 805013,
        LogRequest = 805014,
        ErrorRedactingResponseBody = 805015
    }

    public static class EventIdExtensions
    {
        public static EventId ToEventId(this EventIds eventId)
        {
            return new EventId((int)eventId, eventId.ToString());
        }
    }
}