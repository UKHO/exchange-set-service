using System;

namespace UKHO.ExchangeSetService.Common.Extensions
{
    public static class CallbackUriExtensions
    {
        public static bool IsValidCallbackUri(this string callbackUri)
        {
            try
            {
                Uri baseUri = new Uri(callbackUri);
                return (baseUri.Scheme == Uri.UriSchemeHttps);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
