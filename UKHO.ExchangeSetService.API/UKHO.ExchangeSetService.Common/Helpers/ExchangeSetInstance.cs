using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class ExchangeSetInstance : ISmallExchangeSetInstance, IMediumExchangeSetInstance, ILargeExchangeSetInstance
    {
        private int instanceCount = 0;

        public int GetCurrentInstanceCount() => instanceCount;

        public int GetInstanceCount(int maxInstanceCount)
        {
            if (instanceCount >= maxInstanceCount)
            {
                ResetInstanceCount();
            }
            instanceCount += 1;
            return instanceCount;
        }

        public void ResetInstanceCount() => instanceCount = 0;
    }
}
