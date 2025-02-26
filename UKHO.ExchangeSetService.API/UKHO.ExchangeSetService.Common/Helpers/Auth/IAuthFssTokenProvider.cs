using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers.Auth
{
    public interface IAuthFssTokenProvider
    {
        public Task<string> GetManagedIdentityAuthAsync(string resource);
    }
}
