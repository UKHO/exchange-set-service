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

        public BespokeExchangeSetAuthorizationFilterAttribute(IOptions<AzureADConfiguration> azureAdConfiguration)
        {
            this.azureAdConfiguration = azureAdConfiguration;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var tokenAudience = context.HttpContext.User.FindFirstValue(TokenAudience);
            var azureAdClientId = azureAdConfiguration.Value.ClientId;

            context.HttpContext.Request.Query.TryGetValue(ExchangeSetStandard, out var queryStringValue);
            var exchangeSetStandard = context.HttpContext.Request.Query.ContainsKey(ExchangeSetStandard)
                ? Convert.ToString(queryStringValue)
                : Common.Models.Enums.ExchangeSetStandard.s63.ToString();

            if (string.IsNullOrEmpty(exchangeSetStandard) || !EnumTryParseStrict(exchangeSetStandard, out ExchangeSetStandard parsedEnum, true))
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

        public static bool EnumTryParseStrict<TEnum>(string value, out TEnum result, bool ignoreCase = false) where TEnum : struct, Enum
        {
            if (value.Any(x => Char.IsWhiteSpace(x)))
            {
                result = default;
                return false;
            }

            if (value == "0" || value == "1")
            {
                result = default;
                return false;
            }

            return Enum.TryParse(value, ignoreCase, out result) && Enum.IsDefined(typeof(TEnum), result);
        }
    }
}