using System;

namespace UKHO.ExchangeSetService.Common.Models.Request
{
    public class AccessTokenItem
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresIn { get; set; }
    }
}
