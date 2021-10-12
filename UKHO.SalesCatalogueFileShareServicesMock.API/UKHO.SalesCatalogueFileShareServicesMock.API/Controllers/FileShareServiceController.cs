using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response;
using UKHO.SalesCatalogueFileShareServicesMock.API.Services;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Controllers
{
    [ApiController]
    public class FileShareServiceController : BaseController
    {
        private readonly FileShareService fileShareService;
        public Dictionary<string, string> ErrorsCreateBatch { get; set; }

        public FileShareServiceController(IHttpContextAccessor httpContextAccessor, FileShareService fileShareService) : base(httpContextAccessor)
        {
            this.fileShareService = fileShareService;
            ErrorsCreateBatch = new Dictionary<string, string>
            {
                { "source", "RequestBody" },
                { "description", "Either body is null or malformed." }
            };
        }

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

        [HttpGet]
        [Route("/batch")]
        public IActionResult GetBatches([FromQuery] int? limit, [FromQuery] int start = 0, [FromQuery(Name = "$filter")] string filter = "")
        {
            if (limit != null && !string.IsNullOrEmpty(filter))
            {
                var response = fileShareService.GetGetBatches(filter);
                if (response != null)
                {
                    return Ok(response);
                }
            }
            return BadRequest();
        }
    }
}
