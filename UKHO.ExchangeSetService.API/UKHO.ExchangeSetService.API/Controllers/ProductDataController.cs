using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace UKHO.ExchangeSetService.API.Controllers
{
    public class ProductDataController : BaseController<ProductDataController>
    {
        public ProductDataController(IHttpContextAccessor contextAccessor,
           ILogger<ProductDataController> logger
          )
       : base(contextAccessor, logger)
        {
           
        }
       
    }
}
