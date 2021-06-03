using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    public class FulfillmentDataResponse
    {
        public string BatchId { get; set; }

        public string ProductName { get; set; }

        public int EditionNumber { get; set; }

        public int UpdateNumber { get; set; }

        public IEnumerable<string> FileUri { get; set; }
    }
}
