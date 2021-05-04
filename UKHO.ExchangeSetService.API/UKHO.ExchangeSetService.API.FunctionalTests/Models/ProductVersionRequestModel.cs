using System.Collections.Generic;


namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    public class ProductVersionRequestModel
    {
        public List<ProductVersionModel> ProductVersions { get; set; }
        public string CallbackUri { get; set; }
    }
}
