using System;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class AioConfiguration
    {
        public bool? AioEnabled { get; set; }
        public string AioCells { get; set; }
        public bool IsAioEnabled
        {
            get
            {
                return true;
            }
            private set
            {
                //AioEnabled = true;
            }
        }
    }
}
