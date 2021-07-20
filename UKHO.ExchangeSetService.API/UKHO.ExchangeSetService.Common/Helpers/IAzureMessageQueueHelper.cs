using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAzureMessageQueueHelper
    {
        Task AddMessage(string batchId, int instanceCount, string storageAccountConnectionString, string message, string correlationId);
    }
}
