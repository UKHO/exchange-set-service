using Microsoft.AspNetCore.Http;
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
    public class SalesCatalogueServiceController : BaseController
    {
        private readonly SalesCatalogueService salesCatalogueService;
        public Dictionary<string, string> ErrorsIdentifiers { get; set; }
        public Dictionary<string, string> ErrorsVersions { get; set; }
        public Dictionary<string, string> ErrorsSinceDateTime { get; set; }

        public SalesCatalogueServiceController(IHttpContextAccessor httpContextAccessor, SalesCatalogueService salesCatalogueService) : base(httpContextAccessor)
        {
            this.salesCatalogueService = salesCatalogueService;
            ErrorsIdentifiers = new Dictionary<string, string>
            {
                { "source", "productIds" },
                { "description", "None of the product Ids exist in the database" }
            };
            ErrorsVersions = new Dictionary<string, string>
            {
                { "source", "productVersions" },
                { "description", "None of the product Ids exist in the database" }
            };
            ErrorsSinceDateTime = new Dictionary<string, string>
            {
                { "source", "productSinceDateTime" },
                { "description", "None of the product Ids exist in the database" }
            };
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
                    return Ok(response.ResponseBody);
                }
            }
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
                    return Ok(response.ResponseBody);
                }
            }
            return BadRequest(new { CorrelationId = GetCurrentCorrelationId(), Errors = ErrorsIdentifiers });
        }

        [HttpPost]
        [Route("v1/productData/encs57/product/productVersions")]
        public async Task<IActionResult> ProductVersions(List<ProductVersionRequest> productVersionRequest)
        {
            if (productVersionRequest != null && productVersionRequest.Any())
            {
                await Task.CompletedTask;
                var productVersionRequestSearchText = new StringBuilder();
                bool isInitalIndex = true;
                foreach (var item in productVersionRequest)
                {
                    productVersionRequestSearchText.Append((isInitalIndex ? "" : "-") + item.ProductName + "-" + item.EditionNumber + "-" + item.UpdateNumber);
                    isInitalIndex = false;
                }
                var response = salesCatalogueService.GetProductVersion("productVersion-" + productVersionRequestSearchText.ToString());
                if (response != null)
                {
                    return Ok(response.ResponseBody);
                }
            }
            return BadRequest(new { CorrelationId = GetCurrentCorrelationId(), Errors = ErrorsVersions });
        }
    }
}
