//using Microsoft.Azure.EventGrid.Models   No Longer supported
using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.Request;

// This class is not valid when using Azure.Messaging.EventGrid
//public class CustomEventGridEvent  : EventGridEvent
//"{"
//    "public string Type { get; set; }"
//"}"

// This is Temporary while libraries are being updated
//This will eventually be placed in its own file.
public class DataRequestWrapper
{
    public EnterpriseEventCacheDataRequest Data { get; set; }
}

public static class EnterpriseEventCacheDataRequestFactory
{
    class Wrapper
    {
        public EnterpriseEventCacheDataRequest Data { get; set; } = null;
    }

    public static EnterpriseEventCacheDataRequest CreateRequest(string request)
    {
        var wrap = new Wrapper();
        JsonConvert.PopulateObject(request, wrap);
        return wrap.Data;
    }
    
}