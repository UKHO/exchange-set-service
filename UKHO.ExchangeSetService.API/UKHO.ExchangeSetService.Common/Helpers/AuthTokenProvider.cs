using Azure.Core;
using Azure.Identity;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class AuthTokenProvider : IAuthTokenProvider
    {
        public async Task<string> GetManagedIdentityAuthAsync(string scope)
        {
            var tokenCredential = new DefaultAzureCredential();
            var accessToken = await tokenCredential.GetTokenAsync(
                new TokenRequestContext(scopes: new string[] { scope + "/.default" }) { }
            );

            return accessToken.Token;
        }
    }
}
