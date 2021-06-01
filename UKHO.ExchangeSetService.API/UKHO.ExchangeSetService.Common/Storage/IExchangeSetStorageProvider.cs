using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Storage
{
    public interface IExchangeSetStorageProvider
    {
        /// <summary>
        /// Saves/uploads a Sales catalogue Service response
        /// </summary>       
        /// <param name="salesCatalogueResponse">Sales catalogue response</param>       
        /// <param name="batchId">batch Id</param>        
        /// <returns></returns>
        Task<bool> SaveSalesCatalogueResponse(SalesCatalogueResponse salesCatalogueResponse, string batchId);

    }
}
