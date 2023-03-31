namespace UKHO.ExchangeSetService.Common.Configuration
{
    public class AioConfiguration
    {
        public bool? IsAioEnabled { get; set; }
        public bool AioEnabled
        {
            get
            {
                return (bool)(!IsAioEnabled.HasValue ? false : IsAioEnabled);
            }
            set
            {
                IsAioEnabled = value;
            }
        }
        public string AioCells { get; set; }
    }
}