using Azure.Data.Tables;

namespace UKHO.ExchangeSetService.Common.Models.AzureTableEntities
{
    public class CacheTableData : TableEntity
    {
        public string BatchId { get; set; }
    }
}
