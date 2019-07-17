using ITfoxtec.Identity;
using Microsoft.AspNetCore.Authorization;

namespace FoxIDs.Infrastructure.Security
{
    public class MasterAuthorizeAttribute : AuthorizeAttribute
    {
        public const string Name = nameof(MasterAuthorizeAttribute);

        public MasterAuthorizeAttribute() : base(Name)
        {
            AuthenticationSchemes = JwtBearerMultipleTenantsHandler.AuthenticationScheme;
        }

        public static void AddPolicy(AuthorizationOptions options)
        {
            options.AddPolicy(Name, policy =>
            {
                policy.RequireClaim(JwtClaimTypes.Scope, "foxids_api:master");
            });
        }
    }
}
