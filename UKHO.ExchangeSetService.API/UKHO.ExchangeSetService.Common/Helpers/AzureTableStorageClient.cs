using Azure.Data.Tables;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureTableStorageClient: IAzureTableStorageClient
    {
        public async Task<TElement> RetrieveFromTableStorageAsync<TElement>(string partitionKey, string rowKey, string tableName, string storageAccountConnectionString) where TElement :class,ITableEntity
        {
            ////var retrieveOperation = TableOperation.Retrieve<TElement>(partitionKey, rowKey);
            ////return await ExecuteTableOperation(retrieveOperation, tableName, storageAccountConnectionString) as ITableEntity;
            var tableClient = await GetAzureTable(tableName, storageAccountConnectionString);
            var operation = await tableClient.GetEntityAsync<TElement>(partitionKey, rowKey);
            return operation.Value;
        }

        public async Task<ITableEntity> InsertOrMergeIntoTableStorageAsync(ITableEntity entity, string tableName, string storageAccountConnectionString)
        {
            ////var insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            ////return await ExecuteTableOperation(insertOrMergeOperation, tableName, storageAccountConnectionString) as ITableEntity;
            var tableClient = await GetAzureTable(tableName, storageAccountConnectionString);
            var result = await tableClient.UpsertEntityAsync(entity);
            return result as ITableEntity;
        }
       
        ////private async Task<CloudTable> GetAzureTable(string tableName, string storageAccountConnectionString)
        ////{
        ////    var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
        ////    var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
        ////    var table = tableClient.GetTableReference(tableName);
        ////    await table.CreateIfNotExistsAsync();
        ////    return table;
        ////}

        

        ////private async Task<object> ExecuteTableOperation(TableOperation tableOperation, string tableName, string storageAccountConnectionString)
        ////{
        ////    var table = await GetAzureTable(tableName, storageAccountConnectionString);
        ////    var tableResult = await table.ExecuteAsync(tableOperation);
        ////    return tableResult.Result;
        ////}

        public async Task<ITableEntity> DeleteAsync(ITableEntity entity, string tableName, string storageAccountConnectionString, string containerName)
        {
            ////var deleteOperation = TableOperation.Delete(entity);
            ////return await ExecuteTableOperation(deleteOperation, tableName, storageAccountConnectionString) as ITableEntity;
            var tableClient = await GetAzureTable(tableName, storageAccountConnectionString);
            var result = await tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag);
            return result as ITableEntity;
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
