using ITfoxtec.Identity;
using Microsoft.AspNetCore.Authorization;

namespace FoxIDs.Infrastructure.Security
{
    public class TenantAuthorizeAttribute : AuthorizeAttribute
    {
        public const string Name = nameof(TenantAuthorizeAttribute);

        public TenantAuthorizeAttribute() : base(Name)
        {
            AuthenticationSchemes = JwtBearerMultipleTenantsHandler.AuthenticationScheme;
        }

        public static void AddPolicy(AuthorizationOptions options)
        {
            options.AddPolicy(Name, policy =>
            {
                policy.RequireClaim(JwtClaimTypes.Scope, "foxids_api:tenant");
                policy.RequireClaim(JwtClaimTypes.Role, "master_admin");
            });
        }
    }
}
