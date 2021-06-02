using Newtonsoft.Json;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    public class SearchBatchResponse
    {
        public int Count { get; set; }

        public int Total { get; set; }

        public List<BatchDetail> Entries { get; set; }

        [JsonProperty("_links")]
        public PagingLinks Links { get; set; }
    }
}
