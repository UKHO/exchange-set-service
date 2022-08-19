using CsvHelper.Configuration.Attributes;

namespace UKHO.ExchangeSetService.Common.Models.SalesCatalogue
{
    public class ProductsCsvDetails
    {
        [Name("ENC Cell")]
        public string ProductName { get; set; }
        [Name("Latest Edition No.")]
        public int? EditionNumber { get; set; }
        [Name("Latest Update No.")]
        public int? UpdateNumber { get; set; }
        [Name("Latest Edition Issue Date")]
        public string EditionIssueDate { get; set; }
        [Name("Latest Update Issue Date")]
        public string UpdateIssueDate { get; set; }
    }
}
