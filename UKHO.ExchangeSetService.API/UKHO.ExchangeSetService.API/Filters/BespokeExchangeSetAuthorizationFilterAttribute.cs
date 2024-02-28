using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Models.Enums;

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
        private static readonly string[] ExchangeSetStandards = Enum.GetNames(typeof(ExchangeSetStandard));

        public BespokeExchangeSetAuthorizationFilterAttribute(IOptions<AzureADConfiguration> azureAdConfiguration)
        {
            this.azureAdConfiguration = azureAdConfiguration ?? throw new ArgumentNullException(nameof(azureAdConfiguration));
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var tokenAudience = context.HttpContext.User.FindFirstValue(TokenAudience);
            var azureAdClientId = azureAdConfiguration.Value.ClientId;

            context.HttpContext.Request.Query.TryGetValue(ExchangeSetStandard, out var queryStringValue);
            var exchangeSetStandard = context.HttpContext.Request.Query.ContainsKey(ExchangeSetStandard)
                ? Convert.ToString(queryStringValue)
                : Common.Models.Enums.ExchangeSetStandard.s63.ToString();

            if (!ValidateExchangeSetStandard(exchangeSetStandard, out ExchangeSetStandard parsedEnum))
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            context.ActionArguments[ExchangeSetStandard] = parsedEnum.ToString();

            //If request is Bespoke exchange set and user is Non UKHO
            if (string.Equals(exchangeSetStandard, Common.Models.Enums.ExchangeSetStandard.s57.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                if (azureAdClientId != tokenAudience)
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }
            await next();
        }

        private static bool ValidateExchangeSetStandard<TEnum>(string exchangeSetStandard, out TEnum result) where TEnum : struct, Enum
        {
            result = default;
            if (string.IsNullOrEmpty(exchangeSetStandard))
            {
                return false;
            }
            if (exchangeSetStandard.Any(x => Char.IsWhiteSpace(x)))
            {
                return false;
            }
            if (!ExchangeSetStandardExists(exchangeSetStandard))
            {
                return false;
            }

            return Enum.TryParse(exchangeSetStandard, true, out result);
        }

        private static bool ExchangeSetStandardExists(string exchangeSetStandard)
        {
            return ExchangeSetStandards.Any(s => exchangeSetStandard.Contains(s, StringComparison.OrdinalIgnoreCase));
        }
    }
}