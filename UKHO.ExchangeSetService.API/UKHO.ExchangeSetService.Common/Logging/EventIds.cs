using Microsoft.Extensions.Logging;

namespace UKHO.ExchangeSetService.Common.Logging
{
    public enum EventIds
    {
        SCSGetAllProductRequestStart,
        SCSGetAllProductRequestCompleted,
        SalesCatalogueNonOkResponse,
        SCSPostProductIdentifiersRequestCompleted,
        SCSPostProductIdentifiersRequestStart,
        SCSPostProductVersionsRequestStart,
        SCSPostProductVersionsRequestCompleted,
    }

    public static class EventIdExtensions
    {
        public static EventId ToEventId(this EventIds eventId)
        {
            return new EventId((int)eventId, eventId.ToString());
        }
    }
}
