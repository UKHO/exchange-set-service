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
        /// <param name="callBackUri">batch Id</param>   
        /// <param name="correlationId">batch Id</param>   
        /// <param name="expiryDate">batch Id</param>
        /// <returns></returns>
        Task<bool> SaveSalesCatalogueStorageDetails(SalesCatalogueProductResponse salesCatalogueResponse, string batchId, string callBackUri, string correlationId, string expiryDate);

    }
}
