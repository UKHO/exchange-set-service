using System;
using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class ExchangeSetInstance : ISmallExchangeSetInstance, IMediumExchangeSetInstance, ILargeExchangeSetInstance
    {
        private int instanceNumber = 0;
        private readonly Object _lock = new Object();

        public int GetCurrentInstanceNumber() => instanceNumber;

        public int GetInstanceNumber(int maxInstanceCount)
        {
            lock (_lock)
            {
                if (instanceNumber >= maxInstanceCount)
                {
                    ResetInstanceNumber();
                }
                instanceNumber++;
            }
            return instanceNumber;
        }

        private void ResetInstanceNumber() => instanceNumber = 0;
    }
}
