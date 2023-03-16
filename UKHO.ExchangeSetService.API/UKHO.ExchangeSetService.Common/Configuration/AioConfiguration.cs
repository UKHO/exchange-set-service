using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Configuration
{
    public class AioConfiguration
    {
        public bool AioEnabled { get; set; }
        public string AioCells { get; set; }
        public static List<string> AioCellList { get; set; }
    }
}