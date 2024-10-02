using Azure;
using Azure.Data.Tables;
using System;

namespace UKHO.ExchangeSetService.Common.Models.AzureTableEntities
{
    public class CacheTableData : ITableEntity
    {
        public string BatchId { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
