using Azure.Data.Tables;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAzureTableStorageClient
    {
        Task<TElement> RetrieveFromTableStorageAsync<TElement>(string partitionKey, string rowKey, string tableName, string storageAccountConnectionString) where TElement :class, ITableEntity;
        Task<ITableEntity> InsertOrMergeIntoTableStorageAsync(ITableEntity entity, string tableName, string storageAccountConnectionString);
        Task<ITableEntity> DeleteAsync(ITableEntity entity, string tableName, string storageAccountConnectionString, string containerName);
    }
}