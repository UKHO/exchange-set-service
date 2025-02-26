using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class AioConfiguration
    {
        private bool? AioEnabled { get; set; }
        public string AioCells { get; set; }
        public bool IsAioEnabled
        {
            get
            {
                return true;
            }
            private set
            {
                
            }
        }
    }
}
