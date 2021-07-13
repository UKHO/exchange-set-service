using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    public class WriteBlocksToFileMetaData
    {
        public string AccessToken { get; set; }
        public string BatchId { get; set; }
        public string FileName { get; set; }
        public List<string> BlockIds { get; set; }
    }
}
