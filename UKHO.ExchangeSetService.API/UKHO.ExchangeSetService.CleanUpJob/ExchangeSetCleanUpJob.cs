using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.CleanUpJob.Services;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.CleanUpJob
{
    [ExcludeFromCodeCoverage]
    public class ExchangeSetCleanUpJob
    {
        private readonly IExchangeSetCleanUpService exchangeSetCleanUpService;
        private readonly ILogger<ExchangeSetCleanUpJob> logger;
        private readonly IOptions<EssFulfilmentStorageConfiguration> storageConfig;
        public ExchangeSetCleanUpJob(IExchangeSetCleanUpService exchangeSetCleanUpService,
                                     ILogger<ExchangeSetCleanUpJob> logger,
                                     IOptions<EssFulfilmentStorageConfiguration> storageConfig)
        {
            this.exchangeSetCleanUpService = exchangeSetCleanUpService;
            this.logger = logger;
            this.storageConfig = storageConfig;
        }
        public async Task ProcessCleanUp()
        {
            logger.LogInformation(EventIds.ESSCleanUpJobRequestStart.ToEventId(), "Exchange set service clean up web job started at " + DateTime.Now + " for storage account name {StorageAccountName} & queue {QueueName} ", storageConfig.Value.StorageAccountName , storageConfig.Value.QueueName);

            await exchangeSetCleanUpService.DeleteHistoricFoldersAndFiles();

            logger.LogInformation(EventIds.ESSCleanUpJobRequestCompleted.ToEventId(), "Exchange set service clean up web job completed at " + DateTime.Now + " for storage account name {StorageAccountName} & queue {QueueName} ", storageConfig.Value.StorageAccountName, storageConfig.Value.QueueName);
        }
    }
}
