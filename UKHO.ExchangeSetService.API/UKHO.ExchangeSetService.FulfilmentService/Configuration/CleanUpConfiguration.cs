namespace UKHO.ExchangeSetService.FulfilmentService.Configuration
{
    public class CleanUpConfiguration
    {
        public bool ContinuousCleanupEnabled { get; set; }
        public int NumberOfDays { get; set; }
        public string DailyMaintenanceCronSchedule { get; set; }
    }
}
