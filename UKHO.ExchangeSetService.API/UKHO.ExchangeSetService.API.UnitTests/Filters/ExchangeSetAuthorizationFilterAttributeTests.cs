using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Filters;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.Enums;

namespace UKHO.ExchangeSetService.API.UnitTests.Filters
{
    [TestFixture]
    public class ExchangeSetAuthorizationFilterAttributeTests
    {
        private ExchangeSetAuthorizationFilterAttribute exchangeSetFilterAttribute;
        private ActionExecutingContext actionExecutingContext;
        private ActionExecutedContext actionExecutedContext;
        private const string TokenAudience = "aud";
        private const string ExchangeSetStandard = "exchangeSetStandard";
        private IOptions<AzureADConfiguration> fakeAzureAdConfig;
        private IConfiguration fakeConfiguration;
        private HttpContext httpContext;
        private IAzureAdB2CHelper fakeAzureAdB2CHelper;
        private ILogger<ExchangeSetAuthorizationFilterAttribute> fakeLogger;
        [SetUp]
        public void Setup()
        {
            fakeAzureAdConfig = A.Fake<IOptions<AzureADConfiguration>>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakeAzureAdB2CHelper = A.Fake<IAzureAdB2CHelper>();
            fakeLogger = A.Fake<ILogger<ExchangeSetAuthorizationFilterAttribute>>();
            fakeAzureAdConfig.Value.ClientId = "80a6c68b-59aa-49a4-939a-7968ff79d676";
            fakeAzureAdConfig.Value.TenantId = "azure-ad-tenant";
            fakeAzureAdConfig.Value.MicrosoftOnlineLoginUrl = "https://login.microsoftonline.com/";
            httpContext = new DefaultHttpContext();

            exchangeSetFilterAttribute = new ExchangeSetAuthorizationFilterAttribute(fakeAzureAdConfig, fakeConfiguration, fakeAzureAdB2CHelper, fakeLogger);

            var claims = new List<Claim>()
            {
                    new Claim(TokenAudience, fakeAzureAdConfig.Value.ClientId)
            };
            var identity = httpContext.User.Identities.FirstOrDefault();
            identity.AddClaims(claims);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullAzureAdConfigExchangeSetFilterAttribute = () => new ExchangeSetAuthorizationFilterAttribute(null, fakeConfiguration, fakeAzureAdB2CHelper, fakeLogger);
            nullAzureAdConfigExchangeSetFilterAttribute.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureAdConfiguration");

            Action nullConfigurationExchangeSetFilterAttribute = () => new ExchangeSetAuthorizationFilterAttribute(fakeAzureAdConfig, null, fakeAzureAdB2CHelper, fakeLogger);
            nullConfigurationExchangeSetFilterAttribute.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("configuration");

            Action nullAzureAdB2CConfigurationExchangeSetFilterAttribute = () => new ExchangeSetAuthorizationFilterAttribute(fakeAzureAdConfig, fakeConfiguration, null, fakeLogger);
            nullAzureAdB2CConfigurationExchangeSetFilterAttribute.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureAdB2CHelper");

            Action nullLoggerExchangeSetFilterAttribute = () => new ExchangeSetAuthorizationFilterAttribute(fakeAzureAdConfig, fakeConfiguration, fakeAzureAdB2CHelper, null);
            nullLoggerExchangeSetFilterAttribute.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("ExchangeSetAuthorizationFilterAttribute");
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIsNotSentAndAzureADClientIDIsEqualsWithTokenAudience_ThenReturnBadRequest()
        {
            var dictionary = new Dictionary<string, StringValues> { };
            httpContext.Request.RouteValues = new RouteValueDictionary(dictionary);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), exchangeSetFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), exchangeSetFilterAttribute);

            await exchangeSetFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIss100AndAzureADClientIDIsEqualsWithTokenAudience_ThenReturnNextRequest()
        {
            httpContext.Request.RouteValues.Add(ExchangeSetStandard, ExchangeSetStandardForUnitTests.s100.ToString());
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), exchangeSetFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), exchangeSetFilterAttribute);

            await exchangeSetFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            actionExecutingContext.ActionArguments[ExchangeSetStandard].Should().Be(ExchangeSetStandardForUnitTests.s100.ToString());
        }

        [Test]
        [TestCase("s57")]
        [TestCase("s63")]
        public async Task WhenExchangeSetStandardParameterIss57Ors63AndAzureADClientIDIsEqualsWithTokenAudience_ThenReturnBadRequest(string exchangeSetStandard)
        {
            httpContext.Request.RouteValues.Add(ExchangeSetStandard, exchangeSetStandard);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), exchangeSetFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), exchangeSetFilterAttribute);

            await exchangeSetFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            actionExecutingContext.ActionArguments[ExchangeSetStandard].Should().NotBe(ExchangeSetStandardForUnitTests.s100.ToString());
        }

        [Test]
        [TestCase(" s100 ")]
        [TestCase("")]
        [TestCase("s 100")]
        public async Task WhenExchangeSetStandardParameterIsInvalidAndAzureADClientIDIsEqualsWithTokenAudience_ThenReturnBadRequest(string exchangeSetStandard)
        {
            httpContext.Request.RouteValues.Add(ExchangeSetStandard, exchangeSetStandard);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), exchangeSetFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), exchangeSetFilterAttribute);

            await exchangeSetFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIsGarbageValueAndAzureADClientIDIsEqualsWithTokenAudience_ThenReturnBadRequest()
        {
            httpContext.Request.RouteValues.Add(ExchangeSetStandard, ExchangeSetStandardForUnitTests.Test.ToString());
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), exchangeSetFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), exchangeSetFilterAttribute);

            await exchangeSetFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIss100AndUserIsNonUKHO_ReturnsStatus403Forbidden()
        {
            httpContext = new DefaultHttpContext();
            httpContext.Request.RouteValues.Add(ExchangeSetStandard, ExchangeSetStandardForUnitTests.s100.ToString());
            var claims = new List<Claim>()
            {
                new Claim("aud",  "some-audiance"),
                new Claim("iss", "some-issuer"),
                new Claim("http://schemas.microsoft.com/identity/claims/tenantid", fakeAzureAdConfig.Value.TenantId)
            };

            var identity = httpContext.User.Identities.FirstOrDefault();
            identity.AddClaims(claims);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), exchangeSetFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), exchangeSetFilterAttribute);

            await exchangeSetFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            actionExecutingContext.ActionArguments[ExchangeSetStandard].Should().Be(ExchangeSetStandardForUnitTests.s100.ToString());
        }
    }
}
