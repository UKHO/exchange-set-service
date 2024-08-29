using Azure.Data.Tables;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureTableStorageClient: IAzureTableStorageClient
    {
        public async Task<TElement> RetrieveFromTableStorageAsync<TElement>(string partitionKey, string rowKey, string tableName, string storageAccountConnectionString) where TElement :class, ITableEntity
        {
            var tableClient = await GetAzureTable(tableName, storageAccountConnectionString);
            var operation = await tableClient.GetEntityAsync<TElement>(partitionKey, rowKey);
            return operation.Value;
        }

        public async Task InsertOrMergeIntoTableStorageAsync(ITableEntity entity, string tableName, string storageAccountConnectionString)
        {
            var tableClient = await GetAzureTable(tableName, storageAccountConnectionString);
            await tableClient.UpsertEntityAsync(entity);
        }


        public async Task DeleteAsync(ITableEntity entity, string tableName, string storageAccountConnectionString, string containerName)
        {
            var tableClient = await GetAzureTable(tableName, storageAccountConnectionString);
            await tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag);
        }


        private static async Task<TableClient> GetAzureTable(string tableName, string storageAccountConnectionString)
        {
            var serviceClient = new TableServiceClient(storageAccountConnectionString);
            var tableClient = serviceClient.GetTableClient(tableName);
            await tableClient.CreateIfNotExistsAsync();
            return tableClient;
        }
    }
}
