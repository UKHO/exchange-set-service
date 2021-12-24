using Microsoft.Azure.Cosmos.Table;

namespace UKHO.ExchangeSetService.Common.Models.AzureTableEntities
{
    public class FssResponseCache : TableEntity
    {
        public string BatchId { get; set; }
        public string Response { get; set; }
    }
}
