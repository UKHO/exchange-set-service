namespace UKHO.ExchangeSetService.Common.Configuration
{
    public class ExchangeSetInstance : ISmallExchangeSetInstance, IMediumExchangeSetInstance, ILargeExchangeSetInstance
    {
        private int instanceCount = 0;

        public int GetCurrentInstaceCount() => instanceCount;

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
