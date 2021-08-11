namespace UKHO.ExchangeSetService.FulfilmentService.Configuration
{
    public class EssCallBackConfiguration
    {
        public string SpecVersion { get; set; }
        public string Type { get; set; }
        public string Source { get; set; }
        public string SubjectForCreated { get; set; }
        public string SubjectForErrors { get; set; }
        public string ErrorFileUrl { get; set; }
        public string Reason { get; set; }
    }
}