using Microsoft.Azure.EventGrid.Models;

namespace UKHO.ExchangeSetService.Common.Models.Request
{
    public class CustomEventGridEvent : EventGridEvent
    {
        public string Type { get; set; }
    }
}