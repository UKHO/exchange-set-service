using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAzureMessageQueueHelper
    {
        Task AddMessage(string batchId, int instanceNumber, string storageAccountConnectionString, string message, string correlationId);
        Task<HealthCheckResult> CheckMessageQueueHealth(string storageAccountConnectionString, string queueName);
    }
}
