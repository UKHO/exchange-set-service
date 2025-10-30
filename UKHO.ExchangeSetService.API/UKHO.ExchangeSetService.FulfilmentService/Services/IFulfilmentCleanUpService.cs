using UKHO.ExchangeSetService.Common.Models.WebJobs;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentCleanUpService
    {
        void DeleteBatchFolder(FulfilmentServiceBatch batch);
        void DeleteHistoricBatchFolders(FulfilmentServiceBatchBase batchBase);
    }
}
