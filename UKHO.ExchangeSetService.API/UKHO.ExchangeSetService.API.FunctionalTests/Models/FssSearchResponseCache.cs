using Azure;
using System;
using Azure.Data.Tables;
namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    public class FssSearchResponseCache : ITableEntity
    {
        public string BatchId { get; set; }
        public string Response { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
