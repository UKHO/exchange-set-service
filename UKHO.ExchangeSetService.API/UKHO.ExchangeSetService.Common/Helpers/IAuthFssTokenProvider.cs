using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAuthFssTokenProvider
    {
        public Task<string> GetManagedIdentityAuthAsync(string resource);
    }
}
