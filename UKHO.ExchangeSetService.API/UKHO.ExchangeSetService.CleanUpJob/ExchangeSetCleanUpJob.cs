using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.CleanUpJob.Services;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.CleanUpJob
{
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
        public async Task ProcessCleanUp([TimerTrigger("%ScheduleTimer%", RunOnStartup = true)] TimerInfo timerInfo)
        {
            logger.LogInformation(EventIds.CreateExchangeSetRequestStart.ToEventId(), "Create Exchange Set Clean Up web job started.");
            await exchangeSetCleanUpService.CleanUpFoldersFiles();
        }
    }
}
