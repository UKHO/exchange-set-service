using System;
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
        /// eg. C:\Home\14Oct2025\635219b9-43b6-4e96-9b33-72759ac6d5c2
        /// </summary>
        public string BatchDirectory { get; }

        public FulfilmentServiceBatch(
            IConfiguration configuration,
            SalesCatalogueServiceResponseQueueMessage message,
            DateTime currentUtcDateTime
            ) : base(configuration, currentUtcDateTime)
        {
            Message = message;
            BatchDirectory = Path.Combine(BaseDirectory, CurrentUtcDate, message.BatchId);
            BatchId = message.BatchId;
            CorrelationId = message.CorrelationId;
        }
    }

    /// <summary>
    /// Contains data related to the base folder for all batch temporary storage.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="currentUtcDateTime"></param>
    public class FulfilmentServiceBatchBase(IConfiguration configuration, DateTime currentUtcDateTime)
    {
        public const string CurrentUtcDateFormat = "ddMMMyyyy";

        /// <summary>
        /// eg. 14Oct2025
        /// </summary>
        public string CurrentUtcDate { get; } = currentUtcDateTime.ToString(CurrentUtcDateFormat);

        /// <summary>
        /// The DateTime value related to <see cref="CurrentUtcDate"/>
        /// </summary>
        public DateTime CurrentUtcDateTime { get; } = currentUtcDateTime;

        /// <summary>
        /// eg. C:\Home
        /// </summary>
        public string BaseDirectory { get; } = configuration["HOME"];
    }
}
