
namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    public class CreateBatchResponseModel
    {
        public string BatchId { get; set; }

        public string BatchStatusUri { get; set; }

        public string ExchangeSetBatchDetailsUri { get; set; }

        public string BatchExpiryDateTime { get; set; }

        public string ExchangeSetFileUri { get; set; }
    }
}
