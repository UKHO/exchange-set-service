using Microsoft.Azure.Cosmos.Table;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAzureTableStorageClient
    {
        Task<ITableEntity> RetrieveFromTableStorageAsync<TElement>(string partitionKey, string rowKey, string tableName, string storageAccountConnectionString) where TElement : ITableEntity;
        Task<ITableEntity> InsertOrMergeIntoTableStorageAsync(ITableEntity entity, string tableName, string storageAccountConnectionString);
        Task<ITableEntity> DeleteAsync(ITableEntity entity, string tableName, string storageAccountConnectionString, string containerName);
    }
}