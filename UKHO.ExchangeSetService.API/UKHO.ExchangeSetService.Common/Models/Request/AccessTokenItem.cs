using System;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Models.Request
{
    [ExcludeFromCodeCoverage]
    public class AccessTokenItem
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresIn { get; set; }
    }
}
