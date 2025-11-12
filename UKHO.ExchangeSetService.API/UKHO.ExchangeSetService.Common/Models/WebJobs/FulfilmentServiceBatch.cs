using System.IO;
using Microsoft.Extensions.Configuration;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Models.WebJobs
{
    /// <summary>
    /// Contains data related to the current batch.
    /// </summary>
    public class FulfilmentServiceBatch : FulfilmentServiceBatchBase
    {
        /// <summary>
        /// Queue message
        /// </summary>
        public SalesCatalogueServiceResponseQueueMessage Message { get; }

        /// <summary>
        /// BatchId from queue message
        /// </summary>
        public string BatchId { get; }

        /// <summary>
        /// CorrelationId from queue message
        /// </summary>
        public string CorrelationId { get; }

        /// <summary>
        /// eg. C:\Temp\ess-fulfilment\635219b9-43b6-4e96-9b33-72759ac6d5c2
        /// </summary>
        public string BatchDirectory { get; }

        public FulfilmentServiceBatch(
            IConfiguration configuration,
            SalesCatalogueServiceResponseQueueMessage message
            ) : base(configuration)
        {
            Message = message;
            BatchDirectory = Path.Combine(BaseDirectory, message.BatchId);
            BatchId = message.BatchId;
            CorrelationId = message.CorrelationId;
        }
    }

    /// <summary>
    /// Contains data related to the base folder for all batch temporary storage.
    /// </summary>
    /// <param name="configuration"></param>
    public class FulfilmentServiceBatchBase(IConfiguration configuration)
    {
        /// <summary>
        /// eg. C:\Temp\ess-fulfilment
        /// </summary>
        public string BaseDirectory { get; } = Path.Combine(configuration["TMP"], "ess-fulfilment");
    }
}
