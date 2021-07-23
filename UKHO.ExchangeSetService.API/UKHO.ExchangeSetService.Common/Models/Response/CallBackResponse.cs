namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class CallBackResponse
    {
        public string Specversion { get; set; }
        public string Type { get; set; }
        public string Source { get; set; }
        public string Id { get; set; }
        public string Time { get; set; }
        public string Subject { get; set; }
        public string DataContentType { get; set; }
        public ExchangeSetResponse Data { get; set; }
    }
}