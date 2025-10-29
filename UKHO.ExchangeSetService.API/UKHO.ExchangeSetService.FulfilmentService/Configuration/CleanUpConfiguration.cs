namespace UKHO.ExchangeSetService.FulfilmentService.Configuration
{
    public class CleanUpConfiguration
    {
        public bool Enabled { get; set; }
        public int NumberOfDays { get; set; }
        public string DailyMaintenanceCronSchedule { get; set; }
    }
}
