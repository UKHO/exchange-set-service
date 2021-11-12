using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UKHO.SalesCatalogueFileShareServicesMock.API.Helpers;
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

        private readonly IWebHostEnvironment _hostingEnvironment;

        public FileShareServiceController(IHttpContextAccessor httpContextAccessor, FileShareService fileShareService, IConfiguration configuration, IWebHostEnvironment hostingEnvironment) : base(httpContextAccessor)
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
            this._hostingEnvironment = hostingEnvironment;
        }

        [HttpPost]
        [Route("/batch")]
        public IActionResult CreateBatch([FromBody] BatchRequest batchRequest)
        {
            string contentRootPath = _hostingEnvironment.ContentRootPath;

            if (batchRequest != null && !string.IsNullOrEmpty(batchRequest.BusinessUnit))
            {
                string batchFolderPath = FileHelper.GetBatchFolderPath(contentRootPath);
                var response = fileShareService.CreateBatch(batchFolderPath);
                if (response != null)
                {
                    return Created(string.Empty, response);
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
        [Route("/batch/{batchId}/files/{fileName}")]
        public FileResult DownloadFile(string fileName)
        {
            byte[] bytes = null;
            if (!string.IsNullOrEmpty(fileName))
            {
                bytes = fileShareService.GetFileData(fileName);
            }

            return File(bytes, "application/octet-stream", fileName);
        }

        [HttpPut]
        [Route("batch/{batchId}/files/{fileName}/{blockId}")]
        [Produces("application/json")]
        [Consumes("application/octet-stream")]
        public IActionResult UploadBlockOfFile( [FromRoute, SwaggerSchema(Format = "GUID"), SwaggerParameter(Required = true)] string batchId,
                                                [FromRoute, SwaggerParameter(Required = true)] string fileName, [FromRoute, SwaggerParameter(Required = true)] string blockId,
                                                [FromHeader(Name = "Content-Length"), SwaggerSchema(Format = ""), SwaggerParameter(Required = true)] decimal? contentLength,
                                                [FromHeader(Name = "Content-MD5"), SwaggerSchema(Format = "byte"), SwaggerParameter(Required = true)] string contentMD5,
                                                [FromHeader(Name = "Content-Type"), SwaggerSchema(Format = "MIME"), SwaggerParameter(Required = true)] string contentType,
                                                [FromBody] object data )
        {
            string contentRootPath = _hostingEnvironment.ContentRootPath;
            if (!string.IsNullOrEmpty(batchId) && data != null && !string.IsNullOrEmpty(blockId) && !string.IsNullOrEmpty(contentMD5) && !string.IsNullOrEmpty(contentType))
            {
                string batchFolderPath = FileHelper.GetBatchFolderPath(contentRootPath);
                var response = fileShareService.UploadBlockOfFile(batchId, fileName, data, batchFolderPath);
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
            string contentRootPath = _hostingEnvironment.ContentRootPath;
            if (!string.IsNullOrEmpty(batchId) && !string.IsNullOrEmpty(fileName) && payload != null)
            {
                string batchFolderPath = FileHelper.GetBatchFolderPath(contentRootPath);
                var response = fileShareService.CheckBatchWithZipFileExist(batchId, fileName, batchFolderPath);
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
            string contentRootPath = _hostingEnvironment.ContentRootPath;
            if (!string.IsNullOrEmpty(batchId) && body != null)
            {
                string batchFolderPath = FileHelper.GetBatchFolderPath(contentRootPath);
                var response = fileShareService.CheckBatchWithZipFileExist(batchId, body.Select(a => a.FileName).FirstOrDefault(), batchFolderPath);
                if (response)
                {
                    return Accepted(new BatchCommitResponse() { Status = new Status { URI = $"/batch/{batchId}/status" } });
                }
            }
            return BadRequest(new { CorrelationId = GetCurrentCorrelationId(), Errors = ErrorsCommitBatch });
        }
    }
}
