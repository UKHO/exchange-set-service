using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Azure.Data.Tables;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureTableStorageClient : IAzureTableStorageClient
    {
        public async Task<TElement> RetrieveFromTableStorageAsync<TElement>(string partitionKey, string rowKey, string tableName, string storageAccountConnectionString) where TElement : class, ITableEntity
        {
            var tableClient = await GetAzureTable(tableName, storageAccountConnectionString);
            var operation = await tableClient.GetEntityIfExistsAsync<TElement>(partitionKey, rowKey);
            return operation.HasValue ? operation.Value : null;
        }

        public async Task<ITableEntity> InsertOrMergeIntoTableStorageAsync(ITableEntity entity, string tableName, string storageAccountConnectionString)
        {
            var tableClient = await GetAzureTable(tableName, storageAccountConnectionString);
            var result = await tableClient.UpsertEntityAsync(entity);
            return result as ITableEntity;
        }

        public async Task<ITableEntity> DeleteAsync(ITableEntity entity, string tableName, string storageAccountConnectionString, string containerName)
        {
            var tableClient = await GetAzureTable(tableName, storageAccountConnectionString);
            await tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag);
            return entity;
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
