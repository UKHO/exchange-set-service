using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;
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
        private const string TokenIssuer = "iss";
        private const string TokenTenantId = "http://schemas.microsoft.com/identity/claims/tenantid";
        private readonly IOptions<AzureADConfiguration> azureAdConfiguration;
        private static readonly string[] ExchangeSetStandards = Enum.GetNames(typeof(ExchangeSetStandard));
        private readonly IConfiguration configuration;
        private readonly IAzureAdB2CHelper azureAdB2CHelper;
        public BespokeExchangeSetAuthorizationFilterAttribute(IOptions<AzureADConfiguration> azureAdConfiguration, IConfiguration configuration, IAzureAdB2CHelper azureAdB2CHelper)
        {
            this.azureAdConfiguration = azureAdConfiguration ?? throw new ArgumentNullException(nameof(azureAdConfiguration));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.azureAdB2CHelper = azureAdB2CHelper ?? throw new ArgumentNullException(nameof(azureAdB2CHelper));
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var azureAdClientId = azureAdConfiguration.Value.ClientId;
            var azureAdTenantId = azureAdConfiguration.Value.TenantId;

            var tokenAudience = context.HttpContext.User.FindFirstValue(TokenAudience);
            var tokenTenantId = context.HttpContext.User.FindFirstValue(TokenTenantId);
            var tokenIssuer = context.HttpContext.User.FindFirstValue(TokenIssuer);
            var correlationId = context.HttpContext.Request.Headers[CorrelationIdMiddleware.XCorrelationIdHeaderKey].FirstOrDefault();

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
                if (azureAdTenantId == tokenTenantId)
                {
                    if (azureAdClientId != tokenAudience)
                    {
                        context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return;
                    }
                }

                AzureAdB2C azureAdB2C = new AzureAdB2C
                {
                    AudToken = tokenAudience,
                    IssToken = tokenIssuer,
                };

                if (azureAdB2CHelper.IsAzureB2CUser(azureAdB2C, correlationId))
                {
                    var adminDomains = !string.IsNullOrEmpty(this.configuration["AdminDomains"]) ? new(this.configuration["AdminDomains"].Split(',').Select(s => s.Trim())) : new List<string>();
                    var userEmail = context.HttpContext.User.FindFirstValue(ClaimTypes.Email);

                    if (userEmail == null || !adminDomains.Any(x => userEmail.EndsWith(x, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return;
                    }
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