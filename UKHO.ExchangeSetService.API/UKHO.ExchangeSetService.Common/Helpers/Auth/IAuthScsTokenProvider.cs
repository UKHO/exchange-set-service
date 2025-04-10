using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers.Auth
{
    public interface IAuthScsTokenProvider
    {
        public Task<string> GetManagedIdentityAuthAsync(string resource);
    }
}
