using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.FileBuilders
{
    public interface IExchangeSetBuilder
    {
        Task CreateStandardExchangeSet(SalesCatalogueServiceResponseQueueMessage message, SalesCatalogueProductResponse response, List<Products> essItems, string exchangeSetPath, SalesCatalogueDataResponse salesCatalogueEssDataResponse, string businessUnit);
        Task<bool> CreateStandardLargeMediaExchangeSet(SalesCatalogueServiceResponseQueueMessage message, string homeDirectoryPath, string currentUtcDate, LargeExchangeSetDataResponse largeExchangeSetDataResponse, string largeExchangeSetFolderName, string largeMediaExchangeSetFilePath, CancellationToken cancellationToken);
        Task<bool> CreateAioExchangeSet(SalesCatalogueServiceResponseQueueMessage message, string currentUtcDate, string homeDirectoryPath, List<Products> aioItems, SalesCatalogueDataResponse salesCatalogueEssDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse);
        Task<List<FulfilmentDataResponse>> QueryFileShareServiceFiles(SalesCatalogueServiceResponseQueueMessage message, List<Products> products, string exchangeSetRootPath, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string businessUnit);
    }
}
