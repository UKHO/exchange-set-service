using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.API.Filters
{
    /// <summary>
    /// 140109 : ESS API :- Add authorization to allow only UKHO people to create unencrypted exchange set (Bespoke Exchange Set)
    /// </summary>
    public class BespokeFilterAttribute : ActionFilterAttribute
    {
        private readonly IConfiguration configuration;
        private const string TokenAudience = "aud";
        private const string IsUnencrypted = "IsUnencrypted";
        private const string ESSAzureADConfigurationClientId = "ESSAzureADConfiguration:ClientId";
        public BespokeFilterAttribute(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            string tokenAudience = context.HttpContext.User.FindFirstValue(TokenAudience);
            bool isUnencrypted = Convert.ToBoolean(context.HttpContext.Request.Query[IsUnencrypted]);
            string azureADClientID = configuration[ESSAzureADConfigurationClientId];

            //If request is Bespoke exchange set and user is Non UKHO
            if (isUnencrypted)
            {
                if (azureADClientID != tokenAudience)
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }
            await next();
        }
    }
}
