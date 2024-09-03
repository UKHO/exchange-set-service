using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UKHO.ExchangeSetService.Common.Models.Request
{
    public class CustomEventGridEvent
    {
        public CustomEventGridEvent(JObject request)
        {
            JsonConvert.PopulateObject(request.ToString(), this);
        }

        public EnterpriseEventCacheDataRequest Data { get; init; } = null;
    }

    // Will use one of theses classes to create the request object
    // not sure which one yet.

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

