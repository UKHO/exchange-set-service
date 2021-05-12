using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAuthTokenProvider
    {
        public Task<string> GetManagedIdentityAuthAsync(string scope);
    }
}
