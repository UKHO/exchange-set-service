using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Net;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Request;
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
        [Route("/batch")]
        public IActionResult CreateBatch([FromBody] BatchRequest batchRequest)
        {
            if (batchRequest != null && !string.IsNullOrEmpty(batchRequest.BusinessUnit))
            {
                var response = fileShareService.CreateBatch(batchRequest);
                if (response != null)
                {
                    return Ok(response);
                }
            }
            return BadRequest();
        }

        [HttpGet]
        [Route("/batch")]
        public IActionResult GetBatches([FromQuery] int? limit, [FromQuery] int start = 0, [FromQuery(Name = "$filter")] string filter = "")
        {
            if (limit != null && !string.IsNullOrEmpty(filter))
            {
                var response = fileShareService.GetBatches(filter);
                if (response != null)
                {
                    return Ok(response);
                }
            }
            return BadRequest();
        }

        [HttpGet]
        [Route("/batch/{batchId}/files/{filesName}")]
        public FileResult DownloadFile(string filesName)
        {
            byte[] bytes = null;
            if (!string.IsNullOrEmpty(filesName))
            {
                bytes = fileShareService.GetFileData(filesName);
            }

            return File(bytes, "application/octet-stream", filesName);
        }

        [HttpPut]
        [Route("batch/{batchId}/files/{fileName}/{blockId}")]
        [Produces("application/json")]
        [Consumes("application/octet-stream")]
        public IActionResult UploadBlockOfFile( [FromRoute, SwaggerSchema(Format = "GUID"), SwaggerParameter(Required = true)] string batchId,
                                                           [FromRoute, SwaggerParameter(Required = true)] string fileName,
                                                           [FromRoute, SwaggerParameter(Required = true)] string blockId,
                                                           [FromHeader(Name = "Content-Length"), SwaggerSchema(Format = ""), SwaggerParameter(Required = true)] decimal? contentLength,
                                                           [FromHeader(Name = "Content-MD5"), SwaggerSchema(Format = "byte"), SwaggerParameter(Required = true)] string contentMD5,
                                                           [FromHeader(Name = "Content-Type"), SwaggerSchema(Format = "MIME"), SwaggerParameter(Required = true)] string contentType,
                                                           [FromBody] Object data )
        {
            if (batchId != null && data != null)
            {
                var response = fileShareService.UploadBlockOfFile(batchId, fileName, data);
                if (response)
                {
                    return StatusCode((int)HttpStatusCode.Created);
                }
            }

            return BadRequest();
        }
    }
}
