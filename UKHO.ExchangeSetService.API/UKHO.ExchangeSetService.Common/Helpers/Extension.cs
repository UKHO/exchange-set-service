using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public static class Extensions
    {        
        public static async Task<T> ReadAsTypeAsync<T>(this HttpResponseMessage httpResponseMessage)
        {
            string bodyJson = await httpResponseMessage.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(bodyJson);
        }
    }
}
