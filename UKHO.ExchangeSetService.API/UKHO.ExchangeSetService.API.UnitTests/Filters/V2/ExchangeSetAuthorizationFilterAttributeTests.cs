using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Filters.V2;
using UKHO.ExchangeSetService.Common.Models.V2.Enums;

namespace UKHO.ExchangeSetService.API.UnitTests.Filters.V2
{
    [TestFixture]
    public class ExchangeSetAuthorizationFilterAttributeTests
    {
        private ExchangeSetAuthorizationFilterAttribute exchangeSetFilterAttribute;
        private ActionExecutingContext actionExecutingContext;
        private ActionExecutedContext actionExecutedContext;
        private const string Standard = "exchangeSetStandard";
        private HttpContext httpContext;

        [SetUp]
        public void Setup()
        {
            httpContext = new DefaultHttpContext();
            exchangeSetFilterAttribute = new ExchangeSetAuthorizationFilterAttribute();
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIsNotSent_ThenReturnBadRequest()
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
        public async Task WhenExchangeSetStandardParameterIsS100_ThenReturnNextRequest()
        {
            httpContext.Request.RouteValues.Add(Standard, ExchangeSetStandard.s100.ToString());
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), exchangeSetFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), exchangeSetFilterAttribute);

            await exchangeSetFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            actionExecutingContext.ActionArguments[Standard].Should().Be(ExchangeSetStandard.s100.ToString());
        }

        [Test]
        [TestCase("s57")]
        [TestCase("s63")]
        [TestCase(" s100 ")]
        [TestCase("")]
        [TestCase("s 100")]
        public async Task WhenExchangeSetStandardParameterIsInvalid_ThenReturnBadRequest(string exchangeSetStandard)
        {
            httpContext.Request.RouteValues.Add(Standard, exchangeSetStandard);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), exchangeSetFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), exchangeSetFilterAttribute);

            await exchangeSetFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

    }
}
