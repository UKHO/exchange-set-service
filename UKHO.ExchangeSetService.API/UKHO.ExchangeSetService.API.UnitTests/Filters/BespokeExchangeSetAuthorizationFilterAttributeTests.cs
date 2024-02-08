using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
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
    [TestFixture]
    public class BespokeExchangeSetAuthorizationFilterAttributeTests
    {
        private IConfiguration fakeConfiguration;
        private BespokeExchangeSetAuthorizationFilterAttribute bespokeFilterAttribute;
        private ActionExecutingContext actionExecutingContext;
        private ActionExecutedContext actionExecutedContext;
        private const string TokenAudience = "aud";
        private const string IsUnencrypted = "IsUnencrypted";
        private const string ESSAzureADConfigurationClientId = "ESSAzureADConfiguration:ClientId";
        private const string ClientId = "80a6c68b-59aa-49a4-939a-7968ff79d676";

        readonly HttpContext httpContext = new DefaultHttpContext();

        [SetUp]
        public void Setup()
        {
            fakeConfiguration = A.Fake<IConfiguration>();
            bespokeFilterAttribute = new BespokeExchangeSetAuthorizationFilterAttribute(fakeConfiguration);

            var claims = new List<Claim>()
            {
                    new Claim(TokenAudience, ClientId),
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

            fakeConfiguration[ESSAzureADConfigurationClientId] = ClientId;

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

            fakeConfiguration[ESSAzureADConfigurationClientId] = ClientId;

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

            fakeConfiguration[ESSAzureADConfigurationClientId] = "80a6c68b-59ab-49a4-939a-7968ff79d678";

            var actionContext = new ActionContext(httpContext, new RouteData(), new());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);

            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }
    }
}
