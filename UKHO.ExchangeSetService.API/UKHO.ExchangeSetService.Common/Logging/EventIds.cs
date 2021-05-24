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
        FSSCreateBatchRequestStart = 805007,
        FSSCreateBatchRequestCompleted = 805008,
        FSSCreateBatchNonOkResponse = 805009
    }

    public static class EventIdExtensions
    {
        public static EventId ToEventId(this EventIds eventId)
        {
            return new EventId((int)eventId, eventId.ToString());
        }
    }
}
