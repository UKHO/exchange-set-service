using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Filters;
using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.API.UnitTests.Filters
{
    [TestFixture]
    public class BespokeExchangeSetAuthorizationFilterAttributeTests
    {
        private BespokeExchangeSetAuthorizationFilterAttribute bespokeFilterAttribute;
        private ActionExecutingContext actionExecutingContext;
        private ActionExecutedContext actionExecutedContext;
        private const string TokenAudience = "aud";
        private const string IsUnencrypted = "IsUnencrypted";
        private IOptions<AzureADConfiguration> fakeAzureAdConfig;

        readonly HttpContext httpContext = new DefaultHttpContext();

        [SetUp]
        public void Setup()
        {
            fakeAzureAdConfig = A.Fake<IOptions<AzureADConfiguration>>();
            fakeAzureAdConfig.Value.ClientId = "80a6c68b-59aa-49a4-939a-7968ff79d676";

            bespokeFilterAttribute = new BespokeExchangeSetAuthorizationFilterAttribute(fakeAzureAdConfig);

            var claims = new List<Claim>()
            {
                    new Claim(TokenAudience, fakeAzureAdConfig.Value.ClientId)
            };
            var identity = httpContext.User.Identities.FirstOrDefault();
            identity.AddClaims(claims);
        }

        [Test]
        public async Task WhenIsUnencryptedParameterIsFalseAndAzureADClientIDIsEqualsWithTokenAudience_ThenReturnNextRequest()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                { IsUnencrypted, "false" }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            var actionContext = new ActionContext(httpContext, new RouteData(), new());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);

            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Test]
        public async Task WhenIsUnencryptedParameterIsTrueAndAzureADClientIDIsEqualsWithTokenAudience_ThenReturnNextRequest()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                { IsUnencrypted, "true" }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            var actionContext = new ActionContext(httpContext, new RouteData(), new());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);

            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Test]
        public async Task WhenIsUnencryptedParameterIsTrueAndAzureADClientIDIsNotEqualsWithTokenAudience_ThenReturnUnauthorized()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                { IsUnencrypted, "true" }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            fakeAzureAdConfig.Value.ClientId = "80a6c68b-59ab-49a4-939a-7968ff79d678";

            var actionContext = new ActionContext(httpContext, new RouteData(), new());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);

            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }

        [Test]
        public async Task WhenIsUnencryptedParameterIsGarbageValueAndAzureADClientIDIsEqualsWithTokenAudience_ThenCodeExecuted()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                { IsUnencrypted, "test" }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            var actionContext = new ActionContext(httpContext, new RouteData(), new());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);

            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }
    }
}
