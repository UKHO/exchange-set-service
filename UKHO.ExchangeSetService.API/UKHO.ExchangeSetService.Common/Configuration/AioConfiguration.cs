namespace UKHO.ExchangeSetService.Common.Configuration
{
    public class AioConfiguration
    {
        public bool? AioEnabled { get; set; }
        public bool IsAioEnabled
        {
            get
            {
                return (bool)(!AioEnabled.HasValue ? false : AioEnabled);
            }
            set
            {
                AioEnabled = value;
            }
        }
        public string AioCells { get; set; }
    }
}