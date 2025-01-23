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
        private const string ProductTypeKey = "productType";

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!TryGetExchangeSetStandard(context, out var productType) ||
                !TryParseExchangeSetStandard(productType, out ProductType parsedEnum) ||
                !IsValidExchangeSetStandard(parsedEnum))
            {
                SetBadRequestResponse(context);
                return;
            }
            context.ActionArguments[ProductTypeKey] = parsedEnum.ToString();
            await next();
        }

        private static bool TryGetExchangeSetStandard(ActionExecutingContext context, out string productType)
        {
            productType = null;
            if (context.HttpContext.Request.RouteValues.TryGetValue(ProductTypeKey, out var queryStringValue))
            {
                productType = Convert.ToString(queryStringValue);
                return true;
            }
            return false;
        }

        private static bool TryParseExchangeSetStandard<TEnum>(string productType, out TEnum result) where TEnum : struct, Enum
        {
            result = default;
            if (string.IsNullOrEmpty(productType) || productType.Any(char.IsWhiteSpace))
            {
                return false;
            }
            return Enum.TryParse(productType, true, out result);
        }

        private static bool IsValidExchangeSetStandard(ProductType productType)
        {
            return productType == ProductType.s100;
        }

        private static void SetBadRequestResponse(ActionExecutingContext context)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}
