using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.API.Filters
{
    /// <summary>
    /// Authorization to allow only UKHO people to create unencrypted exchange set.
    /// </summary>
    public class BespokeExchangeSetAuthorizationFilterAttribute : ActionFilterAttribute
    {
        private const string TokenAudience = "aud";
        private const string IsUnencrypted = "IsUnencrypted";
        private readonly IOptions<AzureADConfiguration> azureAdConfiguration;

        public BespokeExchangeSetAuthorizationFilterAttribute(IOptions<AzureADConfiguration> azureAdConfiguration)
        {
            this.azureAdConfiguration = azureAdConfiguration;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var tokenAudience = context.HttpContext.User.FindFirstValue(TokenAudience);
            var query = context.HttpContext.Request.Query[IsUnencrypted];
            var isUnencrypted = Convert.ToBoolean(!string.IsNullOrEmpty(query) ? Convert.ToString(query).ToLower() == "true" : "false");
            var azureADClientId = azureAdConfiguration.Value.ClientId;

            //If request is Bespoke exchange set and user is Non UKHO
            if (isUnencrypted)
            {
                if (azureADClientId != tokenAudience)
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }
            await next();
        }
    }
}
