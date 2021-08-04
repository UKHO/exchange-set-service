namespace UKHO.ExchangeSetService.Common.Configuration
{
    public interface IMediumExchangeSetInstance
    {
        int GetCurrentInstanceNumber();
        int GetInstanceNumber(int mediumMaxInstanceCount);
    }
}
