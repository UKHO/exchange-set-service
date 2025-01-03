using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using UKHO.ExchangeSetService.Common.Models.V2.Enums;

namespace UKHO.ExchangeSetService.API.Filters.V2
{
    public class ExchangeSetAuthorizationFilterAttribute : ActionFilterAttribute
    {
        private const string ExchangeSetStandardKey = "exchangeSetStandard";

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!TryGetExchangeSetStandard(context, out var exchangeSetStandard) ||
                !TryParseExchangeSetStandard(exchangeSetStandard, out ExchangeSetStandard parsedEnum) ||
                !IsValidExchangeSetStandard(parsedEnum))
            {
                SetBadRequestResponse(context);
                return;
            }
            context.ActionArguments[ExchangeSetStandardKey] = parsedEnum.ToString();
            await next();
        }

        private static bool TryGetExchangeSetStandard(ActionExecutingContext context, out string exchangeSetStandard)
        {
            exchangeSetStandard = null;
            if (context.HttpContext.Request.RouteValues.TryGetValue(ExchangeSetStandardKey, out var queryStringValue))
            {
                exchangeSetStandard = Convert.ToString(queryStringValue);
                return true;
            }
            return false;
        }

        private static bool TryParseExchangeSetStandard<TEnum>(string exchangeSetStandard, out TEnum result) where TEnum : struct, Enum
        {
            result = default;
            if (string.IsNullOrEmpty(exchangeSetStandard) || exchangeSetStandard.Any(char.IsWhiteSpace))
            {
                return false;
            }
            return Enum.TryParse(exchangeSetStandard, true, out result);
        }

        private static bool IsValidExchangeSetStandard(ExchangeSetStandard exchangeSetStandard)
        {
            return exchangeSetStandard == ExchangeSetStandard.s100;
        }

        private static void SetBadRequestResponse(ActionExecutingContext context)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}
