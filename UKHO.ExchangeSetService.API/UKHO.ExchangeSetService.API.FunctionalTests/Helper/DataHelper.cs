using System.Collections.Generic;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class DataHelper
    {
        public ProductVersionModel ProductVersionModel { get; set; }       

        public ProductVersionModel GetProductVersionModelData(string productName, int? editionNumber, int? updateNumber)
        {
            ProductVersionModel = new ProductVersionModel()
            {
                ProductName = productName,
                EditionNumber = editionNumber,
                UpdateNumber = updateNumber
            };

            return ProductVersionModel;
        }

        public List<string> GetProductIdentifierData()
        {
            return new List<string>() { "DE5NOBRK", "DE4NO18Q", "DE416080" };         
            
        }

        public List<string> GetOnlyProductIdentifierData()
        {
            return new List<string>() { "DE260001" };

        }
    }
}
