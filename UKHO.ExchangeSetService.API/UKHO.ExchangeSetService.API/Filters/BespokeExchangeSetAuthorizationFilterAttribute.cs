using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.API.Filters
{
    /// <summary>
    /// Authorization to allow only UKHO people to create unencrypted exchange set.
    /// </summary>
    public class BespokeExchangeSetAuthorizationFilterAttribute : ActionFilterAttribute
    {
        private const string TokenAudience = "aud";
        private const string ExchangeSetStandard = "exchangeSetStandard";
        private readonly IOptions<AzureADConfiguration> azureAdConfiguration;

        public BespokeExchangeSetAuthorizationFilterAttribute(IOptions<AzureADConfiguration> azureAdConfiguration)
        {
            this.azureAdConfiguration = azureAdConfiguration;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var tokenAudience = context.HttpContext.User.FindFirstValue(TokenAudience);
            var azureADClientId = azureAdConfiguration.Value.ClientId;
            string exchangeSetStandard = string.Empty;

            context.HttpContext.Request.Query.TryGetValue(ExchangeSetStandard, out var queryStringValue);
            if (context.HttpContext.Request.Query.ContainsKey(ExchangeSetStandard))
            {
                exchangeSetStandard = Convert.ToString(queryStringValue).Trim('"');
            }

            if (string.IsNullOrEmpty(exchangeSetStandard))
            {
                exchangeSetStandard = Common.ExchangeSetStandard.s63.ToString();
                context.ActionArguments[ExchangeSetStandard] = exchangeSetStandard;
            }
            else
            {
                Common.ExchangeSetStandard parsedEnum;
                if (!Enum.TryParse(exchangeSetStandard, true, out parsedEnum))
                {
                    exchangeSetStandard = Common.ExchangeSetStandard.s63.ToString();
                    context.ActionArguments[ExchangeSetStandard] = exchangeSetStandard;
                }
                else
                {
                    context.ActionArguments[ExchangeSetStandard] = parsedEnum.ToString();
                }
            }

            //If request is Bespoke exchange set and user is Non UKHO
            if (string.Equals(exchangeSetStandard, Common.ExchangeSetStandard.s57.ToString(), StringComparison.OrdinalIgnoreCase))
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