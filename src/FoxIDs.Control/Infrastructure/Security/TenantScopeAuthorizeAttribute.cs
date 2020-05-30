using ITfoxtec.Identity;
using Microsoft.AspNetCore.Authorization;

namespace FoxIDs.Infrastructure.Security
{
    public class TenantScopeAuthorizeAttribute : AuthorizeAttribute
    {
        public const string Name = nameof(TenantScopeAuthorizeAttribute);

        public TenantScopeAuthorizeAttribute() : base(Name)
        {
            AuthenticationSchemes = JwtBearerMultipleTenantsHandler.AuthenticationScheme;
        }

        public static void AddPolicy(AuthorizationOptions options)
        {
            options.AddPolicy(Name, policy =>
            {
                policy.RequireScope("foxids_control_api:foxids_tenant");
            });
        }
    }
}
