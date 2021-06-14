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
        static AzureAdB2CConfiguration B2CConfig = new TestConfiguration().AzureAdB2CConfig;
        static EssAuthorizationTokenConfiguration EssAuthConfig = new TestConfiguration().EssAuthorizationConfig;


        /// <summary>
        /// To Get B2C Token
        /// </summary>
        public async Task<string> GetToken()
        {
            return await GenerateToken(B2CConfig.ClientId);
        }


        /// <summary>
        /// Generate Token
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        private static async Task<string> GenerateToken(string clientId)
        {
            if (B2CConfig.IsRunningOnLocalMachine)
            {
                //Generate token locally and pass it in appsettings.json
                return B2CConfig.LocalToken;
            }
            else
            {
                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                           .Create(clientId)
                           .WithTenantId(B2CConfig.TenantId)
                           .WithAuthority($"{EssAuthConfig.MicrosoftOnlineLoginUrl}{B2CConfig.TenantId}", true)
                           .WithClientSecret(B2CConfig.ClientSecret)
                           .Build();
                var scopes = new string[] { $"https://{B2CConfig.Domain}/api/.default" };
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
                {  "kid", "GvnPApfWMdLRi8PDmisFn7bprKg"}
            };

            var provider = new UtcDateTimeProvider();
            var now = provider.GetNow();
            var tokenIssued = UnixEpoch.GetSecondsSince(now);
            var expiry = tokenIssued + 3600;

            var payload = new Dictionary<string, object>
            {
                { "exp", expiry },
                { "nbf", tokenIssued},
                { "ver", "1.0"},
                { "iss", $"{B2CConfig.Instance}{B2CConfig.TenantId}/v2.0/" },
                { "sub", $"{B2CConfig.TenantId}"},
                { "aud", $"{B2CConfig.ClientId}"},
                { "acr", $"{B2CConfig.SignUpSignInPolicy}" },
                { "iat", tokenIssued},
                { "auth_time", tokenIssued},
                { "email", $"{B2CConfig.UserId}" },
                { "oid", $"{B2CConfig.TenantId}"}
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
