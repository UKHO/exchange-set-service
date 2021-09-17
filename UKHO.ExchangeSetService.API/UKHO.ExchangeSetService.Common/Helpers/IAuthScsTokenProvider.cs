using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAuthScsTokenProvider
    {
        public Task<string> GetManagedIdentityAuthAsync(string resource);
    }
}
