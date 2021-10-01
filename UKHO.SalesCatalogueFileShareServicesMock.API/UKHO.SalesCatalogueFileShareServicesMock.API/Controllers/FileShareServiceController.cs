using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Controllers
{
    [ApiController]
    public class FileShareServiceController : ControllerBase
    {
        [HttpPost]
        [Route("batch")]
        public async Task<IActionResult> CreateBatch(string correlationId)
        {
            if (!string.IsNullOrEmpty(correlationId))
            {
                await Task.CompletedTask;
                return Ok(new CreateBatchResponse());
            }
            return BadRequest();
        }
    }
}
