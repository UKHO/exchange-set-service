using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Request;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response;
using UKHO.SalesCatalogueFileShareServicesMock.API.Services;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Controllers
{
    [ApiController]
    public class FileShareServiceController : BaseController
    {
        private readonly FileShareService fileShareService;
        public Dictionary<string, string> ErrorsCreateBatch { get; set; }
        public Dictionary<string, string> ErrorsPutBlocksInFile { get; set; }
        public Dictionary<string, string> ErrorsCommitBatch { get; set; }

        public FileShareServiceController(IHttpContextAccessor httpContextAccessor, FileShareService fileShareService) : base(httpContextAccessor)
        {
            this.fileShareService = fileShareService;
            ErrorsCreateBatch = new Dictionary<string, string>
            {
                { "source", "RequestBody" },
                { "description", "Either body is null or malformed." }
            };
            ErrorsPutBlocksInFile = new Dictionary<string, string>
            {
                { "source", "BatchId" },
                { "description", "Invalid or non-existing batch ID." }
            };
            ErrorsCommitBatch = new Dictionary<string, string>
            {
                { "source", "BatchId" },
                { "description", "BatchId does not exist." }
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

        [HttpPut]
        [Route("batch/{batchId}/files/{fileName}")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public IActionResult PutBlocksInFile([FromRoute, SwaggerSchema(Format = "GUID"), SwaggerParameter(Required = true)] string batchId,
                                                               [FromRoute, SwaggerParameter(Required = true)] string fileName,
                                                               [FromBody, SwaggerParameter(Required = true)] FileCommitPayload payload)
        {
            if (!string.IsNullOrEmpty(batchId) && !string.IsNullOrEmpty(fileName))
            {
                var response = fileShareService.CheckBatchWithZipFileExist(batchId, fileName);
                if (response)
                {
                    return StatusCode((int)HttpStatusCode.NoContent);
                }
            }
            return BadRequest(new { CorrelationId = GetCurrentCorrelationId(), Errors = ErrorsPutBlocksInFile });
        }

        [HttpPut]
        [Route("/batch/{batchId}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public IActionResult CommitBatch([FromRoute] string batchId, [FromBody] List<BatchCommitRequest> body)
        {
            if (!string.IsNullOrEmpty(batchId) && body != null)
            {
                var response = fileShareService.CheckBatchWithZipFileExist(batchId, body.Select(a => a.FileName).FirstOrDefault());
                if (response)
                {
                    return Accepted(new BatchCommitResponse() { Status = new Status { URI = $"/batch/{batchId}/status" } });
                }
            }
            return BadRequest(new { CorrelationId = GetCurrentCorrelationId(), Errors = ErrorsCommitBatch });
        }
    }
}
