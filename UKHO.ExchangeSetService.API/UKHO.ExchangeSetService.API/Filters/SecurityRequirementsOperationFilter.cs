using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace UKHO.ExchangeSetService.API.Filters
{
    [ExcludeFromCodeCoverage]
    public class SecurityRequirementsOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var requiredScopes = context.MethodInfo
                                .DeclaringType
                                .GetCustomAttributes(true)
                                .OfType<AuthorizeAttribute>()
                                .Select(attr => attr.Policy)
                                .Distinct();

            if (requiredScopes.Any())
            {

                var oAuthScheme = new OpenApiSecuritySchemeReference("jwtBearerAuth");

                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        [oAuthScheme] = requiredScopes.ToList()
                    }
                };
            }
        }
    }
}
