using System;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.Response;
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
        /// <param name="exchangeSetStandard">Default value of exchangeSetStandard is s63, if standard passed as s57, exchangeSetStandard will be of type s57</param>
        /// <param name="correlationId">batch Id</param>
        /// <param name="expiryDate">batch Id</param>
        /// <param name="scsRequestDateTime">Scs Request DateTime</param>
        /// <param name="isEmptyEncExchangeSet">"create empty enc exchange set"</param>
        /// <param name="isEmptyAioExchangeSet">"create empty aio exchange set"</param>
        /// <param name="exchangeSetResponse">Exchange set response</param>
        /// <param name="exchangeSetLayout">Exchange set layout</param>
        /// <returns></returns>
        Task<bool> SaveSalesCatalogueStorageDetails(SalesCatalogueProductResponse salesCatalogueResponse, string batchId, string callBackUri, string exchangeSetStandard, string correlationId, string expiryDate, DateTime scsRequestDateTime, bool isEmptyEncExchangeSet, bool isEmptyAioExchangeSet, ExchangeSetResponse exchangeSetResponse, string exchangeSetLayout);
    }
}
