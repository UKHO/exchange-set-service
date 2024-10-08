﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.Identity.Client;
using static UKHO.ExchangeSetService.API.FunctionalTests.Helper.TestConfiguration;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class AuthTokenProvider
    {
        static string EssAccessToken = null;
        static string FssAccessToken = null;
        static string EssAccessTokenNoAuth = null;
        static string ScsAccessToken = null;
        static EssAuthorizationTokenConfiguration EssauthConfig = new TestConfiguration().EssAuthorizationConfig;
        static FileShareService FssAuthConfig = new TestConfiguration().FssConfig;
        static SalesCatalogue ScsAuthConfig = new TestConfiguration().ScsAuthConfig;

        public async Task<string> GetEssToken()
        {
            EssAccessToken = await GenerateEssToken(EssauthConfig.AutoTestClientId, EssauthConfig.AutoTestClientSecret, EssAccessToken);
            return EssAccessToken;
        }

        public async Task<string> GetEssTokenNoAuth()
        {
            EssAccessTokenNoAuth = await GenerateEssToken(EssauthConfig.AutoTestClientIdNoAuth, EssauthConfig.AutoTestClientSecretNoAuth, EssAccessTokenNoAuth);
            return EssAccessTokenNoAuth;
        }

        public async Task<string> GetFssToken()
        {
            FssAccessToken = await GenerateFssToken(EssauthConfig.AutoTestClientId, EssauthConfig.AutoTestClientSecret, FssAccessToken);
            return FssAccessToken;
        }

        /// <summary>
        /// Generate FSS Token
        /// </summary>
        /// <param name="ClientId"></param>
        /// <param name="ClientSecret"></param>
        /// <param name="Token"></param>
        /// <returns></returns>

        private static async Task<string> GenerateFssToken(string ClientId, string ClientSecret, string Token)
        {
            string[] scopes = new string[] { $"{FssAuthConfig.ResourceId}/.default" };
            if (Token == null)
            {
                if (FssAuthConfig.IsRunningOnLocalMachine)
                {
                    IPublicClientApplication debugApp = PublicClientApplicationBuilder.Create(FssAuthConfig.ResourceId).
                                                        WithRedirectUri("http://localhost").WithTenantId(EssauthConfig.TenantId).Build();

                    //Acquiring token through user interaction
                    AuthenticationResult tokenTask = await debugApp.AcquireTokenInteractive(scopes)
                                                            .ExecuteAsync();
                    Token = tokenTask.AccessToken;
                }
                else
                {
                    IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(ClientId)
                                                    .WithClientSecret(ClientSecret)
                                                    .WithAuthority(new Uri($"{EssauthConfig.MicrosoftOnlineLoginUrl}{EssauthConfig.TenantId}"))
                                                    .Build();

                    AuthenticationResult tokenTask = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                    Token = tokenTask.AccessToken;
                }

            }
            return Token;
        }

        /// <summary>
        /// GenerateToken
        /// </summary>
        /// <param name="ClientId"></param>
        /// <param name="ClientSecret"></param>
        /// <param name="Token"></param>
        /// <returns></returns>

        private static async Task<string> GenerateEssToken(string ClientId, string ClientSecret, string Token)
        {
            string[] scopes = new string[] { $"{EssauthConfig.EssClientId}/.default" };
            if (Token == null)
            {
                if (EssauthConfig.IsRunningOnLocalMachine)
                {
                    IPublicClientApplication debugApp = PublicClientApplicationBuilder.Create(EssauthConfig.EssClientId).
                                                        WithRedirectUri("http://localhost").WithTenantId(EssauthConfig.TenantId).Build();

                    //Acquiring token through user interaction
                    AuthenticationResult tokenTask = await debugApp.AcquireTokenInteractive(scopes)
                                                            .ExecuteAsync();
                    Token = tokenTask.AccessToken;
                }
                else
                {
                    IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(ClientId)
                                                    .WithClientSecret(ClientSecret)
                                                    .WithAuthority(new Uri($"{EssauthConfig.MicrosoftOnlineLoginUrl}{EssauthConfig.TenantId}"))
                                                    .Build();

                    AuthenticationResult tokenTask = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                    Token = tokenTask.AccessToken;
                }

            }
            return Token;
        }

        public async Task<string> GetScsToken()
        {
            ScsAccessToken = await GenerateScsToken(EssauthConfig.AutoTestClientId, EssauthConfig.AutoTestClientSecret, ScsAccessToken);
            return ScsAccessToken;
        }

        /// <summary>
        /// Generate SCS Token
        /// </summary>
        /// <param name="ClientId"></param>
        /// <param name="ClientSecret"></param>
        /// <param name="Token"></param>
        /// <returns></returns>

        private static async Task<string> GenerateScsToken(string ClientId, string ClientSecret, string Token)
        {
            string[] scopes = new string[] { $"{ScsAuthConfig.ResourceId}/.default" };
            if (Token == null)
            {
                if (ScsAuthConfig.IsRunningOnLocalMachine)
                {
                    IPublicClientApplication debugApp = PublicClientApplicationBuilder.Create(ScsAuthConfig.ResourceId).
                                                        WithRedirectUri("http://localhost").WithTenantId(EssauthConfig.TenantId).Build();

                    //Acquiring token through user interaction
                    AuthenticationResult tokenTask = await debugApp.AcquireTokenInteractive(scopes)
                                                            .ExecuteAsync();
                    Token = tokenTask.AccessToken;
                }
                else
                {
                    IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(ClientId)
                                                    .WithClientSecret(ClientSecret)
                                                    .WithAuthority(new Uri($"{EssauthConfig.MicrosoftOnlineLoginUrl}{EssauthConfig.TenantId}"))
                                                    .Build();

                    AuthenticationResult tokenTask = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                    Token = tokenTask.AccessToken;
                }

            }
            return Token;
        }

        /// <summary>
        /// Generate custom signature verified Auth Token
        /// </summary>
        public string GenerateCustomToken()
        {
            var privateKey = TestConfiguration.FakeTokenPrivateKey;
            var header = new Dictionary<string, object>
            {
                {  "x5t", "kg2LYs2T0CTjIfj4rt6JIynen38"},
                {  "kid", "kg2LYs2T0CTjIfj4rt6JIynen38"}
            };

            var provider = new UtcDateTimeProvider();
            var now = provider.GetNow();
            var tokenIssued = UnixEpoch.GetSecondsSince(now);
            var expiry = tokenIssued + 3600;

            var payload = new Dictionary<string, object>
            {
                { "aud", $"{EssauthConfig.EssClientId}"},
                { "iss", $"https://sts.windows.net/{EssauthConfig.TenantId}/" },
                { "iat",tokenIssued},
                { "nbf", tokenIssued},
                { "exp", expiry },
                { "aio", "E2RgYPisIWqdtDHp72InvliZoLuf+m/cOdbklLQrIXRDxgPb23MB" },
                { "appid", $"{EssauthConfig.AutoTestClientId}"},
                { "appidacr", "1" },
                { "idp", $"https://sts.windows.net/{EssauthConfig.TenantId}/"},
                { "oid", "da599026-93fc-4d2a-92c8-94b724e26176" },
                { "rh", "0.AAAASMo0kT1mBUqWijGkLwrtPjtAyT6ZgpBKjswH7mZCEJ8CAP0."},
                { "roles", new string [] { "BatchCreate" }  },
                { "sub", "uftNZPaOJaWSYJqHrMIkFhg3rgQ97G9Km9fDl48WQPk"},
                { "tid", "9134ca48-663d-4a05-968a-31a42f0aed3e"},
                { "uti", "KOT0iQPMzESCe4R_Ce94AA"},
                { "ver", "1.0"}
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
