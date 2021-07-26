namespace UKHO.ExchangeSetService.Common.Configuration
{
    public interface IMediumExchangeSetInstance
    {
        int GetCurrentInstanceCount();
        int GetInstanceCount(int maxInstanceCount);
        void ResetInstanceCount();
    }
}
