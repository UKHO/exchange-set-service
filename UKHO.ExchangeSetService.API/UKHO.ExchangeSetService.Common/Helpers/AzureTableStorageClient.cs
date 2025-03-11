using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Azure.Data.Tables;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureTableStorageClient : IAzureTableStorageClient
    {
        private static class TableClientFactory
        {
            // time stamp for an hour.
            private static readonly ConcurrentDictionary<string, Task<TableClient>> tableClients = new();
            public static async Task<TableClient> GetTableClient(string tableName, string storageAccountConnectionString)
            {
                var key = $"{tableName}-{storageAccountConnectionString}";
                return await tableClients.GetOrAdd(key, async _ => await CreateTableClientAsync(tableName, storageAccountConnectionString));
            }
            private static async Task<TableClient> CreateTableClientAsync(string tableName, string storageAccountConnectionString)
            {
                var serviceClient = new TableServiceClient(storageAccountConnectionString);
                var tableClient = serviceClient.GetTableClient(tableName);
                await tableClient.CreateIfNotExistsAsync();
                return tableClient;
            }
        }

        public async Task<TElement> RetrieveFromTableStorageAsync<TElement>(string partitionKey, string rowKey, string tableName, string storageAccountConnectionString) where TElement : class, ITableEntity
        {
            var tableClient = await TableClientFactory.GetTableClient(tableName, storageAccountConnectionString);
            var operation = await tableClient.GetEntityIfExistsAsync<TElement>(partitionKey, rowKey);
            return operation.HasValue ? operation.Value : null;
        }

        public async Task<ITableEntity> InsertOrMergeIntoTableStorageAsync(ITableEntity entity, string tableName, string storageAccountConnectionString)
        {
            var tableClient = await TableClientFactory.GetTableClient(tableName, storageAccountConnectionString);
            var result = await tableClient.UpsertEntityAsync(entity);
            return result as ITableEntity;
        }

        public async Task<ITableEntity> DeleteAsync(ITableEntity entity, string tableName, string storageAccountConnectionString, string containerName)
        {
            var tableClient = await TableClientFactory.GetTableClient(tableName, storageAccountConnectionString);
            await tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag);
            return entity;
        }

        
    }
}
