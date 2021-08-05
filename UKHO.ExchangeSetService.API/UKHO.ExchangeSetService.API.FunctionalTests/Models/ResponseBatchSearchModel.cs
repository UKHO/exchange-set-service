using System.Collections.Generic;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    public class ResponseBatchSearchModel
    {
        public int Count { get; set; }
        public int Total { get; set; }
        public List<ResponseBatchDetailsModel> Entries { get; set; }       
    }   
}
