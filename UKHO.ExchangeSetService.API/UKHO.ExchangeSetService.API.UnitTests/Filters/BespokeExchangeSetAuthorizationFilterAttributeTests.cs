﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FakeItEasy;
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
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;

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
        private const string TokenIssuer = "iss";
        private const string TokenTenantId = "http://schemas.microsoft.com/identity/claims/tenantid";
        public const string XCorrelationIdHeaderKey = "X-Correlation-ID";
        private IOptions<AzureADConfiguration> fakeAzureAdConfig;
        private IOptions<AzureAdB2CConfiguration> fakeAzureAdB2CConfiguration;
        private IConfiguration fakeConfiguration;
        private HttpContext httpContext;
        private IAzureAdB2CHelper fakeAzureAdB2CHelper;
        private ILogger<BespokeExchangeSetAuthorizationFilterAttribute> fakelogger;
        [SetUp]
        public void Setup()
        {
            fakeAzureAdConfig = A.Fake<IOptions<AzureADConfiguration>>();
            fakeAzureAdB2CConfiguration = A.Fake<IOptions<AzureAdB2CConfiguration>>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakeAzureAdB2CHelper = A.Fake<IAzureAdB2CHelper>();
            fakelogger = A.Fake<ILogger<BespokeExchangeSetAuthorizationFilterAttribute>>();
            fakeAzureAdConfig.Value.ClientId = "80a6c68b-59aa-49a4-939a-7968ff79d676";
            fakeAzureAdB2CConfiguration.Value.ClientId = "azure-adb2c-client";
            fakeAzureAdConfig.Value.TenantId = "azure-ad-tenant";
            fakeAzureAdB2CConfiguration.Value.Instance = "https://azureAdB2CInstance/";
            fakeAzureAdB2CConfiguration.Value.TenantId = "azure-ad2c-tenant";
            fakeAzureAdConfig.Value.MicrosoftOnlineLoginUrl = "https://login.microsoftonline.com/";
            fakeConfiguration["PrivilegedUserDomains"] = "abc.com";
            httpContext = new DefaultHttpContext();

            bespokeFilterAttribute = new BespokeExchangeSetAuthorizationFilterAttribute(fakeAzureAdConfig, fakeConfiguration, fakeAzureAdB2CHelper, fakelogger);

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
            var ex = Assert.Throws<ArgumentNullException>(() => new BespokeExchangeSetAuthorizationFilterAttribute(null, null, null, null));
            Assert.That(ex.ParamName, Is.EqualTo("azureAdConfiguration"));
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

            Assert.Multiple(() =>
            {
                Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
                Assert.That(actionExecutingContext.ActionArguments[ExchangeSetStandard], Is.EqualTo(Common.Models.Enums.ExchangeSetStandardForUnitTests.s63.ToString()));
            });
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

            Assert.Multiple(() =>
            {
                Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
                Assert.That(actionExecutingContext.ActionArguments[ExchangeSetStandard], Is.EqualTo(Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString()));
            });
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

            Assert.Multiple(() =>
            {
                Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
                Assert.That(actionExecutingContext.ActionArguments[ExchangeSetStandard], Is.EqualTo(Common.Models.Enums.ExchangeSetStandardForUnitTests.s63.ToString()));
            });
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
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);

            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            Assert.Multiple(() =>
            {
                Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
                Assert.That(actionExecutingContext.ActionArguments[ExchangeSetStandard], Is.EqualTo(Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString()));
            });
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

            Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
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

            Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
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

            Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIss57AndAzureADClientIDIsNotEqualsWithTokenAudience_ThenReturnForbidden()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                { ExchangeSetStandard, Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString() }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            fakeAzureAdConfig.Value.ClientId = "80a6c68b-59ab-49a4-939a-7968ff79d678";

            var claims = new List<Claim>()
            {
                    new Claim(TokenAudience, fakeAzureAdConfig.Value.ClientId),
                    new Claim(TokenTenantId, fakeAzureAdConfig.Value.TenantId),
            };

            var identity = httpContext.User.Identities.FirstOrDefault();
            identity.AddClaims(claims);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);

            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            Assert.Multiple(() =>
            {
                Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
                Assert.That(actionExecutingContext.ActionArguments[ExchangeSetStandard], Is.EqualTo(Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString()));
            });
        }

        [Test]
        [TestCase("0")]
        [TestCase("1")]
        public async Task WhenExchangeSetStandardParameterIsZeroOrOneAndAzureADClientIDIsEqualsWithTokenAudience_ThenReturnBadRequest(string value)
        {
            var dictionary = new Dictionary<string, StringValues>
            {
               { ExchangeSetStandard, value}
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);

            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIss57AndAzureB2CClientIDIsEqualsWithTokenAudienceAndUserisNotAdmin_ThenReturnForbidden()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                { ExchangeSetStandard, Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString() }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            var claims = new List<Claim>()
            {
                    new Claim(TokenAudience, fakeAzureAdB2CConfiguration.Value.ClientId),
                    new Claim(ClaimTypes.Email, "testUser@gmail.com"),
                    new Claim(TokenIssuer, "https://azureAdB2CInstance/azure-ad2c-tenant/v2.0/"),
            };

            var identity = httpContext.User.Identities.FirstOrDefault();
            identity.AddClaims(claims);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);
            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            Assert.Multiple(() =>
            {
                Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
                Assert.That(actionExecutingContext.ActionArguments[ExchangeSetStandard], Is.EqualTo(Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString()));
            });
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIss57AndAzureB2CClientIDIsEqualsWithTokenAudienceAndUserisAdmin_ThenReturnNextRequest()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                { ExchangeSetStandard, Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString() }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            var claims = new List<Claim>()
            {
                    new Claim(TokenAudience, fakeAzureAdB2CConfiguration.Value.ClientId),
                    new Claim(ClaimTypes.Email, "testUser@abc.com"),
                    new Claim(TokenIssuer, "https://azureAdB2CInstance/azure-ad2c-tenant/v2.0/"),
            };

            var identity = httpContext.User.Identities.FirstOrDefault();
            identity.AddClaims(claims);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);
            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            Assert.Multiple(() =>
            {
                Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
                Assert.That(actionExecutingContext.ActionArguments[ExchangeSetStandard], Is.EqualTo(Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString()));
            });
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIss57AndAzureADB2CClientIDIsEqualsWithTokenAudienceAndUserisAdmin_ThenReturnNextRequest()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                { ExchangeSetStandard, Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString() }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            var claims = new List<Claim>()
            {
                    new Claim(TokenAudience, fakeAzureAdB2CConfiguration.Value.ClientId),
                    new Claim(ClaimTypes.Email, "testUser@abc.com"),
                    new Claim(TokenIssuer, "https://login.microsoftonline.com/azure-ad2c-tenant/v2.0/"),
            };

            var identity = httpContext.User.Identities.FirstOrDefault();
            identity.AddClaims(claims);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);
            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            Assert.Multiple(() =>
            {
                Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
                Assert.That(actionExecutingContext.ActionArguments[ExchangeSetStandard], Is.EqualTo(Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString()));
            });
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIss57AndAzureADB2CClientIDIsEqualsWithTokenAudienceAndUserisNotAdmin_ThenReturnForbidden()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                { ExchangeSetStandard, Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString() }
            };
            var correlationId = "305ae767-35c3-41f7-9f57-f25b019d30c5";

            httpContext.Request.Query = new QueryCollection(dictionary);
            httpContext.Request.Headers.Append(XCorrelationIdHeaderKey, correlationId);

            var claims = new List<Claim>()
            {
                    new Claim(TokenAudience, fakeAzureAdB2CConfiguration.Value.ClientId),
                    new Claim(ClaimTypes.Email, "testUser@gmail.com"),
                    new Claim(TokenIssuer, "https://login.microsoftonline.com/azure-ad2c-tenant/v2.0/"),
            };

            var identity = httpContext.User.Identities.FirstOrDefault();
            identity.AddClaims(claims);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);
            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            Assert.Multiple(() =>
            {
                Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
                Assert.That(actionExecutingContext.ActionArguments[ExchangeSetStandard], Is.EqualTo(Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString()));
            });
        }

        [Test]
        public async Task WhenExchangeSetStandardParameterIss57AndAzureADB2CClientIDIsEqualsWithTokenAudienceAndUserEmailisNull_ThenReturnForbidden()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                { ExchangeSetStandard, Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString() }
            };
            httpContext.Request.Query = new QueryCollection(dictionary);

            var claims = new List<Claim>()
            {
                    new Claim(TokenAudience, fakeAzureAdB2CConfiguration.Value.ClientId),
                    new Claim(TokenIssuer, "https://login.microsoftonline.com/azure-ad2c-tenant/v2.0/"),
            };

            var identity = httpContext.User.Identities.FirstOrDefault();
            identity.AddClaims(claims);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);
            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            Assert.Multiple(() =>
            {
                Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
                Assert.That(actionExecutingContext.ActionArguments[ExchangeSetStandard], Is.EqualTo(Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString()));
            });
        }

        [Test]
        public async Task WhenExchangeSet_Request_Has_Invalid_CorrelationId_ThenLogBadRequest_ThenReturnNextRequest()
        {
            var dictionary = new Dictionary<string, StringValues>
            {
                { ExchangeSetStandard, Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString() }
            };
            var correlationId = "305ae767-35c3-f25b019d30c5";

            httpContext.Request.Query = new QueryCollection(dictionary);
            httpContext.Request.Headers.Append(XCorrelationIdHeaderKey, correlationId);

            var claims = new List<Claim>()
            {
                    new Claim(TokenAudience, fakeAzureAdB2CConfiguration.Value.ClientId),
                    new Claim(ClaimTypes.Email, "testUser@gmail.com"),
                    new Claim(TokenIssuer, "https://login.microsoftonline.com/azure-ad2c-tenant/v2.0/"),
            };

            var identity = httpContext.User.Identities.FirstOrDefault();
            identity.AddClaims(claims);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), bespokeFilterAttribute);
            actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), bespokeFilterAttribute);
            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);
            await bespokeFilterAttribute.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));

            Assert.Multiple(() =>
            {
                Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
                Assert.That(actionExecutingContext.ActionArguments[ExchangeSetStandard], Is.EqualTo(Common.Models.Enums.ExchangeSetStandardForUnitTests.s57.ToString()));
            });

            A.CallTo(fakelogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.BadRequest.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "_X-Correlation-ID is invalid :{correlationId}").MustHaveHappenedOnceExactly();
        }
    }
}
