using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.API.Filters
{
    /// <summary>
    /// 140109 : ESS API :- Add authorization to allow only UKHO people to create unencrypted exchange set (Bespoke Exchange Set)
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class BespokeFilterAttribute : ActionFilterAttribute
    {
        private readonly IConfiguration configuration;
        public BespokeFilterAttribute(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            string tokenAudience = context.HttpContext.User.FindFirstValue("aud");
            string isUnencrypted = context.HttpContext.Request.Query["IsUnencrypted"];
            string azureADClientID = configuration["ESSAzureADConfiguration:ClientId"];

            //If request is Bespoke exchange set and user is Non UKHO
            if (isUnencrypted == "true" && azureADClientID != tokenAudience)
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            await next();
        }
    }
}
