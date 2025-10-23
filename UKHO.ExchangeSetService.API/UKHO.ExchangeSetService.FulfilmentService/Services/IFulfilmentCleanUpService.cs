using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.WebJobs;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentCleanUpService
    {
        Task DeleteScsResponseAsync(FulfilmentServiceBatch batch);
        void DeleteBatchFolder(FulfilmentServiceBatch batch);
    }
}
