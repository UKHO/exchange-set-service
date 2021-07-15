namespace UKHO.ExchangeSetService.Common.Configuration
{
    public interface IMediumExchangeSetInstance
    {
        int GetCurrentInstaceCount();
        int GetInstanceCount();
        void ResetInstanceCount();
    }
}
