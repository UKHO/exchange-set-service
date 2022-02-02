using Microsoft.Azure.Cosmos.Table;

namespace UKHO.ExchangeSetService.Common.Models.AzureTableEntities
{
    public class CacheTableData : TableEntity
    {
        public string BatchId { get; set; }
    }
}
