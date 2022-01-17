using Microsoft.Azure.EventGrid.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    public class CustomEventGridEvent : EventGridEvent
    {
        public string Type { get; set; }
    }
}