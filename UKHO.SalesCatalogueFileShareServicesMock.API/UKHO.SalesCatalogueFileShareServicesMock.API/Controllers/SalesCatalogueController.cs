using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Request;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response;
using UKHO.SalesCatalogueFileShareServicesMock.API.Services;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Controllers
{
    [ApiController]
    public class SalesCatalogueController : ControllerBase
    {
        private readonly SalesCatalogueService salesCatalogueService;
        public Dictionary<string, string> ErrorsIdentifiers { get; set; }
        public Dictionary<string, string> ErrorsVersions { get; set; }
        public Dictionary<string, string> ErrorsSinceDateTime { get; set; }

        public SalesCatalogueController(SalesCatalogueService salesCatalogueService)
        {
            this.salesCatalogueService = salesCatalogueService;
            ErrorsIdentifiers = new Dictionary<string, string>();
            ErrorsIdentifiers.Add("source", "productIds");
            ErrorsIdentifiers.Add("description", "None of the product Ids exist in the database");
            ErrorsVersions = new Dictionary<string, string>();
            ErrorsVersions.Add("source", "productVersions");
            ErrorsVersions.Add("description", "None of the product Ids exist in the database");
            ErrorsVersions = new Dictionary<string, string>();
            ErrorsVersions.Add("source", "productSinceDateTime");
            ErrorsVersions.Add("description", "None of the product Ids exist in the database");
        }

        [HttpGet]
        [Route("v1/productData/encs57/product")]
        public async Task<IActionResult> ProductsSinceDateTime(string sinceDateTime)
        {
            if (!string.IsNullOrEmpty(sinceDateTime))
            {
                await Task.CompletedTask;
                var response = salesCatalogueService.GetProductSinceDateTime(sinceDateTime);
                if (response != null)
                {
                    response.ResponseCode = System.Net.HttpStatusCode.OK;
                    response.LastModified = DateTime.Now.AddDays(-2);
                    return Ok(response);
                }
            }
            ////return BadRequest(new { CorrelationId = Guid.NewGuid(), Errors = ErrorsSinceDateTime });
            return BadRequest();
        }

        [HttpPost]
        [Route("v1/productData/encs57/product/productIdentifiers")]
        public async Task<IActionResult> ProductIdentifiers(List<string> productIdentifiers)
        {
            if (productIdentifiers != null && productIdentifiers.Any())
            {
                await Task.CompletedTask;
                var response = salesCatalogueService.GetProductIdentifier("productIdentifier-" + String.Join("-", productIdentifiers));
                if (response != null)
                {
                    response.ResponseCode = System.Net.HttpStatusCode.OK;
                    response.LastModified = DateTime.Now.AddDays(-2);
                    return Ok(response);
                }
            }
            return BadRequest(new { CorrelationId = Guid.NewGuid(), Errors = ErrorsIdentifiers });
        }

        [HttpPost]
        [Route("v1/productData/encs57/product/productVersions")]
        public async Task<IActionResult> ProductVersions(List<ProductVersionRequest> productVersionRequest)
        {
            if (productVersionRequest != null && productVersionRequest.Any())
            {
                await Task.CompletedTask;
                var sb = new StringBuilder();
                bool isInitalIndex = true;
                foreach (var item in productVersionRequest)
                {
                    sb.Append((isInitalIndex ? "" : "-") + item.ProductName + "-" + item.EditionNumber + "-" + item.UpdateNumber);
                    isInitalIndex = false;
                }
                var response = salesCatalogueService.GetProductVersion("productVersion-" + sb.ToString());
                if (response != null)
                {
                    response.ResponseCode = System.Net.HttpStatusCode.OK;
                    response.LastModified = DateTime.Now.AddDays(-2);
                    return Ok(response);
                }
            }
            return BadRequest(new { CorrelationId = Guid.NewGuid(), Errors = ErrorsVersions });
        }
    }
}
