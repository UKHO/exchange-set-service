using System;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public abstract class BaseExchangeSetInstance
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
