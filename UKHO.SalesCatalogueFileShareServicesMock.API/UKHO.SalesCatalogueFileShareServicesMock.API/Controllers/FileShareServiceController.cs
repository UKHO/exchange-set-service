﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using UKHO.SalesCatalogueFileShareServicesMock.API.Common;
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
        public Dictionary<string, string> ErrorsAddFileinBatch { get; set; }
        protected IConfiguration configuration;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfiguration;
        private readonly string homeDirectoryPath;

        public FileShareServiceController(IHttpContextAccessor httpContextAccessor, FileShareService fileShareService, IConfiguration configuration, IOptions<FileShareServiceConfiguration> fileShareServiceConfiguration) : base(httpContextAccessor)
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
            ErrorsAddFileinBatch = new Dictionary<string, string>
            {
                { "source","FileError" },
                { "description","Error while creating file" }
            };
            this.configuration = configuration;
            this.fileShareServiceConfiguration = fileShareServiceConfiguration;

            homeDirectoryPath = configuration["HOME"];
        }

        [HttpPost]
        [Route("/batch")]
        public IActionResult CreateBatch([FromBody] BatchRequest batchRequest)
        {
            if (batchRequest != null && !string.IsNullOrEmpty(batchRequest.BusinessUnit))
            {
                var response = fileShareService.CreateBatch(homeDirectoryPath);
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
            if (!string.IsNullOrEmpty(filter))
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
        public ActionResult DownloadFile(string batchId, string fileName)
        {
            byte[] bytes = null;

             (bool validated, string validatedBatchId) = ValidateBatchId(batchId);

            if (fileName == "DE260001.000")
            {
                HttpContext.Response.Headers.Append("Location", fileShareServiceConfiguration.Value.DownloadENCFiles307ResponseUri);
                return StatusCode(StatusCodes.Status307TemporaryRedirect);
            }
            if (validated && !string.IsNullOrEmpty(fileName) && !Path.IsPathRooted(fileName))
            {
                bytes = fileShareService.GetFileData(homeDirectoryPath, validatedBatchId, Path.GetFileName(fileName));
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
            (bool validated, string validatedBatchId) = ValidateBatchId(batchId);

            if (validated && !Path.IsPathRooted(fileName) && data != null && !string.IsNullOrEmpty(blockId) && !string.IsNullOrEmpty(contentMD5) && !string.IsNullOrEmpty(contentType))
            {
                var response = fileShareService.UploadBlockOfFile(validatedBatchId, Path.GetFileName(fileName), data, homeDirectoryPath);
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
            (bool validated, string validatedBatchId) = ValidateBatchId(batchId);

            if (validated && !string.IsNullOrEmpty(fileName) && !Path.IsPathRooted(fileName) && payload != null)
            {
                var response = fileShareService.CheckBatchWithZipFileExist(validatedBatchId, Path.GetFileName(fileName), homeDirectoryPath);
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
            (bool validated, string validatedBatchId) = ValidateBatchId(batchId);

            string fileName = body?.Select(a => a.FileName).FirstOrDefault();

            if (validated && !string.IsNullOrEmpty(fileName) && !Path.IsPathRooted(fileName))
            {
                var response = fileShareService.CheckBatchWithZipFileExist(validatedBatchId, Path.GetFileName(fileName), homeDirectoryPath);
                if (response)
                {
                    return Accepted(new BatchCommitResponse() { Status = new Status { URI = $"/batch/{batchId}/status" } });
                }
            }
            return BadRequest(new { CorrelationId = GetCurrentCorrelationId(), Errors = ErrorsCommitBatch });
        }


        [HttpPost]
        [Route("batch/{batchId}/files/{fileName}")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public IActionResult AddFileToBatch([FromRoute, SwaggerSchema(Format = "GUID"), SwaggerParameter(Required = true)] string batchId,
                                            [FromRoute, SwaggerParameter(Required = true)] string fileName,
                                            [FromHeader(Name = "X-MIME-Type"), SwaggerSchema(Format = "MIME")] string contentType,
                                            [FromHeader(Name = "X-Content-Size"), SwaggerSchema(Format = ""), SwaggerParameter(Required = true)] long? xContentSize,
                                            [FromBody] FileRequest attributes)
        {
            (bool validated, string validatedBatchId) = ValidateBatchId(batchId);

            if (validated)
            {
                var response = fileShareService.CheckBatchFolderExists(validatedBatchId, homeDirectoryPath);
                if (response)
                {
                    return StatusCode(StatusCodes.Status201Created);
                }
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { CorrelationId = GetCurrentCorrelationId(), Errors = ErrorsAddFileinBatch });
        }

        [HttpGet]
        [Route("/batch/{batchId}/status")]
        [Produces("application/json")]
        public IActionResult GetBatchStatus([FromRoute, Required] string batchId)
        {
            (bool validated, string validatedBatchId) = ValidateBatchId(batchId);

            if (validated)
            {
                BatchStatusResponse batchStatusResponse = fileShareService.GetBatchStatus(validatedBatchId, homeDirectoryPath);
                if (batchStatusResponse.Status == "Committed")
                {
                    return new OkObjectResult(batchStatusResponse);
                }
            }
            return Unauthorized();
        }

        [HttpPost]
        [Route("/cleanUp")]
        public IActionResult CleanUp([FromBody] List<string> batchId)
        {
            var validatedBatchIds = ValidateBatchIds(batchId);
            if (validatedBatchIds.Count > 0)
            {
                var response = fileShareService.CleanUp(validatedBatchIds, homeDirectoryPath);
                if (response)
                {
                    return Ok();
                }
            }
            return BadRequest();
        }
        [HttpGet]
        [Route("/batch/{batchId}/redirectFiles/{fileName}")]
        public ActionResult RedirectDownloadFile(string batchId, string fileName)
        {
            byte[] bytes = null;
            (bool validated, string validatedBatchId) = ValidateBatchId(batchId);

            if (validated && !string.IsNullOrEmpty(fileName) && !Path.IsPathRooted(fileName))
            {
                bytes = fileShareService.GetFileData(homeDirectoryPath, validatedBatchId, Path.GetFileName(fileName));
                HttpContext.Response.Headers.Append("x-redirect-status", "true");
            }

            return File(bytes, "application/octet-stream", fileName);
        }

        private static (bool, string) ValidateBatchId(string batchId)
        {
            var validated = Guid.TryParse(batchId, out var result);
            return (validated, result.ToString());
        }

        private static List<string> ValidateBatchIds(IEnumerable<string> batchIds)
        {
            var validatedBatchIds = new List<string>();
            foreach (var batchId in batchIds)
            {
                var (validated, validatedBatchId) = ValidateBatchId(batchId);
                if (validated)
                {
                    validatedBatchIds.Add(validatedBatchId);
                }
            }

            return validatedBatchIds;
        }
    }
}
