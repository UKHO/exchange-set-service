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
using Microsoft.AspNetCore.Mvc.Abstractions;
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
        private const string ExchangeSetStandard = "exchangeSetStandard";
        private IOptions<AzureADConfiguration> fakeAzureAdConfig;
        private HttpContext httpContext;
        
        [SetUp]
        public void Setup()
        {
            fakeAzureAdConfig = A.Fake<IOptions<AzureADConfiguration>>();
            fakeAzureAdConfig.Value.ClientId = "80a6c68b-59aa-49a4-939a-7968ff79d676";
            httpContext = new DefaultHttpContext();

            bespokeFilterAttribute = new BespokeExchangeSetAuthorizationFilterAttribute(fakeAzureAdConfig);

            var claims = new List<Claim>()
            {
                    new Claim(TokenAudience, fakeAzureAdConfig.Value.ClientId)
            };
            var identity = httpContext.User.Identities.FirstOrDefault();
            identity.AddClaims(claims);
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIsNotSentAndAzureADClientIDIsEqualsWithTokenAudience_ThenReturnNextRequest()
        {
            var dictionary = new Dictionary<string, StringValues> { };

            httpContext.Request.Query = new QueryCollection(dictionary);

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);

            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            actionExecutingContext.ActionArguments[ExchangeSetStandard].Should().Be(Common.Models.Enums.ExchangeSetStandardForUnitTests.s63.ToString());
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIss57AndAzureADClientIDIsEqualsWithTokenAudience_ThenReturnNextRequest()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                {ExchangeSetStandard, Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString() }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);

            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            actionExecutingContext.ActionArguments[ExchangeSetStandard].Should().Be(Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString());
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIss63AndAzureADClientIDIsEqualsWithTokenAudience_ThenReturnNextRequest()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                { ExchangeSetStandard, Common.Models.Enums.ExchangeSetStandardForUnitTests.s63.ToString() }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);

            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            actionExecutingContext.ActionArguments[ExchangeSetStandard].Should().Be(Common.Models.Enums.ExchangeSetStandardForUnitTests.s63.ToString());
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIsS57AndAzureADClientIDIsEqualsWithTokenAudience_ThenReturnNextRequest()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
               { ExchangeSetStandard, Common.Models.Enums.ExchangeSetStandardForUnitTests.S57.ToString() }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);

            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            actionExecutingContext.ActionArguments[ExchangeSetStandard].Should().Be(Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString());
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIsGarbageValueAndAzureADClientIDIsEqualsWithTokenAudience_ThenReturnBadRequest()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
               { ExchangeSetStandard, Common.Models.Enums.ExchangeSetStandardForUnitTests.Test.ToString() }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);

            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }
        
        [Test]
        public async Task WhenExchangeSetStandardParameterIsEmptyAndAzureADClientIDIsEqualsWithTokenAudience_ThenReturnBadRequest()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
               { ExchangeSetStandard, string.Empty  }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);

            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterHasWhiteSpaceAndAzureADClientIDIsEqualsWithTokenAudience_ThenReturnBadRequest()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
               { ExchangeSetStandard, " s63 "}
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);

            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIss57AndAzureADClientIDIsNotEqualsWithTokenAudience_ThenReturnUnauthorized()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                { ExchangeSetStandard, Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString() }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            fakeAzureAdConfig.Value.ClientId = "80a6c68b-59ab-49a4-939a-7968ff79d678";

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);

            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
            actionExecutingContext.ActionArguments[ExchangeSetStandard].Should().Be(Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString());
        }
    }
}
