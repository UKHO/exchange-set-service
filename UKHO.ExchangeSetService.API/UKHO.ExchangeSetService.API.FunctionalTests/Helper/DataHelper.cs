using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class DataHelper
    {
        public ProductVersionModel ProductVersionmodel { get; set; }

        public ProductVersionModel GetProductVersionModelData(string productName, int? editionNumber, int? updateNumber)
        {
            ProductVersionmodel = new ProductVersionModel()
            {
                ProductName = productName,
                EditionNumber = editionNumber,
                UpdateNumber = updateNumber



            };

            return ProductVersionmodel;
        }
    }
}
