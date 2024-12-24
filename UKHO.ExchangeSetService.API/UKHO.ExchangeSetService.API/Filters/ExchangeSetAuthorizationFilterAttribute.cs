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
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;
using UKHO.ExchangeSetService.Common.Models.Enums;

namespace UKHO.ExchangeSetService.API.Filters
{
    public class ExchangeSetAuthorizationFilterAttribute : ActionFilterAttribute
    {
        private const string TokenAudience = "aud";
        private const string ExchangeSetStandard = "exchangeSetStandard";
        private const string TokenIssuer = "iss";
        private const string TokenTenantId = "http://schemas.microsoft.com/identity/claims/tenantid";
        private const string standard = "s100";
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

            if (!Guid.TryParse(correlationId, out Guid correlationIdGuid))
            {
                correlationId = Guid.Empty.ToString();
                logger.LogError(EventIds.BadRequest.ToEventId(), null, "_X-Correlation-ID is invalid :{correlationId}", correlationId);
            }

            if (!context.HttpContext.Request.RouteValues.TryGetValue(ExchangeSetStandard, out var queryStringValue))
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var exchangeSetStandard = Convert.ToString(queryStringValue);
            if (!ValidateExchangeSetStandard(exchangeSetStandard, out ExchangeSetStandard parsedEnum))
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            context.ActionArguments[ExchangeSetStandard] = parsedEnum.ToString();

            if (parsedEnum.ToString() != standard)
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            // If request is s100 exchange set and user is Non UKHO
            if (string.Equals(exchangeSetStandard, Common.Models.Enums.ExchangeSetStandard.s100.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                if (azureAdTenantId == tokenTenantId && azureAdClientId != tokenAudience)
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                AzureAdB2C azureAdB2C = new AzureAdB2C
                {
                    AudToken = tokenAudience,
                    IssToken = tokenIssuer,
                };
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
