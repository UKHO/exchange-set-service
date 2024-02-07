using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Filters;

namespace UKHO.ExchangeSetService.API.UnitTests.Filters
{
    public class BespokeFilterAttributeTests
    {
        private IConfiguration fakeConfiguration;
        private BespokeFilterAttribute bespokeFilterAttribute;
        private ActionExecutingContext actionExecutingContext;
        private ActionExecutedContext actionExecutedContext;
        public const string TokenAudience = "aud";
        public const string IsUnencrypted = "IsUnencrypted";
        public const string ESSAzureADConfigurationClientId = "ESSAzureADConfiguration:ClientId";
        readonly HttpContext httpContext = new DefaultHttpContext();

        [SetUp]
        public void Setup()
        {
            fakeConfiguration = A.Fake<IConfiguration>();
            bespokeFilterAttribute = new BespokeFilterAttribute(fakeConfiguration);

            var claims = new List<Claim>()
            {
                    new Claim(TokenAudience,"80a6c68b-59aa-49a4-939a-7968ff79d676"),
            };
            var identity = httpContext.User.Identities.FirstOrDefault();
            identity.AddClaims(claims);
        }

        [Test]
        public void WhenIsUnencyptedParameterIsFalseAndAzureADClientIDIsNotEqualsWithTokenAudience_ThenReturnNextRequest()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                { IsUnencrypted, "false" }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            fakeConfiguration[ESSAzureADConfigurationClientId] = "80a6c68b-59aa-49a4-939a-7968ff79d676";

            var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null);
            var result = bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));
            result.IsCompletedSuccessfully.Should().BeTrue();
        }

        [Test]
        public void WhenIsUnencyptedParameterIsTrueAndAzureADClientIDIsEqualsWithTokenAudience_ThenReturnNextRequest()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                { IsUnencrypted, "true" }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);
            fakeConfiguration[ESSAzureADConfigurationClientId] = "80a6c68b-59aa-49a4-939a-7968ff79d676";

            var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null);
            var result = bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));
            result.IsCompletedSuccessfully.Should().BeTrue();
        }

        [Test]
        public async Task WhenIsUnencyptedParameterIsTrueAndAzureADClientIDIsNotEqualsWithTokenAudience_ThenReturnUnauthorized()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                { IsUnencrypted, "true" }
            };

            httpContext.Request.Query = new QueryCollection(dictionary);
            fakeConfiguration[ESSAzureADConfigurationClientId] = "80a6c68b-59ab-49a4-939a-7968ff79d678";

            var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null);
            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));
            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }
    }
}
