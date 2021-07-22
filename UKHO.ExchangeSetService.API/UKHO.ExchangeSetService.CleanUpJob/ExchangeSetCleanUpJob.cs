using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.CleanUpJob.Services;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.CleanUpJob
{
    [ExcludeFromCodeCoverage]
    public class ExchangeSetCleanUpJob
    {
        private readonly IExchangeSetCleanUpService exchangeSetCleanUpService;
        private readonly ILogger<ExchangeSetCleanUpJob> logger;
        public ExchangeSetCleanUpJob(IExchangeSetCleanUpService exchangeSetCleanUpService,
                                     ILogger<ExchangeSetCleanUpJob> logger)
        {
            this.exchangeSetCleanUpService = exchangeSetCleanUpService;
            this.logger = logger;
        }
        public async Task ProcessCleanUp()
        {
            logger.LogInformation(EventIds.ESSCleanUpJobRequestStart.ToEventId(), "Exchange set service clean up web job started at " + DateTime.Now);
            logger.LogWarning("Start clean up web at " + DateTime.Now);

            await exchangeSetCleanUpService.DeleteHistoricFoldersAndFiles();

            logger.LogWarning("End clean up web at " + DateTime.Now);
            logger.LogInformation(EventIds.ESSCleanUpJobRequestCompleted.ToEventId(), "Exchange set service clean up web job completed at " + DateTime.Now);
        }
    }
}
