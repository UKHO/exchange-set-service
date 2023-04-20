using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    [ExcludeFromCodeCoverage]
    public class FulfilmentDataResponse
    {
        public string BatchId { get; set; }

        public string ProductName { get; set; }

        public int EditionNumber { get; set; }

        public int UpdateNumber { get; set; }

        public IEnumerable<string> FileUri { get; set; }

        public IEnumerable<BatchFile> Files { get; set; }

        public int FileShareServiceSearchQueryCount { get; set; }
    }
}
