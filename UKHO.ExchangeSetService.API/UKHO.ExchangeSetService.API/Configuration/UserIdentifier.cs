using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace UKHO.ExchangeSetService.API.Configuration
{
    public class UserIdentifier
    {
        public string UserIdentity { get; set; }
        public UserIdentifier(IHttpContextAccessor httpContextAccessor)
        {           
            UserIdentity = Convert.ToString(httpContextAccessor.HttpContext.User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier"));

        }
    }
}
