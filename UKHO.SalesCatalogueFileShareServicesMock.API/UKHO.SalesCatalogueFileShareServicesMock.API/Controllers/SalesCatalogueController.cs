using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Request;
using UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Controllers
{
    [ApiController]
    public class SalesCatalogueController : ControllerBase
    {
        public SalesCatalogueController()
        {

        }

        [HttpGet]
        [Route("v1/productData/encs57/product")]
        public async Task<IActionResult> ProductsSinceDateTime(string sinceDateTime)
        {
            if (!string.IsNullOrEmpty(sinceDateTime))
            {
                await Task.CompletedTask;
                return Ok(new SalesCatalogueResponse());
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
                return Ok(new SalesCatalogueResponse());
            }
            return BadRequest();
        }

        [HttpPost]
        [Route("v1/productData/encs57/product/productVersions")]
        public async Task<IActionResult> ProductVersions(List<ProductVersionRequest> productVersionRequest)
        {
            if (productVersionRequest != null && productVersionRequest.Any())
            {
                await Task.CompletedTask;
                return Ok(new SalesCatalogueResponse());
            }
            return BadRequest();
        }
    }
}
