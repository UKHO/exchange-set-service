using System;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    public class CreateBatchResponseModel
    {
        public string BatchId { get; set; }

        public string BatchStatusUri { get; set; }

        public DateTime BatchExpiryDateTime { get; set; }

        public string ExchangeSetFileUri { get; set; }

    }
}
