using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Models.WebJobs
{
    public class FulfilmentServiceBatch
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
        /// eg. 14OCT2025
        /// </summary>
        public string CurrentUtcDate { get; }

        /// <summary>
        /// eg. C:\Home
        /// </summary>
        public string BaseDirectory { get; }

        /// <summary>
        /// eg. C:\Home\14OCT2025\635219b9-43b6-4e96-9b33-72759ac6d5c2
        /// </summary>
        public string BatchDirectory { get; }

        public FulfilmentServiceBatch(
            IConfiguration configuration,
            SalesCatalogueServiceResponseQueueMessage message,
            DateTime currentUtcDate
            )
        {
            Message = message;
            BaseDirectory = configuration["HOME"];
            CurrentUtcDate = currentUtcDate.ToString("ddMMMyyyy");
            BatchDirectory = Path.Combine(BaseDirectory, CurrentUtcDate, Message.BatchId);
            BatchId = Message.BatchId;
            CorrelationId = Message.CorrelationId;
        }
    }
}
