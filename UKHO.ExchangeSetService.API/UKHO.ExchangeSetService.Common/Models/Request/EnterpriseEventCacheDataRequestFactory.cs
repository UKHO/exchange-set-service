using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.Request
{
    public static class EnterpriseEventCacheDataRequestFactory 
    {
        class Wrapper
        {
            public EnterpriseEventCacheDataRequest Data { get; init; } = null;
        }

        public static EnterpriseEventCacheDataRequest CreateRequest(JObject request)
        {
            var wrap = new Wrapper();
            JsonConvert.PopulateObject(request.ToString(), wrap);
            return wrap.Data;
        }
    }
}
