using Microsoft.AspNetCore.Mvc;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Controllers
{
    [ApiController]
    public class FileShareServiceController : ControllerBase
    {
        [HttpPost]
        [Route("batch")]
        public IActionResult CreateBatch(string correlationId)
        {
            if (!string.IsNullOrEmpty(correlationId))
            {
                return Ok(new CreateBatchResponse());
            }
            return BadRequest();
        }
    }
}
