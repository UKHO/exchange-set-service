using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static UKHO.ExchangeSetService.API.FunctionalTests.Helper.TestConfiguration;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class AzureB2CAuthTokenProvider

    {
        static AzureAdB2CConfiguration B2cConfig = new TestConfiguration().AzureAdB2CConfig;
        static EssAuthorizationTokenConfiguration EssAuthConfig = new TestConfiguration().EssAuthorizationConfig;


        /// <summary>
        /// To Get B2C Token
        /// </summary>
        public async Task<string> GetToken()
        {
            return await GenerateToken(B2cConfig.ClientId);
        }


        /// <summary>
        /// Generate Token
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        private static async Task<string> GenerateToken(string clientId)
        {
            if (B2cConfig.IsRunningOnLocalMachine)
            {
                //Generate token locally and pass it in appsettings.json
                return B2cConfig.LocalTestToken;
            }
            else
            {
                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                           .Create(clientId)
                           .WithTenantId(B2cConfig.TenantId)
                           .WithAuthority($"{EssAuthConfig.MicrosoftOnlineLoginUrl}{B2cConfig.TenantId}", true)
                           .WithClientSecret(B2cConfig.ClientSecret)
                           .Build();
                var scopes = new string[] { $"https://{B2cConfig.Domain}/exchangesetservice/.default" };
                AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                return result.AccessToken;
            }
        }


        /// <summary>
        /// Generate custom B2C Token
        /// </summary>
        public string GenerateCustomToken()
        {
            var privateKey = TestConfiguration.FakeTokenPrivateKey;
            var header = new Dictionary<string, object>
            {
                {  "typ", "JWT" },                
                {  "kid", "GvnPApfWMdLRi8PDmisFn7bprKu"}
            };

            var provider = new UtcDateTimeProvider();
            var now = provider.GetNow();
            var tokenIssued = UnixEpoch.GetSecondsSince(now);
            var expiry = tokenIssued + 3600;

            var payload = new Dictionary<string, object>
            {
                { "aud", $"{B2cConfig.TenantId}"},
                { "iss", $"{B2cConfig.MicrosoftOnlineLoginUrl}{B2cConfig.TenantId}/v2.0/" },
                { "iat", tokenIssued},
                { "nbf", tokenIssued},
                { "exp", expiry },
                { "aio", "E2ZgYOD/bNfNzDaJjWGlunhesLAUAA==" },
                { "azp", $"{B2cConfig.ClientId}" },
                { "azpacr", "1" },
                { "oid", "da599026-93fc-4d2a-92c8-94b724e26176"},
                { "rh", "0.AT8A2Ihb28pF7EqgbHH88S3oGvAwypvZIDhLmevHr_a38FFAAAA."},
                { "sub", "da599026-93fc-4d2a-92c8-94b724e26176"},
                { "tid", $"{B2cConfig.TenantId}"},
                { "uti", "C6oLcfz8e0mzZbv-6pRwAQ"},
                { "ver", "2.0"}

            };

            var privateKeyBytes = Convert.FromBase64String(privateKey);
            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
            IJwtAlgorithm algorithm = new RS256Algorithm(rsa, rsa);
            IJsonSerializer serializer = new JsonNetSerializer();
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            IJwtEncoder encoder = new JwtEncoder(algorithm, serializer, urlEncoder);

            return encoder.Encode(header, payload, "");
        }
    }
}
