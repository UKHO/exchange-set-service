using System;
using System.IO;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

#nullable enable

namespace UKHO.ExchangeSetService.Common.Models.WebJobs
{
    /// <summary>
    /// Store batch data used during exchange set creation.
    /// </summary>
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
        /// eg. C:\Temp\xqcve9YzMkuU0sDJ0eE0Dg\20250615\635219b9-43b6-4e96-9b33-72759ac6d5c2
        /// </summary>
        public string BatchDirectory { get; }

        /// <summary>
        /// eg. C:\Temp\xqcve9YzMkuU0sDJ0eE0Dg\20250615\635219b9-43b6-4e96-9b33-72759ac6d5c2\V01X01
        /// </summary>
        public string ExchangeSetDirectory { get; }

        /// <summary>
        /// eg. C:\Temp\xqcve9YzMkuU0sDJ0eE0Dg\20250615\635219b9-43b6-4e96-9b33-72759ac6d5c2\V01X01\ENC_ROOT
        /// </summary>
        public string ExchangeSetEncRootDirectory { get; }

        /// <summary>
        /// eg. C:\Temp\xqcve9YzMkuU0sDJ0eE0Dg\20250615\635219b9-43b6-4e96-9b33-72759ac6d5c2\AIO
        /// </summary>
        public string AioExchangeSetDirectory { get; }

        /// <summary>
        /// eg. C:\Temp\xqcve9YzMkuU0sDJ0eE0Dg\20250615\635219b9-43b6-4e96-9b33-72759ac6d5c2\AIO\ENC_ROOT
        /// </summary>
        public string AioExchangeSetEncRootDirectory { get; }

        /// <summary>
        /// eg. M0{0}X02
        /// </summary>
        public string LargeExchangeSetFolderName { get; }

        /// <summary>
        /// eg. C:\Temp\xqcve9YzMkuU0sDJ0eE0Dg\20250615\635219b9-43b6-4e96-9b33-72759ac6d5c2\M0{0}X02
        /// </summary>
        public string LargeExchangeSetDirectory { get; }

        /// <summary>
        /// eg. C:\Temp\xqcve9YzMkuU0sDJ0eE0Dg\20250615\635219b9-43b6-4e96-9b33-72759ac6d5c2\M0{0}X02\{1}\ENC_ROOT
        /// </summary>
        public string LargeExchangeSetEncRootDirectory { get; }

        /// <summary>
        /// eg. C:\Temp\xqcve9YzMkuU0sDJ0eE0Dg\20250615\635219b9-43b6-4e96-9b33-72759ac6d5c2\error.txt
        /// </summary>
        public string ErrorFile { get; }

        public FulfilmentServiceBatch(
            string baseWorkDirectory,
            SalesCatalogueServiceResponseQueueMessage fulfilmentServiceQueueMessage,
            FileShareServiceConfiguration fileShareServiceConfiguration,
            PeriodicOutputServiceConfiguration periodicOutputServiceConfiguration
            )
        {
            // The BatchDirectory is in the format {baseWorkDirectory}/{uniqueId}/{yyyyMMdd}/{batchId}, where {uniqueId} is used to identify the fulfilment job itself.
            BatchDirectory = Path.Combine(baseWorkDirectory, "xqcve9YzMkuU0sDJ0eE0Dg", DateTime.UtcNow.ToString("yyyyMMdd"), fulfilmentServiceQueueMessage.BatchId);
            Message = fulfilmentServiceQueueMessage;
            BatchId = fulfilmentServiceQueueMessage.BatchId;
            CorrelationId = fulfilmentServiceQueueMessage.CorrelationId;
            ExchangeSetDirectory = Path.Combine(BatchDirectory, fileShareServiceConfiguration.ExchangeSetFileFolder);
            ExchangeSetEncRootDirectory = Path.Combine(ExchangeSetDirectory, fileShareServiceConfiguration.EncRoot);
            AioExchangeSetDirectory = Path.Combine(BatchDirectory, fileShareServiceConfiguration.AioExchangeSetFileFolder);
            AioExchangeSetEncRootDirectory = Path.Combine(AioExchangeSetDirectory, fileShareServiceConfiguration.EncRoot);
            LargeExchangeSetFolderName = periodicOutputServiceConfiguration.LargeExchangeSetFolderName;
            LargeExchangeSetDirectory = Path.Combine(BatchDirectory, periodicOutputServiceConfiguration.LargeExchangeSetFolderName);
            LargeExchangeSetEncRootDirectory = Path.Combine(LargeExchangeSetDirectory, "{1}", fileShareServiceConfiguration.EncRoot);
            ErrorFile = Path.Combine(BatchDirectory, fileShareServiceConfiguration.ErrorFileName);
        }
    }
}
