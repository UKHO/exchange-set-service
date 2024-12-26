using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;

namespace UKHO.ExchangeSetService.API.Filters
{
    public class ExchangeSetAuthorizationFilterAttribute : ActionFilterAttribute
    {
        private const string TokenAudience = "aud";
        private const string ExchangeSetStandard = "exchangeSetStandard";
        private const string TokenIssuer = "iss";
        private const string TokenTenantId = "http://schemas.microsoft.com/identity/claims/tenantid";
        private readonly IOptions<AzureADConfiguration> azureAdConfiguration;
        private readonly IConfiguration configuration;
        private readonly IAzureAdB2CHelper azureAdB2CHelper;
        private readonly ILogger<ExchangeSetAuthorizationFilterAttribute> logger;

        public ExchangeSetAuthorizationFilterAttribute(IOptions<AzureADConfiguration> azureAdConfiguration, IConfiguration configuration, IAzureAdB2CHelper azureAdB2CHelper, ILogger<ExchangeSetAuthorizationFilterAttribute> logger)
        {
            this.azureAdConfiguration = azureAdConfiguration ?? throw new ArgumentNullException(nameof(azureAdConfiguration));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.azureAdB2CHelper = azureAdB2CHelper ?? throw new ArgumentNullException(nameof(azureAdB2CHelper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(ExchangeSetAuthorizationFilterAttribute));
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var azureAdClientId = azureAdConfiguration.Value.ClientId;
            var azureAdTenantId = azureAdConfiguration.Value.TenantId;

            var tokenAudience = context.HttpContext.User.FindFirstValue(TokenAudience);
            var tokenTenantId = context.HttpContext.User.FindFirstValue(TokenTenantId);
            var tokenIssuer = context.HttpContext.User.FindFirstValue(TokenIssuer);
            var correlationId = context.HttpContext.Request.Headers[CorrelationIdMiddleware.XCorrelationIdHeaderKey].FirstOrDefault();

            if (!context.HttpContext.Request.RouteValues.TryGetValue(ExchangeSetStandard, out var queryStringValue))
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            var exchangeSetStandard = Convert.ToString(queryStringValue);
            if (!ValidateExchangeSetStandard(exchangeSetStandard, out Common.Models.V2.Enums.ExchangeSetStandard parsedEnum))
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            context.ActionArguments[ExchangeSetStandard] = parsedEnum.ToString();

            if (parsedEnum.ToString() != Common.Models.V2.Enums.ExchangeSetStandard.s100.ToString())
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
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

            return Enum.TryParse(exchangeSetStandard, true, out result);
        }
    }
}
