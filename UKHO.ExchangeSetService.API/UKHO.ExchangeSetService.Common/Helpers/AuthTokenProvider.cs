using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage] ////Excluded from code coverage as it has ADD interaction
    public class AuthTokenProvider : IAuthTokenProvider
    {
        public async Task<string> GetManagedIdentityAuthAsync(string resource)
        {
            ////var tokenCredential = new DefaultAzureCredential();
            ////var accessToken = await tokenCredential.GetTokenAsync(
            ////    new TokenRequestContext(scopes: new string[] { resource + "/.default" }) { }
            ////);

            ////return accessToken.Token;
            await Task.CompletedTask;
            return "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Im5PbzNaRHJPRFhFSzFqS1doWHNsSFJfS1hFZyIsImtpZCI6Im5PbzNaRHJPRFhFSzFqS1doWHNsSFJfS1hFZyJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjIyNjM1MjM0LCJuYmYiOjE2MjI2MzUyMzQsImV4cCI6MTYyMjYzOTEzNCwiYWNyIjoiMSIsImFpbyI6IkFXUUFtLzhUQUFBQUxtbVh3Sk50aStLaHppRzBIQlpIUmI4MXBCS1ZwOXZkZWpTTVdaSWllNEJPcUlQckp2TVd2UFdWQUVUd0xIWnBVUzR3cVQ5eVZkRCtGMktuZUY0a2ZhclR2V1ZjWjFCY1Z4cXY3K01qdlg0bDVFaUY5ZjhsV3paRjVmM0VIVlFZIiwiYW1yIjpbInB3ZCIsIm1mYSJdLCJhcHBpZCI6IjgwNWJlMDI0LWEyMDgtNDBmYi1hYjZmLTM5OWMyNjQ3ZDMzNCIsImFwcGlkYWNyIjoiMCIsImVtYWlsIjoic2lkZGFydDE0MzkzQG1hc3Rlay5jb20iLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9hZGQxYzUwMC1hNmQ3LTRkYmQtYjg5MC03ZjhjYjZmN2Q4NjEvIiwiaXBhZGRyIjoiMjIzLjE4OS45LjE0MSIsIm5hbWUiOiJTaWRkYXJ0aGEgWS5Nb3RlIiwib2lkIjoiNWE1NzQzOTUtMzJhMC00NWNjLWEzNmQtOTAxZjc0MTA0YjBhIiwicHdkX2V4cCI6Ijc1MTE4MyIsInB3ZF91cmwiOiJodHRwczovL3BvcnRhbC5taWNyb3NvZnRvbmxpbmUuY29tL0NoYW5nZVBhc3N3b3JkLmFzcHgiLCJyaCI6IjAuQVFJQVNNbzBrVDFtQlVxV2lqR2tMd3J0UGlUZ1c0QUlvdnRBcTI4NW5DWkgwelFDQUF3LiIsInJvbGVzIjpbIkJhdGNoQ3JlYXRlIl0sInNjcCI6IlVzZXIuUmVhZCIsInN1YiI6IlZnSXVIS3hiUXUwZ1dMdFN0NXhaNFNRelJmVEhyNUNoX0pESXJ1eUJGaGMiLCJ0aWQiOiI5MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UiLCJ1bmlxdWVfbmFtZSI6InNpZGRhcnQxNDM5M0BtYXN0ZWsuY29tIiwidXRpIjoiWmJ6TnlRWThPVUNrRm5EdXgwOEZBQSIsInZlciI6IjEuMCJ9.NcdZTSyOeVP3wuqiJSivrBLIhFbnJeu-7sdc2YKmYz96CGmpzp1oQiQoqmP3wSGjs4sRIcPAVhFrt2AeHo-__YiLTJ8ncbttIWSt3hu-B683kvuiCrmJAggQr2EiZQJjnwq51-8fSI10fqaKhuC2yOzMW3EpGOJiAETQ0YXnV1w3RVl6pYPXr3NlIPbsSzhIqdipYui8OiSYqDKVpHveb4IErBwnhphyfZdGJ_mDUohazoxhHgx5pd879IlngVGZRBXCiUSaV59pjlIT66RysaGY1DJERrxc6iS5P0M5hExEbDsm6eG7rk4UtVuCdheSphMmnNc0lRkcWeryIxasCw";
        }
    }
}
