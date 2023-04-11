namespace UKHO.ExchangeSetService.Common.Configuration
{
    public class AioConfiguration
    {
        public bool? AioEnabled { get; set; }
        public string AioCells { get; set; }
        public bool IsAioEnabled
        {
            get
            {
                return (bool)(AioEnabled.HasValue ? AioEnabled : false);
            }
            set
            {
                AioEnabled = value;
            }
        }
    }
}