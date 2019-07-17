using Microsoft.AspNetCore.Authentication;

namespace FoxIDs.Infrastructure.Security
{
    public class JwtBearerMultipleTenantsPotions : AuthenticationSchemeOptions
    {
        public string FoxIDsEndpoint { get; set; }
        public string DownParty { get; set; }
    }
}
