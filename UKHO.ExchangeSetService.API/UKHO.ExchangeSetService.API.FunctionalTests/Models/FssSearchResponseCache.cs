using Microsoft.Azure.Cosmos.Table;
namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    public class FssSearchResponseCache : TableEntity
    {
        public string BatchId { get; set; }
        public string Response { get; set; }
    }
}
